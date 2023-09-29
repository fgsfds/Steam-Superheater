using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Common.Helpers;
using Common;

namespace Common.Entities
{
    public sealed class GameEntity : ObservableObject
    {
        /// <summary>
        /// Steam game ID
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// Game title
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Game install directory
        /// </summary>
        public string InstallDir { get; set; }

        /// <summary>
        /// Game icon
        /// </summary>
        public string Icon => Path.Combine(SteamTools.SteamInstallPath, @$"appcache\librarycache\{Id}_icon.jpg");

        /// <summary>
        /// Game executable
        /// Only defined if the game requires admin rights, otherwise is null
        /// </summary>
        public string? GameExecutable => _gamesThatRequireAdmin.ContainsKey(Id) ? _gamesThatRequireAdmin[Id] : null;

        /// <summary>
        /// Does the game require admin rights
        /// </summary>
        public bool DoesRequireAdmin
        {
            get
            {
                if (!_gamesThatRequireAdmin.ContainsKey(Id))
                {
                    return false;
                }

                var data = Registry.GetValue(Consts.AdminRegistryKey, $"{InstallDir}{GameExecutable}", null);

                if (data is not null &&
                    data.Equals("~ RUNASADMIN"))
                {
                    return false;
                }

                return true;
            }
        }

        public GameEntity(
            int id,
            string name,
            string dir
            )
        {
            Id = id;
            Name = name;
            InstallDir = dir;
        }

        /// <summary>
        /// Add value the the registry to always run the game as admin
        /// </summary>
        public void SetRunAsAdmin()
        {
            Registry.SetValue(Consts.AdminRegistryKey, $"{InstallDir}{GameExecutable}", "~ RUNASADMIN");
            OnPropertyChanged(nameof(DoesRequireAdmin));
        }

        public override string ToString() => Name;

        /// <summary>
        /// Pairs 'game id - game exe'
        /// Only used for games that require admin rights
        /// </summary>
        private readonly Dictionary<int, string> _gamesThatRequireAdmin = new()
        {
            {13530, "PrinceOfPersia.exe" },
            {13500, "PrinceOfPersia.exe" }
        };
    }
}