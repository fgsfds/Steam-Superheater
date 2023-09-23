using SimpleInjector;

namespace SteamFDCommon.DI
{
    public static class BindingsManager
    {
        private static volatile Container? _instance;
        private static readonly object _syncRoot = new();

        public static Container Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        _instance ??= new Container();
                    }
                }
                return _instance;
            }
        }

        public static void Reset()
        {
            _instance = null;
        }
    }
}
