 $ErrorActionPreference = "Stop"
 $root = "D:\PROG\Accounting Prog\Techtoon Accounting C+"
 $mobileDir = Join-Path $root "Techtoon.Mobile"
 $utf8 = New-Object System.Text.UTF8Encoding($false)

function WF($path, $content) {
    [System.IO.File]::WriteAllText($path, $content, $utf8)
    $rel = $path.Replace($root + '\', '')
    Write-Host "  + $rel" -ForegroundColor Green
}

Write-Host "=== E1: MAUI Foundation & Login ===" -ForegroundColor Cyan
Write-Host ""

# 1. Models
Write-Host "[1/6] Creating Auth Models..." -ForegroundColor Yellow
 $models = @'
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Techtoon.Mobile.Models;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DeviceId { get; set; } = "mobile-app";
    public string DeviceName { get; set; } = "Mobile Device";
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}

public class TokenResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;
    
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
    
    [JsonPropertyName("expiresAt")]
    public System.DateTimeOffset ExpiresAt { get; set; }
    
    [JsonPropertyName("user")]
    public UserDto User { get; set; } = new();
}
'@
WF "$mobileDir\Models\AuthModels.cs" $models

# 2. ApiService
Write-Host "[2/6] Creating ApiService..." -ForegroundColor Yellow
 $apiService = @'
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Techtoon.Mobile.Models;

namespace Techtoon.Mobile.Services;

public class ApiService
{
    private const string BaseUrl = "https://techtoon-api.fly.dev/";
    private readonly HttpClient _client;
    private string? _token;

