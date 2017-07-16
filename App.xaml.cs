using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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

    }
}
