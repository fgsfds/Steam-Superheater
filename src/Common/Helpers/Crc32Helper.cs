using System.IO.Hashing;

namespace Common.Helpers;

public static class Crc32Helper
{
    public static async Task<string> GetCrc32Async(string path, bool isHex)
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
        var buffer = new byte[BufferSize].AsMemory();
        int bytesRead;

        while ((bytesRead = await fs.ReadAsync(buffer).ConfigureAwait(false)) > 0)
        {
            hasher.Append(buffer.Span);
        }

        var hash = hasher.GetCurrentHash();
        var crc = BitConverter.ToUInt32(hash, 0);

        return isHex
            ? $"0x{crc:X8}"
            : crc.ToString();
    }
}
