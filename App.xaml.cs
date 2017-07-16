using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using kPassKeep.Util;
using Serilog;

namespace kPassKeep
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        static App()
        {
            var loglevel = Serilog.Events.LogEventLevel.Error;
            var args = Environment.GetCommandLineArgs();
            if (args.Contains("-v") || args.Contains("/v") || args.Contains("--v"))
            {
                loglevel = Serilog.Events.LogEventLevel.Verbose;
            }
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(loglevel)
                .WriteTo.Console()
                .WriteTo.RollingFile("log/log-{Date}.txt")
                .CreateLogger();

            Log.Information("Application loaded");
            Log.Verbose("Current directory: {Dir:l}", Environment.CurrentDirectory);
            Log.Verbose("Command line: {Start:l}", Environment.CommandLine);
            Log.Verbose("OS: {OS:l}", Environment.OSVersion);
            Log.Verbose("CLR: {CLR:l}", Environment.Version);
            Log.Verbose("System information: {@Env}", Environment.GetEnvironmentVariables());
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.Exception, "An unhandled exception occurred");

            /**
             *  #1 System.IO.FileLoadException when press target auto fill button
             *  
             *  Description:
             *  
             *      FileLoadException referencing to `System.Core` assembly:
             *          Could not load file or assembly 'System.Core, Version=2.0.5.0...
             *          File name: 'System.Core, Version=2.0.5.0...
             * 
             *  Provided solution:
             *  
             *      Catch such type of exception and stop default exception processing.
             */
            if (e.Exception.GetType() == typeof(System.IO.FileLoadException))
            {
                var filename = ((System.IO.FileLoadException)e.Exception).FileName;

                if (filename.Contains("System.Core"))
                {
                    e.Handled = true;

                    NotifyUserAboutBugNo1();

                    return;
                }
            }

            e.Handled = false;
        }

        private void NotifyUserAboutBugNo1()
        {
            var href = @"https://github.com/kalaider/kPassKeep/issues/1";
            var link = new System.Windows.Documents.Hyperlink
            {
                NavigateUri = new Uri(href)
            };
            link.Inlines.Add("Read more");
            link.RequestNavigate += (s, e) => { System.Diagnostics.Process.Start(href); };
            var content = new System.Windows.Controls.TextBlock();
            content.Inlines.Add("Looks like you have stumbled upon a bug #1 in our bug tracker. ");
            content.Inlines.Add(link);

            var dlg = new ModernDialog
            {
                Title = "Error",
                Content = content,
                MinHeight = 0,
                MinWidth = 0,
                MaxWidth = 640,
                Owner = Application.Current.MainWindow
            };

            dlg.Buttons = dlg.OkButton.Yield();
            dlg.ShowDialog();
        }
    }
}
