# build_infrastructure.py
from pathlib import Path

ROOT = Path(r"D:\PROG\Accounting Prog\Techtoon Accounting C+")
UI = ROOT / "Techtoon.UI"
VM = UI / "ViewModels"
VIEWS = UI / "Views"
SERVICES = UI / "Services"
STYLES = UI / "Styles"
CONVERTERS = UI / "Converters"

for d in [VM, VIEWS, SERVICES, STYLES, CONVERTERS]:
    d.mkdir(parents=True, exist_ok=True)

# ========================================================
# 1. appsettings.json
# ========================================================
(UI / "appsettings.json").write_text("""{
  "ConnectionStrings": {
    "DefaultConnection": "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true;Pooling=true;Maximum Pool Size=20;Timeout=15;CommandTimeout=60"
  }
}
""", encoding="utf-8")

# ========================================================
# 2. App.xaml
# ========================================================
(UI / "App.xaml").write_text("""<Application x:Class="Techtoon.UI.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:Techtoon.UI">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Styles/Colors.xaml"/>
                <ResourceDictionary Source="Styles/Controls.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
""", encoding="utf-8")

# ========================================================
# 3. App.xaml.cs (DI + Config + Exception Handling)
# ========================================================
(UI / "App.xaml.cs").write_text("""using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Data.Context;

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
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Log("UNHANDLED AppDomain: " + ex);
            MessageBox.Show("Unhandled error: " + ex?.Message, "FATAL", MessageBoxButton.OK, MessageBoxImage.Error);
        };
        DispatcherUnhandledException += (s, args) =>
        {
            Log("UNHANDLED Dispatcher: " + args.Exception);
            MessageBox.Show("UI error: " + args.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        Log("=== Techtoon Accounting starting ===");

        try
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connStr = Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection missing");

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
                    await Services.AuthService.EnsureDefaultAdminAsync();
                    Log("Default admin ensured");
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
            MessageBox.Show("Startup failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
""", encoding="utf-8")

# ========================================================
# 4. Styles
# ========================================================
(STYLES / "Colors.xaml").write_text("""<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Color x:Key="PrimaryColor">#009688</Color>
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}"/>
    <SolidColorBrush x:Key="PrimaryLightBrush" Color="#4DB6AC"/>
    <SolidColorBrush x:Key="PrimaryDarkBrush" Color="#00796B"/>

    <Color x:Key="BackgroundColor">#FAFAFA</Color>
    <SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource BackgroundColor}"/>

    <Color x:Key="SurfaceColor">#FFFFFF</Color>
    <SolidColorBrush x:Key="SurfaceBrush" Color="{StaticResource SurfaceColor}"/>

    <Color x:Key="SidebarColor">#263238</Color>
    <SolidColorBrush x:Key="SidebarBrush" Color="{StaticResource SidebarColor}"/>

    <Color x:Key="TextPrimaryColor">#212121</Color>
    <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimaryColor}"/>

    <Color x:Key="TextSecondaryColor">#757575</Color>
    <SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondaryColor}"/>

    <Color x:Key="TextOnPrimaryColor">#FFFFFF</Color>
    <SolidColorBrush x:Key="TextOnPrimaryBrush" Color="{StaticResource TextOnPrimaryColor}"/>

    <Color x:Key="TextOnSidebarColor">#B0BEC5</Color>
    <SolidColorBrush x:Key="TextOnSidebarBrush" Color="{StaticResource TextOnSidebarColor}"/>

    <Color x:Key="SuccessColor">#4CAF50</Color>
    <SolidColorBrush x:Key="SuccessBrush" Color="{StaticResource SuccessColor}"/>

    <Color x:Key="ErrorColor">#F44336</Color>
    <SolidColorBrush x:Key="ErrorBrush" Color="{StaticResource ErrorColor}"/>

    <Color x:Key="WarningColor">#FF9800</Color>
    <SolidColorBrush x:Key="WarningBrush" Color="{StaticResource WarningColor}"/>

</ResourceDictionary>
""", encoding="utf-8")

