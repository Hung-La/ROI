using System.Configuration;
using System.Data;
using System.Windows;

namespace ROI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (s, args) =>
            {
                System.IO.File.WriteAllText("crash.log", args.Exception.ToString());
                MessageBox.Show(args.Exception.ToString(), "Lỗi ứng dụng", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    System.IO.File.WriteAllText("crash.log", ex.ToString());
                }
            };
        }
    }

}
