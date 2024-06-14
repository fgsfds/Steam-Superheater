using Microsoft.Extensions.DependencyInjection;

namespace Common.Client.DI
{
    public static class BindingsManager
    {
        private static ServiceCollection? _instance;
        private static ServiceProvider? _provider;
        private static readonly object _syncRoot = new();

        public static ServiceCollection Instance
        {
            get
            {
                if (_instance is null)
                {
                    lock (_syncRoot)
                    {
                        _instance = new ServiceCollection();
                    }
                }

                return _instance;
            }
        }

        public static ServiceProvider Provider
        {
            get
            {
                if (_provider is null)
                {
                    lock (_syncRoot)
                    {
                        _provider = Instance.BuildServiceProvider();
                    }
                }

                return _provider;
            }
        }

        public static void Reset()
        {
            _instance = null;
            _provider = null;
        }
    }
}
