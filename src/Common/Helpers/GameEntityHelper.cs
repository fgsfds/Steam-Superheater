//using Microsoft.Win32;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;

//namespace Common.Helpers
//{
//    public class GameEntityHelper
//    {

//        /// <summary>
//        /// Pairs 'game id - game exe'
//        /// Only used for games that require admin rights
//        /// </summary>
//        private readonly Dictionary<int, string> _gamesThatRequireAdmin = new()
//        {
//            {13530, "PrinceOfPersia.exe" },
//            {13500, "PrinceOfPersia.exe" }
//        };

//        /// <summary>
//        /// Game executable
//        /// Only defined if the game requires admin rights, otherwise is null
//        /// </summary>
//        public string? GameExecutable => _gamesThatRequireAdmin.ContainsKey(Id) ? _gamesThatRequireAdmin[Id] : null;

//        /// <summary>
//        /// Does the game require admin rights
//        /// </summary>
//        public bool DoesRequireAdmin
//        {
//            get
//            {
//                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//                {
//                    return false;
//                }

//                if (!_gamesThatRequireAdmin.ContainsKey(Id))
//                {
//                    return false;
//                }

//                var data = Registry.GetValue(Consts.AdminRegistryKey, $"{InstallDir}{GameExecutable}", null);

//                if (data is not null &&
//                    data.Equals("~ RUNASADMIN"))
//                {
//                    return false;
//                }

//                return true;
//            }
//        }

//        /// <summary>
//        /// Add value the the registry to always run the game as admin
//        /// </summary>
//        public void SetRunAsAdmin()
//        {
//            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//            {
//                return;
//            }

//            Registry.SetValue(Consts.AdminRegistryKey, $"{InstallDir}{GameExecutable}", "~ RUNASADMIN");
//            OnPropertyChanged(nameof(DoesRequireAdmin));
//        }
//    }
//}
