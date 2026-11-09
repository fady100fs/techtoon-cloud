using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Data.Context;
using Techtoon.UI.Services;

namespace Techtoon.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static IConfiguration Configuration { get; private set; } = null!;

    private static readonly string LogFile = Path.Combine(AppContext.BaseDirectory, "techtoon.log");

    public static void Log(string msg)
    {
        var line = "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + msg;
        try { File.AppendAllText(LogFile, line + Environment.NewLine); } catch { }
        System.Diagnostics.Debug.WriteLine(line);
        Console.WriteLine(line);
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Log("UNHANDLED AppDomain: " + ex);
            Log("  Type: " + ex?.GetType().Name);
            Log("  Message: " + ex?.Message);
            Log("  Stack: " + ex?.StackTrace);
            if (ex?.InnerException != null)
                Log("  Inner: " + ex.InnerException.Message);
        };
        DispatcherUnhandledException += (s, args) =>
        {
            Log("UNHANDLED Dispatcher: " + args.Exception);
            Log("  Type: " + args.Exception.GetType().Name);
            Log("  Message: " + args.Exception.Message);
            Log("  Stack: " + args.Exception.StackTrace);
            if (args.Exception.InnerException != null)
                Log("  Inner: " + args.Exception.InnerException.Message);
            // M3: inform the user instead of silently swallowing the exception.
            ErrorGuard.Report("Dispatcher", args.Exception);
            args.Handled = true;
        };

        Log("=== Techtoon Accounting starting ===");

        try
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connStr = Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

            var services = new ServiceCollection();
            services.AddDbContext<TechtoonDbContext>(options =>
                options.UseNpgsql(connStr, npgsql =>
                {
                    npgsql.CommandTimeout(60);
                    npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                }));
            Services = services.BuildServiceProvider();

            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
                var canConnect = db.Database.CanConnect();
                Log("CanConnect: " + canConnect);
                if (canConnect)
                {
                    await AuthService.EnsureDefaultAdminAsync();
                    Log("Default admin ensured");

                    // نسخ احتياطي تلقائي (خلفية - لا يحجب الواجهة)
                    _ = System.Threading.Tasks.Task.Run(() =>
                    {
                        try { Techtoon.UI.Services.AutoBackupScheduler.CheckAndBackupIfNeeded(connStr); }
                        catch (Exception backupEx) { App.Log("Background backup failed: " + backupEx.Message); }
                    });
                }
            }

            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
            Log("MainWindow shown");
        }
        catch (Exception ex)
        {
            Log("FATAL: " + ex);
            Log("  Type: " + ex.GetType().Name);
            Log("  Message: " + ex.Message);
            Log("  Stack: " + ex.StackTrace);
            if (ex.InnerException != null)
                Log("  Inner: " + ex.InnerException.Message);
            Shutdown(1);
        }
        
    }
}