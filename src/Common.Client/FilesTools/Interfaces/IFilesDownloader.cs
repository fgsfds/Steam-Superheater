using Common.Axiom;

namespace Common.Client.FilesTools.Interfaces;

public interface IFilesDownloader
{
    /// <summary>
    /// Download ZIP
    /// </summary>
    /// <param name="url">Link to file download</param>
    /// <param name="filePath">Absolute path to destination file</param>
    /// <param name="cancellationToken"></param>
    Task<Result> DownloadFileAsync(Uri url, string filePath, CancellationToken cancellationToken);
}