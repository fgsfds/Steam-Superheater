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
    Task<Result> CheckAndDownloadFileAsync(Uri url, string filePath, CancellationToken cancellationToken);
    Task<Result> CheckFileHashAsync(string filePath, string hash, CancellationToken cancellationToken);
}