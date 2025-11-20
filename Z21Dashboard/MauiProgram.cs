using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Z21Dashboard.Application.Interfaces;
using Z21Dashboard.Infrastructure.Z21;
using Z21Dashboard.Services;
using BlazorLogComponent.Interfaces;

using BlazorLogComponent.Services;
using BlazorLogComponent.Logging;



#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
#endif

namespace Z21Dashboard;

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
            })
            .ConfigureLifecycleEvents(events =>
            {
#if WINDOWS
                // This section ONLY handles platform-specific window appearance.
                // All application shutdown logic (saving data, disconnecting) is handled in App.xaml.cs.
                events.AddWindows(windows => windows
                    .OnWindowCreated(window =>
                    {
                        IntPtr nativeWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        WindowId nativeWindowId = Win32Interop.GetWindowIdFromWindow(nativeWindowHandle);
                        AppWindow appWindow = AppWindow.GetFromWindowId(nativeWindowId);

                        // Restore window state using the simple Preferences API.
                        var isMaximized = Preferences.Get("IsWindowMaximized", false);
                        if (isMaximized)
                        {
                            if (appWindow.Presenter is OverlappedPresenter p)
                            {
                                p.Maximize();
                            }
                        }

                        // Handle window closing to save the state.
                        window.Closed += (sender, args) =>
                        {
                            if (appWindow.Presenter is OverlappedPresenter p)
                            {
                                Preferences.Set("IsWindowMaximized", p.State == OverlappedPresenterState.Maximized);
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

        // --- ADD THE FOLLOWING CODE ---

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
        builder.Services.AddSingleton<IZ21Client, Z21Client>();

        // Register the generic data persistence service.
        builder.Services.AddSingleton<IAppDataService, AppDataService>();

        // Register the service for managing user-defined locomotive names.
        builder.Services.AddSingleton<ILocoMetadataService, LocoMetadataService>();

        // Register the service for managing the dynamic dashboard layout.
        builder.Services.AddSingleton<IDashboardStateService, DashboardStateService>();

        // Register the operating time service. It depends on IAppDataService and IZ21Client.
        builder.Services.AddSingleton<ILocoOperatingTimeService, LocoOperatingTimeService>();

        // --- END: SERVICE REGISTRATION SECTION ---

        return builder.Build();
    }
}
