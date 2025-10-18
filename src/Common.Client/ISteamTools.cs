
namespace Common.Client;

public interface ISteamTools
{
    string? SteamInstallPath { get; }

    /// <summary>
    /// Get list of ACF files from all Steam libraries
    /// </summary>
    List<string> GetAcfsList();
}