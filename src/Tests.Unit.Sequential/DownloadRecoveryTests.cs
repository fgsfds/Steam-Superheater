using System.Net;
using System.Net.Http.Headers;
using Common.Axiom;
using Common.Client;
using Common.Client.FilesTools;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Unit.Sequential;

public sealed class DownloadRecoveryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "superheater-download-" + Guid.NewGuid().ToString("N"));
    public DownloadRecoveryTests() => Directory.CreateDirectory(_root);

    [Theory]
    [InlineData("resume", true)]
    [InlineData("restart", true)]
    [InlineData("wrong-offset", false)]
    [InlineData("changed-etag", false)]
    [InlineData("416", false)]
    public async Task ResumeValidatesRangeAndIdentity(string mode, bool success)
    {
        using var handler = new RecoveryHandler(mode);
        var factory = new Mock<IHttpClientFactory>();
        _ = factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, false));
        var progress = new ProgressReport();
        var path = Path.Combine(_root, "download");
        await File.WriteAllTextAsync(path, "previous");
        var result = await new FilesDownloader(progress, factory.Object, new Mock<ILogger>().Object)
            .DownloadFileAsync(new Uri("https://example.test/file"), path, CancellationToken.None);
        Assert.Equal(success, result.IsSuccess);
        Assert.Equal(success ? "abcdef" : "previous", await File.ReadAllTextAsync(path));
        Assert.False(File.Exists(path + ".temp"));
        Assert.Equal(string.Empty, progress.OperationMessage);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task InitialCancellationAndFileCreationErrorsAreResults()
    {
        var progress = new ProgressReport();
        var downloader = new FilesDownloader(progress, new Mock<IHttpClientFactory>().Object, new Mock<ILogger>().Object);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var result = await downloader.DownloadFileAsync(new Uri("https://example.test/file"), Path.Combine(_root, "file"), cancellation.Token);
        Assert.Equal(ResultEnum.Cancelled, result.ResultEnum);
        result = await downloader.DownloadFileAsync(new Uri("https://example.test/file"), Path.Combine(_root, "missing", "file"), CancellationToken.None);
        Assert.Equal(ResultEnum.Error, result.ResultEnum);
        Assert.Equal(string.Empty, progress.OperationMessage);
    }

    private sealed class RecoveryHandler(string mode) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (Calls == 1)
            {
                response.Content = new StreamContent(new InterruptedStream());
                response.Content.Headers.ContentLength = 6;
                response.Headers.ETag = new EntityTagHeaderValue("\"same\"");
            }
            else
            {
                Assert.Equal(3, request.Headers.Range!.Ranges.Single().From);
                Assert.Null(request.Headers.Range.Ranges.Single().To);
                Assert.Equal("\"same\"", request.Headers.IfRange!.EntityTag!.Tag);
                response.StatusCode = mode == "416" ? HttpStatusCode.RequestedRangeNotSatisfiable : mode == "restart" ? HttpStatusCode.OK : HttpStatusCode.PartialContent;
                response.Content = new StringContent(mode == "restart" ? "abcdef" : "def");
                response.Headers.ETag = new EntityTagHeaderValue(mode == "changed-etag" ? "\"changed\"" : "\"same\"");
                if (response.StatusCode == HttpStatusCode.PartialContent)
                {
                    response.Content.Headers.ContentRange = new ContentRangeHeaderValue(mode == "wrong-offset" ? 2 : 3, 5, 6);
                }
            }
            return Task.FromResult(response);
        }
    }

    private sealed class InterruptedStream : MemoryStream
    {
        public InterruptedStream() : base("abc"u8.ToArray()) { }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (Position == Length) { throw new HttpIOException(HttpRequestError.ResponseEnded, "interrupted"); }
            return base.ReadAsync(buffer, cancellationToken);
        }
    }
    public void Dispose() => Directory.Delete(_root, true);
}
