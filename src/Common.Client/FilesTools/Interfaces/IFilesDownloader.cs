namespace Common.Client.FilesTools.Interfaces;

public interface IFilesDownloader
{
    /// <summary>
    /// Download ZIP
    /// </summary>
    /// <param name="url">Link to file download</param>
    /// <param name="filePath">Absolute path to destination file</param>
    /// <param name="cancellationToken"></param>
    /// <param name="fixGuid">Fix GUID</param>
    /// <param name="hash">MD5 to check file against</param>
    /// <exception cref="Exception">Error while downloading file</exception>
    Task<Result> CheckAndDownloadFileAsync(Uri url, string filePath, CancellationToken cancellationToken, Guid? fixGuid = null, string? hash = null);
}