    public ApiService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
        };
        _client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
    }

    public void SetToken(string token)
    {
        _token = token;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public bool IsLoggedIn => !string.IsNullOrEmpty(_token);

    public async Task<TokenResponse?> LoginAsync(string username, string password)
    {
        try
        {
            var request = new LoginRequest
            {
                Username = username,
                Password = password,
                DeviceId = Environment.MachineName,
                DeviceName = "MAUI App"
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("api/auth/login", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseJson);
                if (tokenResponse != null)
                {
                    SetToken(tokenResponse.AccessToken);
                    return tokenResponse;
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> GetDataAsync(string endpoint)
    {
        if (!IsLoggedIn) return "Not logged in";
        
        try
        {
            var response = await _client.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return $"Error: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"Exception: {ex.Message}";
        }
    }
}
'@
WF "$mobileDir\Services\ApiService.cs" $apiService

# 3. LoginPage XAML
Write-Host "[3/6] Creating LoginPage..." -ForegroundColor Yellow
 $loginXaml = @'
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="Techtoon.Mobile.LoginPage"
             Title="تسجيل الدخول"
             FlowDirection="RightToLeft"
             BackgroundColor="#F8F9FA">

    <VerticalStackLayout Spacing="20" Padding="30" VerticalOptions="Center">
        
        <Label Text="Techtoon Accounting"
               FontSize="32"
               FontAttributes="Bold"
               HorizontalOptions="Center"
               TextColor="#2C3E50"
               Margin="0,0,0,20" />

        <Frame BackgroundColor="White" 
               CornerRadius="10" 
               Padding="15" 
               HasShadow="True">
            <Entry x:Name="UsernameEntry"
                   Placeholder="اسم المستخدم"
                   PlaceholderColor="#95A5A6"
                   BackgroundColor="Transparent"
                   TextColor="#2C3E50"
                   FontSize="18" />
        </Frame>

        <Frame BackgroundColor="White" 
               CornerRadius="10" 
               Padding="15" 
               HasShadow="True">
            <Entry x:Name="PasswordEntry"
                   Placeholder="كلمة المرور"
                   PlaceholderColor="#95A5A6"
                   IsPassword="True"
                   BackgroundColor="Transparent"
                   TextColor="#2C3E50"
                   FontSize="18" />
        </Frame>

        <Button x:Name="LoginButton"
                Text="دخول"
                BackgroundColor="#3498DB"
                TextColor="White"
                CornerRadius="10"
                FontSize="18"
                FontAttributes="Bold"
                HeightRequest="50"
                Clicked="OnLoginClicked" />

        <Label x:Name="ErrorLabel"
               TextColor="#E74C3C"
               IsVisible="False"
               HorizontalOptions="Center"
               FontSize="14" />

        <ActivityIndicator x:Name="LoadingIndicator"
                           IsRunning="False"
                           IsVisible="False"
                           Color="#3498DB" />
                           
    </VerticalStackLayout>
    
</ContentPage>
'@
WF "$mobileDir\LoginPage.xaml" $loginXaml

# 4. LoginPage Code-Behind
 $loginCs = @'
using System;
using Techtoon.Mobile.Services;

namespace Techtoon.Mobile;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;

    public LoginPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var username = UsernameEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ErrorLabel.Text = "يرجى إدخال اسم المستخدم وكلمة المرور";
            ErrorLabel.IsVisible = true;
            return;
        }

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        LoginButton.IsEnabled = false;
        ErrorLabel.IsVisible = false;

        try
        {
            var result = await _apiService.LoginAsync(username, password);
            
            if (result != null)
            {
                // Go to Dashboard
                var dashboard = new DashboardPage(_apiService);
                Application.Current!.MainPage = new NavigationPage(dashboard);
            }
            else
            {
                ErrorLabel.Text = "بيانات الدخول غير صحيحة";
                ErrorLabel.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"خطأ: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            LoginButton.IsEnabled = true;
        }
    }
}
'@
WF "$mobileDir\LoginPage.xaml.cs" $loginCs

# 5. DashboardPage XAML + CS
Write-Host "[4/6] Creating DashboardPage..." -ForegroundColor Yellow
 $dashXaml = @'
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="Techtoon.Mobile.DashboardPage"
             Title="الرئيسية"
             FlowDirection="RightToLeft"
             BackgroundColor="#F8F9FA">

    <ScrollView>
        <VerticalStackLayout Spacing="15" Padding="20">
            
            <Label Text="لوحة المعلومات"
                   FontSize="28"
                   FontAttributes="Bold"
                   TextColor="#2C3E50"
                   Margin="0,0,0,10" />

            <Frame BackgroundColor="White" CornerRadius="10" Padding="15" HasShadow="True">
                <VerticalStackLayout Spacing="10">
                    <Label Text="اختبار الاتصال بالـ API" FontSize="18" FontAttributes="Bold" TextColor="#2C3E50"/>
                    <Button x:Name="TestApiButton" Text="جلب بيانات الأصناف" Clicked="OnTestApiClicked" BackgroundColor="#27AE60"/>
                    <Label x:Name="ResultLabel" Text="..." TextColor="#7F8C8D" FontSize="14"/>
                </VerticalStackLayout>
            </Frame>

            <Button x:Name="LogoutButton" Text="تسجيل الخروج" Clicked="OnLogoutClicked" BackgroundColor="#E74C3C" TextColor="White"/>

        </VerticalStackLayout>
    </ScrollView>
    
</ContentPage>
'@
WF "$mobileDir\DashboardPage.xaml" $dashXaml

 $dashCs = @'
using Techtoon.Mobile.Services;

namespace Techtoon.Mobile;

public partial class DashboardPage : ContentPage
{
    private readonly ApiService _apiService;

    public DashboardPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void OnTestApiClicked(object sender, EventArgs e)
    {
        ResultLabel.Text = "جاري التحميل...";
        var result = await _apiService.GetDataAsync("api/items?pageSize=3");
        ResultLabel.Text = result;
    }

    private void OnLogoutClicked(object sender, EventArgs e)
    {
        Application.Current!.MainPage = new LoginPage(_apiService);
    }
}
'@
WF "$mobileDir\DashboardPage.xaml.cs" $dashCs

# 6. Update MauiProgram.cs and App.xaml.cs
Write-Host "[5/6] Updating MauiProgram.cs..." -ForegroundColor Yellow
 $mauiProgram = @'
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Techtoon.Mobile.Services;

namespace Techtoon.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register Services
        builder.Services.AddSingleton<ApiService>();

        return builder.Build();
    }
}
'@
WF "$mobileDir\MauiProgram.cs" $mauiProgram

Write-Host "[6/6] Updating App.xaml.cs..." -ForegroundColor Yellow
 $appCs = @'
using Microsoft.Maui.Controls;
using Techtoon.Mobile.Services;

namespace Techtoon.Mobile;

public partial class App : Application
{
    private readonly ApiService _apiService;

    public App(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
        MainPage = new LoginPage(_apiService);
    }
}
'@
WF "$mobileDir\App.xaml.cs" $appCs

# Delete default MainPage files if they exist
Remove-Item "$mobileDir\MainPage.xaml" -Force -ErrorAction SilentlyContinue
Remove-Item "$mobileDir\MainPage.xaml.cs" -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "=== E1 Setup Complete ===" -ForegroundColor Green
Write-Host "You can now run the MAUI app in Visual Studio 2022" -ForegroundColor Cyan