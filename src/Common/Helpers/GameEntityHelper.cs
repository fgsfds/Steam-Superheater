using Common.Entities;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Common.Helpers
{
    public static class GameEntityHelper
    {
        /// <summary>
        /// Does the game require admin rights
        /// </summary>
        public static bool DoesRequireAdmin(this GameEntity game)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }

            if (!_gamesThatRequireAdmin.ContainsKey(game.Id))
            {
                return false;
            }

            var data = Registry.GetValue(Consts.AdminRegistryKey, $"{game.InstallDir}{GetGameExecutable(game.Id)}", null);

            if (data is not null &&
                data.Equals("~ RUNASADMIN"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Add value to the registry to make the game always run as admin
        /// </summary>
        public static void SetRunAsAdmin(this GameEntity game)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            Registry.SetValue(Consts.AdminRegistryKey, $"{game.InstallDir}{GetGameExecutable(game.Id)}", "~ RUNASADMIN");
        }

        /// <summary>
        /// Pairs of game id and game exe
        /// Only used for games that require admin rights
        /// </summary>
        private static readonly Dictionary<int, string> _gamesThatRequireAdmin = new()
        {
            {13530, "PrinceOfPersia.exe" },
            {13500, "PrinceOfPersia.exe" }
        };

        /// <summary>
        /// Game executable
        /// Only defined if the game requires admin rights, otherwise is null
        /// </summary>
        private static string? GetGameExecutable(int id) => _gamesThatRequireAdmin.TryGetValue(id, out string? value) ? value : null;
    }
}