(STYLES / "Controls.xaml").write_text("""<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Style x:Key="SidebarButton" TargetType="Button">
        <Setter Property="Background" Value="Transparent"/>
        <Setter Property="Foreground" Value="{StaticResource TextOnSidebarBrush}"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Padding" Value="20,12"/>
        <Setter Property="FontSize" Value="14"/>
        <Setter Property="HorizontalContentAlignment" Value="Left"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="border" Background="{TemplateBinding Background}" Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="border" Property="Background" Value="#37474F"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter TargetName="border" Property="Background" Value="{StaticResource PrimaryDarkBrush}"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style x:Key="PrimaryButton" TargetType="Button">
        <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Padding" Value="20,10"/>
        <Setter Property="FontSize" Value="14"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border x:Name="border" Background="{TemplateBinding Background}" CornerRadius="4" Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True">
                            <Setter TargetName="border" Property="Background" Value="{StaticResource PrimaryDarkBrush}"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter TargetName="border" Property="Background" Value="#BDBDBD"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style x:Key="Card" TargetType="Border">
        <Setter Property="Background" Value="{StaticResource SurfaceBrush}"/>
        <Setter Property="CornerRadius" Value="6"/>
        <Setter Property="Padding" Value="20"/>
        <Setter Property="Effect">
            <Setter.Value>
                <DropShadowEffect BlurRadius="15" ShadowDepth="2" Opacity="0.1" Color="Black"/>
            </Setter.Value>
        </Setter>
    </Style>

    <Style x:Key="PageTitle" TargetType="TextBlock">
        <Setter Property="FontSize" Value="24"/>
        <Setter Property="FontWeight" Value="Bold"/>
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
        <Setter Property="Margin" Value="0,0,0,20"/>
    </Style>

    <Style x:Key="CardTitle" TargetType="TextBlock">
        <Setter Property="FontSize" Value="16"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
        <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}"/>
        <Setter Property="Margin" Value="0,0,0,12"/>
    </Style>

    <Style x:Key="StatValue" TargetType="TextBlock">
        <Setter Property="FontSize" Value="28"/>
        <Setter Property="FontWeight" Value="Bold"/>
        <Setter Property="Foreground" Value="{StaticResource PrimaryBrush}"/>
    </Style>

    <Style x:Key="StatLabel" TargetType="TextBlock">
        <Setter Property="FontSize" Value="13"/>
        <Setter Property="Foreground" Value="{StaticResource TextSecondaryBrush}"/>
        <Setter Property="Margin" Value="0,4,0,0"/>
    </Style>

</ResourceDictionary>
""", encoding="utf-8")

# ========================================================
# 5. ViewModelBase + RelayCommand (Infrastructure)
# ========================================================
(VM / "ViewModelBase.cs").write_text("""using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Techtoon.UI.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public virtual Task LoadAsync() => Task.CompletedTask;
}
""", encoding="utf-8")

(VM / "RelayCommand.cs").write_text("""using System.Windows.Input;

namespace Techtoon.UI.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
    public void Execute(object? parameter) => _execute((T?)parameter);
}
""", encoding="utf-8")

# ========================================================
# 6. AuthService
# ========================================================
(SERVICES / "AuthService.cs").write_text("""using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.Services;

public static class AuthService
{
    public static User? CurrentUser { get; private set; }
    public static bool IsLoggedIn => CurrentUser != null;
    public static bool IsAdmin => CurrentUser?.Role == "ADMIN";

    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public User? User { get; set; }
    }

    public static async Task<AuthResult> LoginAsync(string username, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return new AuthResult { Success = false, Message = "Enter username and password" };

            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            if (user == null)
                return new AuthResult { Success = false, Message = "User not found or inactive" };

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return new AuthResult { Success = false, Message = "Wrong password" };

            user.LastLogin = DateTime.UtcNow;
            await db.SaveChangesAsync();

            CurrentUser = user;
            return new AuthResult { Success = true, User = user, Message = "Welcome " + user.FullName };
        }
        catch (Exception ex)
        {
            return new AuthResult { Success = false, Message = "Error: " + ex.Message };
        }
    }

    public static void Logout() => CurrentUser = null;

    public static string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public static async Task EnsureDefaultAdminAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();

            var admin = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            if (admin == null)
            {
                admin = new User
                {
                    Username = "admin",
                    PasswordHash = HashPassword("admin123"),
                    FullName = "System Admin",
                    Role = "ADMIN",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(admin);
                await db.SaveChangesAsync();
                App.Log("Default admin created");
            }
        }
        catch (Exception ex) { App.Log("EnsureDefaultAdminAsync error: " + ex.Message); }
    }
}
""", encoding="utf-8")

