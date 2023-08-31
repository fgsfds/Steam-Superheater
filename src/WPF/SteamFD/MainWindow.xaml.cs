using SteamFD.DI;
using SteamFDCommon.DI;
using System;
using System.Windows;

namespace SteamFD
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Dispatcher.UnhandledException += OnDispatcherUnhandledException;

            var container = BindingsManager.Instance;
            container.Options.EnableAutoVerification = false;

            ModelsBindings.Load(container);
            ViewModelsBindings.Load(container);
            CommonBindings.Load(container);

            InitializeComponent();

            void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
            {
                string errorMessage = $"An unhandled exception occurred:{Environment.NewLine}{e.Exception.Message}";

                if (e.Exception.InnerException is not null)
                {
                    errorMessage += Environment.NewLine;
                    errorMessage += e.Exception.InnerException.Message;
                }

                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);

                e.Handled = true;

                Environment.Exit(-1);
            }
        }
    }
}
