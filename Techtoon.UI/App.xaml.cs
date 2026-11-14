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

    /// <summary>صحيح عندما يحتاج المستخدم إلى تفعيل البرنامج (يوجّهه للت-tab داخل الإعدادات بعد الدخول).</summary>
    public static bool LicenseActivationRequired { get; set; }

    // السجل في مجلد قابل للكتابة (مجلد بيانات المستخدم عند التثبيت في Program Files)
    private static readonly string LogFile = Techtoon.UI.Services.AppPaths.LogFile;

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

        // P5: فحص الترخيص قبل فتح الواجهة (افتراضيًا: فترة تجريبية 10 أيام).
        // لا نُغلق البرنامج هنا — يجب أن يصل المستخدم إلى «الإعدادات ← تفعيل البرنامج» للاستيراد.
        // في حالة عدم السماح نعرض تحذيرًا غير حاجب ونوجّهه إلى تبويب التفعيل بعد تسجيل الدخول.
        try
        {
            var license = Techtoon.Core.Licensing.LicenseService.GetStatus();
            Log($"LICENSE: state={license.State} type={license.LicenseType} daysLeft={license.DaysLeft} bound={license.IsMachineBound} msg={license.Message}");

            if (license.IsAllowed)
            {
                if (license.State == Techtoon.Core.Licensing.LicenseState.Trial)
                    Log("LICENSE: running in trial mode (" + license.DaysLeft + " days left)");
            }
            else
            {
                LicenseActivationRequired = true;
                Log("LICENSE NOT ALLOWED: " + license.Message);
                var msg = license.Message + "\n\n" +
                           "يمكنك تفعيل البرنامج الآن من داخل البرنامج:\n" +
                           "الإعدادات ← تبويب «تفعيل البرنامج» ← استيراد ملف الترخيص.\n\n" +
                           "البرنامج يعمل حاليًا في وضع محدود حتى يتم التفعيل.";
                MessageBox.Show(msg, "البرنامج غير مفعّل", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception licEx)
        {
            Log("LICENSE check failed: " + licEx.Message);
        }

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
                    // نظام الصلاحيات: ضمان وجود جدول role_permissions والأدوار النظامية
                    // وربط المستخدمين بها. آمن للتكرار.
                    try
                    {
                        RoleService.EnsureSchema();
                        Log("Roles and permissions ready");
                    }
                    catch (Exception roleEx) { Log("Role schema failed: " + roleEx.Message); }

                    await AuthService.EnsureDefaultAdminAsync();
                    Log("Default admin ensured");

                    // تعبئة البيانات المرجعية إن كانت قاعدة البيانات جديدة (حسابات/عملات/خزائن).
                    var seed = await SeedDataService.SeedIfEmptyAsync(connStr);
                    if (seed.Created) Log("Seed: " + seed.Message);

                    // صيانة المخطط: مزامنة sequences المتأخرة (إصلاح غير مدمّر — setval فقط).
                    // تُنفَّذ في الخلفية حتى لا تُبطئ ظهور الواجهة.
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            var sync = await SchemaMaintenanceService.SyncSequencesAsync(connStr);
                            if (sync.Fixed > 0) Log("Schema sequences synced: " + sync.Fixed);
                        }
                        catch (Exception ex) { App.Log("Schema sync failed: " + ex.Message); }
                    });

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