# ========================================================
# 7. MainViewModel (Login + Navigation)
# ========================================================
(VM / "MainViewModel.cs").write_text("""using System.Windows.Input;

namespace Techtoon.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase _currentView;
    private string _activeButton = "Dashboard";
    private string _currentUserDisplay = "";
    private bool _isLoggedIn = false;

    private string _username = "";
    private string _password = "";
    private string _errorMessage = "";

    public ViewModelBase CurrentView { get => _currentView; set => SetProperty(ref _currentView, value); }
    public string ActiveButton { get => _activeButton; set => SetProperty(ref _activeButton, value); }
    public string CurrentUserDisplay { get => _currentUserDisplay; set => SetProperty(ref _currentUserDisplay, value); }

    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        set
        {
            if (SetProperty(ref _isLoggedIn, value))
            {
                OnPropertyChanged(nameof(IsLoginScreen));
                OnPropertyChanged(nameof(IsAppVisible));
            }
        }
    }

    public bool IsLoginScreen => !IsLoggedIn;
    public bool IsAppVisible => IsLoggedIn;

    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    public ICommand LoginCommand { get; }
    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }

    public MainViewModel()
    {
        _currentView = new ViewModelBase();
        LoginCommand = new RelayCommand(async () => await LoginAsync());
        NavigateCommand = new RelayCommand<string>(Navigate);
        LogoutCommand = new RelayCommand(Logout);
    }

    private async Task LoginAsync()
    {
        ErrorMessage = "";
        var result = await Services.AuthService.LoginAsync(Username ?? "", Password ?? "");
        if (result.Success)
        {
            if (Services.AuthService.CurrentUser != null)
            {
                var u = Services.AuthService.CurrentUser;
                CurrentUserDisplay = u.FullName + " (" + u.Role + ")";
            }
            IsLoggedIn = true;
            Navigate("Dashboard");
        }
        else
        {
            ErrorMessage = result.Message;
        }
    }

    private void Navigate(string? view)
    {
        if (string.IsNullOrEmpty(view) || !IsLoggedIn) return;
        ActiveButton = view;
        CurrentView = view switch
        {
            "Dashboard" => new DashboardViewModel(),
            _ => new ViewModelBase()
        };
    }

    private void Logout()
    {
        Services.AuthService.Logout();
        IsLoggedIn = false;
        Username = "";
        Password = "";
        ErrorMessage = "";
        CurrentUserDisplay = "";
    }
}
""", encoding="utf-8")

# ========================================================
# 8. DashboardViewModel (Placeholder for now)
# ========================================================
(VM / "DashboardViewModel.cs").write_text("""using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Data.Context;

namespace Techtoon.UI.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private int _invoicesCount;
    private int _partiesCount;
    private int _itemsCount;
    private string _connectionStatus = "Loading...";

    public int InvoicesCount { get => _invoicesCount; set => SetProperty(ref _invoicesCount, value); }
    public int PartiesCount { get => _partiesCount; set => SetProperty(ref _partiesCount, value); }
    public int ItemsCount { get => _itemsCount; set => SetProperty(ref _itemsCount, value); }
    public string ConnectionStatus { get => _connectionStatus; set => SetProperty(ref _connectionStatus, value); }

    public override async Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            InvoicesCount = await db.Invoices.CountAsync();
            PartiesCount = await db.Parties.CountAsync();
            ItemsCount = await db.Items.CountAsync();
            ConnectionStatus = "Connected";
        }
        catch (Exception ex)
        {
            ConnectionStatus = "Error: " + ex.GetType().Name;
            App.Log("Dashboard LoadAsync error: " + ex.Message);
        }
    }
}
""", encoding="utf-8")

