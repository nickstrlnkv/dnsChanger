using System;
using System.IO;
using System.Windows;

namespace DnsChanger;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            File.WriteAllText("crash.log", ex?.ToString() ?? "Unknown exception");
        };

        DispatcherUnhandledException += (s, args) =>
        {
            File.WriteAllText("crash.log", args.Exception?.ToString() ?? "Dispatcher exception");
        };

        base.OnStartup(e);
    }
}
