using BlazorLogComponent.Interfaces;
using BlazorLogComponent.Services;
using BlazorLogComponent.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Z21Client;
using Z21Dashboard.Application.Interfaces;
using Z21Dashboard.Helpers;
using Z21Dashboard.Services;
using WinRT.Interop;

#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
#endif

namespace Z21Dashboard;

public static class MauiProgram
{
    private static Mutex? _mutex;
    public static MauiApp CreateMauiApp()
    {
#if WINDOWS
        //
        // The Z21Dashboard application is only allowed to run in one instance.
        // The JSON settings file will be messed up if several instances are running,
        // and there is no good reason to run more than one instance.
        //
        const string mutexName = "Z21Dashboard_SingleInstance_Mutex";

        _mutex = new Mutex(true, mutexName, out bool createdNew);
        if (!createdNew)
        {
            Environment.Exit(0);
        }

        // Manage the WebView2 folder for temp files:
        // Find the path to the user's AppData folder
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Create a subfolder for the user
        var userDataFolder = Path.Combine(appDataPath, "Z21DashboardApp", "WebView2Data");

        // Make sure that this folder exists
        Directory.CreateDirectory(userDataFolder);

        // Tell WebView2 where to store temp files
        Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", userDataFolder);
#endif

        var builder = MauiApp.CreateBuilder();
        builder.Services.AddLocalization(options => options.ResourcesPath = string.Empty);

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("FluentSystemIcons-Regular.ttf", "FluentSystemIcons");
            })
            .ConfigureLifecycleEvents(events =>
            {
#if WINDOWS
                // This section ONLY handles platform-specific window appearance.
                // All application shutdown logic (saving data, disconnecting) is handled in App.xaml.cs.
                events.AddWindows(windows => windows
                    .OnWindowCreated(window =>
                    {
                        IntPtr nativeWindowHandle = WindowNative.GetWindowHandle(window);
                        WindowId nativeWindowId = Win32Interop.GetWindowIdFromWindow(nativeWindowHandle);
                        AppWindow appWindow = AppWindow.GetFromWindowId(nativeWindowId);

                        const string KeyIsMaximized = "IsWindowMaximized";
                        const string KeyLeft = "WindowLeft";
                        const string KeyTop = "WindowTop";
                        const string KeyWidth = "WindowWidth";
                        const string KeyHeight = "WindowHeight";

                        // --- Restore window state ---
                        bool isMaximized = Preferences.Get(KeyIsMaximized, false);
                        if (appWindow.Presenter is OverlappedPresenter presenter)
                        {
                            if (isMaximized)
                            {
                                presenter.Maximize();
                            }
                            else
                            {
                                // Restore previous size and position if saved
                                var left = Preferences.Get(KeyLeft, double.NaN);
                                var top = Preferences.Get(KeyTop, double.NaN);
                                var width = Preferences.Get(KeyWidth, double.NaN);
                                var height = Preferences.Get(KeyHeight, double.NaN);

                                if (!double.IsNaN(left) &&
                                    !double.IsNaN(top) &&
                                    !double.IsNaN(width) &&
                                    !double.IsNaN(height))
                                {
                                    // We ignore the result, as it is not needed. The purpose is to get the 
                                    // Super Maximize logic to calculate the correct bounds for the current monitor configuration.
                                    _ = SuperMaximizeForWindows.GetSuperMaxBounds();

                                    presenter.Restore(); // ensure state is normal
                                    appWindow.MoveAndResize(new Windows.Graphics.RectInt32(
                                        (int)left, (int)top, (int)width, (int)height));
                                }
                            }
                        }

                        // --- Save state when window closes ---
                        window.Closed += (sender, args) =>
                        {
                            if (appWindow.Presenter is OverlappedPresenter p)
                            {
                                Preferences.Set(KeyIsMaximized, p.State == OverlappedPresenterState.Maximized);

                                // Save position and size only if restored
                                if (p.State == OverlappedPresenterState.Restored)
                                {
                                    var bounds = appWindow.Position;
                                    Preferences.Set(KeyLeft, bounds.X);
                                    Preferences.Set(KeyTop, bounds.Y);
                                    Preferences.Set(KeyWidth, appWindow.Size.Width);
                                    Preferences.Set(KeyHeight, appWindow.Size.Height);
                                }
                            }
                        };
                    }));
#endif
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        // 1. Register the singleton logging service from your new component library.
        //    This makes ILoggingService available for injection throughout the app.
        builder.Services.AddSingleton<ILoggingService, LoggingService>();

        // 2. Add the custom logger provider to the .NET logging pipeline.
        //    This tells the logging framework to send all log messages
        //    to our InMemoryLoggerProvider.
        builder.Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.Services.AddSingleton<ILoggerProvider, InMemoryLoggerProvider>();
        });

        // --- SERVICE REGISTRATION SECTION ---

        // Register the client for communicating with the Z21 command station.
        builder.Services.AddSingleton<IZ21UdpClient, Z21UdpClient>();
        builder.Services.AddSingleton<IZ21Client, Z21Client.Z21Client>();

        // Register the generic data persistence service.
        builder.Services.AddSingleton<IAppDataService, AppDataService>();

        // Register the service for managing user-defined locomotive names.
        builder.Services.AddSingleton<ILocoMetadataService, LocoMetadataService>();

        // Register the service for managing the dynamic dashboard layout.
        builder.Services.AddSingleton<IDashboardStateService, DashboardStateService>();

        // Register the operating time service. It depends on IAppDataService and IZ21Client.
        builder.Services.AddSingleton<ILocoOperatingTimeService, LocoOperatingTimeService>();

        // Register the turnout counter service for tracking turnout activations.
        builder.Services.AddSingleton<ITurnoutCounterService, TurnoutCounterService>();

        // Register the service for opening documentation files.
        builder.Services.AddSingleton<IDocumentationService, DocumentationService>();

        // Register the TitleBarService for managing title bar text
        builder.Services.AddSingleton<ITitleBarService, TitleBarService>();

        // --- END: SERVICE REGISTRATION SECTION ---


        return builder.Build();
    }
}