# ========================================================
# 9. DashboardView (Simple placeholder)
# ========================================================
(VIEWS / "DashboardView.xaml").write_text("""<UserControl x:Class="Techtoon.UI.Views.DashboardView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Loaded="DashboardView_Loaded">
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="30">
            <TextBlock Text="لوحة التحكم" Style="{StaticResource PageTitle}"/>
            <Border Style="{StaticResource Card}" Margin="0,0,0,20">
                <DockPanel>
                    <TextBlock Text="حالة الاتصال:" FontWeight="SemiBold" VerticalAlignment="Center"/>
                    <TextBlock Text="{Binding ConnectionStatus}" Margin="10,0,0,0" Foreground="{StaticResource PrimaryBrush}" FontWeight="Bold" VerticalAlignment="Center"/>
                </DockPanel>
            </Border>
            <UniformGrid Columns="3" Rows="1" Margin="0,0,0,20">
                <Border Style="{StaticResource Card}" Margin="0,0,10,0">
                    <StackPanel>
                        <TextBlock Text="الفواتير" Style="{StaticResource StatLabel}"/>
                        <TextBlock Text="{Binding InvoicesCount}" Style="{StaticResource StatValue}"/>
                    </StackPanel>
                </Border>
                <Border Style="{StaticResource Card}" Margin="10,0">
                    <StackPanel>
                        <TextBlock Text="العملاء" Style="{StaticResource StatLabel}"/>
                        <TextBlock Text="{Binding PartiesCount}" Style="{StaticResource StatValue}"/>
                    </StackPanel>
                </Border>
                <Border Style="{StaticResource Card}" Margin="10,0,0,0">
                    <StackPanel>
                        <TextBlock Text="الأصناف" Style="{StaticResource StatLabel}"/>
                        <TextBlock Text="{Binding ItemsCount}" Style="{StaticResource StatValue}"/>
                    </StackPanel>
                </Border>
            </UniformGrid>
        </StackPanel>
    </ScrollViewer>
</UserControl>
""", encoding="utf-8")

(VIEWS / "DashboardView.xaml.cs").write_text("""using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class DashboardView : UserControl
{
    public DashboardView() { InitializeComponent(); }

    private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
            await vm.LoadAsync();
    }
}
""", encoding="utf-8")

