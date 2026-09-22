using System.Net;
using Common.Axiom;
using Common.Client;
using Common.Client.FilesTools;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

/// <summary>
/// Tests for <see cref="FilesDownloader"/>
/// </summary>
public sealed class FilesDownloaderTests
{
    /// <summary>
    /// A download that keeps failing mid-stream stops retrying and returns an error instead of recursing forever
    /// </summary>
    [Fact]
    public async Task DownloadReturnsErrorWhenConnectionKeepsFailing()
    {
        var handler = new ThrowingHandler();

        var factoryMock = new Mock<IHttpClientFactory>();
        _ = factoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler, disposeHandler: false));

        FilesDownloader downloader = new(new ProgressReport(), factoryMock.Object, new Mock<ILogger>().Object);

        var filePath = Path.Combine(Path.GetTempPath(), "superheater_download_" + Guid.NewGuid().ToString("N") + ".zip");
        var tempFile = filePath + ".temp";

        try
        {
            var result = await downloader.DownloadFileAsync(new Uri("https://example.com/file.zip"), filePath, CancellationToken.None).ConfigureAwait(true);

            Assert.Equal(ResultEnum.Error, result.ResultEnum);
            Assert.Equal(6, handler.SendCount);
        }
        finally
        {
            handler.Dispose();

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        public int SendCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;

            var status = request.Headers.Range is null ? HttpStatusCode.OK : HttpStatusCode.PartialContent;

            var response = new HttpResponseMessage(status);

#pragma warning disable IDISP004 // Content is owned and disposed by the response
            response.Content = new StreamContent(new ThrowingStream());
#pragma warning restore IDISP004

            response.Content.Headers.ContentLength = 100;

            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw CreateException();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw CreateException();

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw CreateException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private static HttpIOException CreateException() => new(HttpRequestError.ResponseEnded, "Connection lost");
    }
}
