using System.IO.Hashing;

namespace Common.Axiom.Helpers;

/// <summary>
/// Calculates CRC32 checksums of files.
/// </summary>
public static class Crc32Helper
{
    /// <summary>
    /// Calculate the CRC32 checksum of a file.
    /// </summary>
    /// <param name="path">Path to the file.</param>
    /// <returns>CRC32 checksum value.</returns>
    public static async Task<uint> GetCrc32Async(string path)
    {
        const int BufferSize = 1 << 20; // 1 MiB

        await using var fs = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            BufferSize,
            FileOptions.SequentialScan | FileOptions.Asynchronous
        );

        var hasher = new Crc32();
        var buffer = new byte[BufferSize];
        int bytesRead;

        while ((bytesRead = await fs.ReadAsync(buffer).ConfigureAwait(false)) > 0)
        {
            hasher.Append(buffer.AsSpan(0, bytesRead));
        }

        var hash = hasher.GetCurrentHash();
        return BitConverter.ToUInt32(hash, 0);
    }
}