# ========================================================
# 10. MainWindow.xaml (Login + Sidebar in one window)
# ========================================================
(UI / "MainWindow.xaml").write_text("""<Window x:Class="Techtoon.UI.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:Techtoon.UI.ViewModels"
        xmlns:views="clr-namespace:Techtoon.UI.Views"
        Title="Techtoon Accounting" Height="750" Width="1280"
        WindowStartupLocation="CenterScreen"
        Background="{StaticResource BackgroundBrush}"
        FlowDirection="RightToLeft"
        UseLayoutRounding="True"
        TextOptions.TextFormattingMode="Display">

    <Window.DataContext>
        <vm:MainViewModel/>
    </Window.DataContext>

    <Window.Resources>
        <BooleanToVisibilityConverter x:Key="BoolToVis"/>
        <DataTemplate DataType="{x:Type vm:DashboardViewModel}">
            <views:DashboardView/>
        </DataTemplate>
    </Window.Resources>

    <Grid>
        <!-- LOGIN SCREEN -->
        <Grid Visibility="{Binding IsLoginScreen, Converter={StaticResource BoolToVis}}">
            <Grid.Background>
                <LinearGradientBrush StartPoint="0,0" EndPoint="1,1">
                    <GradientStop Color="#263238" Offset="0"/>
                    <GradientStop Color="#00695C" Offset="1"/>
                </LinearGradientBrush>
            </Grid.Background>
            <Border HorizontalAlignment="Center" VerticalAlignment="Center" Background="White" CornerRadius="8" Padding="40" Width="400">
                <StackPanel>
                    <TextBlock Text="Techtoon" FontSize="36" FontWeight="Bold" Foreground="{StaticResource PrimaryBrush}" HorizontalAlignment="Center"/>
                    <TextBlock Text="Accounting System" FontSize="14" Foreground="{StaticResource TextSecondaryBrush}" HorizontalAlignment="Center" Margin="0,5,0,25"/>
                    <TextBlock Text="تسجيل الدخول" FontSize="20" FontWeight="Bold" Margin="0,0,0,20" HorizontalAlignment="Center"/>
                    <TextBlock Text="اسم المستخدم:" Margin="0,0,0,5"/>
                    <TextBox x:Name="UsernameBox" Padding="10,8" FontSize="14" Margin="0,0,0,15" Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}" KeyDown="UsernameBox_KeyDown"/>
                    <TextBlock Text="كلمة المرور:" Margin="0,0,0,5"/>
                    <PasswordBox x:Name="PasswordBox" Padding="10,8" FontSize="14" Margin="0,0,0,15" KeyDown="PasswordBox_KeyDown"/>
                    <TextBlock Text="{Binding ErrorMessage}" Foreground="{StaticResource ErrorBrush}" Margin="0,0,0,15" TextWrapping="Wrap"/>
                    <Button Content="🔐 تسجيل الدخول" Style="{StaticResource PrimaryButton}" Click="LoginButton_Click" Padding="0,12" FontSize="15" Margin="0,5,0,15"/>
                    <Border Background="#E3F2FD" CornerRadius="4" Padding="15,10">
                        <StackPanel>
                            <TextBlock Text="بيانات الدخول الافتراضية:" FontWeight="SemiBold" Foreground="#1565C0" Margin="0,0,0,5"/>
                            <TextBlock Text="admin / admin123" Foreground="#1565C0" FontSize="13"/>
                        </StackPanel>
                    </Border>
                </StackPanel>
            </Border>
        </Grid>

        <!-- MAIN APP -->
        <Grid Visibility="{Binding IsAppVisible, Converter={StaticResource BoolToVis}}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="240"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <Border Grid.Column="0" Background="{StaticResource SidebarBrush}">
                <DockPanel>
                    <Border DockPanel.Dock="Top" Padding="20,25,20,25" Background="{StaticResource PrimaryDarkBrush}">
                        <StackPanel>
                            <TextBlock Text="Techtoon" Foreground="White" FontSize="22" FontWeight="Bold"/>
                            <TextBlock Text="Accounting System" Foreground="#B2DFDB" FontSize="11"/>
                        </StackPanel>
                    </Border>
                    <Border DockPanel.Dock="Bottom" Background="#1A232A" Padding="15">
                        <StackPanel>
                            <TextBlock Text="👤 المستخدم:" Foreground="#78909C" FontSize="10" Margin="0,0,0,3"/>
                            <TextBlock Text="{Binding CurrentUserDisplay}" Foreground="White" FontSize="12" FontWeight="SemiBold" TextWrapping="Wrap" Margin="0,0,0,10"/>
                            <Button Content="🚪 تسجيل الخروج" Style="{StaticResource SidebarButton}" Command="{Binding LogoutCommand}" HorizontalContentAlignment="Center" Background="#D32F2F" Foreground="White"/>
                            <TextBlock Text="v2.0.0 - C# Edition" Foreground="#546E7A" FontSize="10" Margin="0,10,0,0" HorizontalAlignment="Center"/>
                        </StackPanel>
                    </Border>
                    <ScrollViewer VerticalScrollBarVisibility="Auto">
                        <StackPanel Margin="0,10">
                            <Button Content="  🏠 لوحة التحكم" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Dashboard"/>
                        </StackPanel>
                    </ScrollViewer>
                </DockPanel>
            </Border>

            <ContentControl Grid.Column="1" Content="{Binding CurrentView}"/>
        </Grid>
    </Grid>
</Window>
""", encoding="utf-8")

# ========================================================
# 11. MainWindow.xaml.cs
# ========================================================
(UI / "MainWindow.xaml.cs").write_text("""using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try { if (UsernameBox != null) UsernameBox.Focus(); } catch { }
    }

    private void UsernameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && PasswordBox != null) PasswordBox.Focus();
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) DoLogin();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e) => DoLogin();

    private void DoLogin()
    {
        if (DataContext is MainViewModel vm && PasswordBox != null)
        {
            vm.Password = PasswordBox.Password;
            vm.LoginCommand.Execute(null);
        }
    }
}
""", encoding="utf-8")

# ========================================================
# 12. Ensure appsettings.json is copied to output
# ========================================================
csproj_path = UI / "Techtoon.UI.csproj"
csproj = csproj_path.read_text(encoding="utf-8")
if "appsettings.json" not in csproj:
    csproj = csproj.replace("</Project>", """
  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
    </None>
  </ItemGroup>
</Project>""")
    csproj_path.write_text(csproj, encoding="utf-8")

print("=== Infrastructure built successfully ===")
print("  - appsettings.json")
print("  - App.xaml + App.xaml.cs (DI + Config)")
print("  - Styles (Colors + Controls)")
print("  - ViewModelBase + RelayCommand")
print("  - AuthService (BCrypt + Login)")
print("  - MainViewModel (Login + Navigation)")
print("  - DashboardViewModel + DashboardView")
print("  - MainWindow (Login + Sidebar)")