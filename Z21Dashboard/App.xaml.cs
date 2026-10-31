using Microsoft.Extensions.Localization;
using Z21Client;
using Z21Dashboard.Locomotives.Interfaces;
using Z21Dashboard.Ui.Interfaces;
using Z21Dashboard.Ui.Services;

namespace Z21Dashboard;

public partial class App : Microsoft.Maui.Controls.Application
{
    private IStringLocalizer _localizer;
    public App(IStringLocalizer<AppXamlResource> localizer)
    {
        _localizer = localizer;
        InitializeComponent();
    }

    /// <summary>
    /// Creates the WinUI3 window for the application.
    /// If the system supports title bar customization, it sets up a custom title bar with
    /// "Super Maximize" functionality.
    /// </summary>
    /// <param name="activationState"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        Window? window = null;
        string winTitle = "Z21Dashboard";

        if (Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
        {
#if WINDOWS
            string subTitle = _localizer["SuperMaxSubTitle"];
            string superMaximizeToolTip = _localizer["SuperMaxToolTip"];
            var titleBar = SuperMaximizeForWindows.BuildTitleBar(winTitle, subTitle, superMaximizeToolTip);

            window = new Window(new MainPage())
            {
                TitleBar = titleBar
            };

            // Listener for the Created event to get the native window
            // as we need the position and size that Windows has set
            window.Created += (sender, eventArgs) =>
            {
                var mauiWindow = sender as Window;
                if (mauiWindow?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
                {
                    SuperMaximizeForWindows.Initialize(nativeWindow);
                }
                else
                {
                    throw new InvalidOperationException("Failed to get native window for Super Maximize initialization.");
                }
            };

            // Hook the cancelable WinUI Closing event so Blazor can veto closing.
            window.Created += OnWindowCreatedHookClose;
#else
            window = new Window(new MainPage()) { Title = winTitle };
#endif
        }
        else
            window = new Window(new MainPage()) { Title = winTitle };

        // Subscribe to the window's Destroying event.
        // This is the correct, robust lifecycle event to handle application shutdown logic.
        window.Destroying += OnWindowDestroying;

        return window;
    }

#if WINDOWS
    private bool _closeApproved;
    private Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;
    private Microsoft.UI.Xaml.Window? _nativeWindow;

    /// <summary>
    /// Hooks the cancelable WinUI <see cref="Microsoft.UI.Windowing.AppWindow.Closing"/> event,
    /// which (unlike MAUI's Destroying) lets us cancel the close and ask Blazor first.
    /// </summary>
    private void OnWindowCreatedHookClose(
        object? sender,
        EventArgs e
        )
    {
        if (((Window)sender!).Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
        {
            _nativeWindow = nativeWindow;
            _dispatcherQueue = nativeWindow.DispatcherQueue;
            nativeWindow.AppWindow.Closing += OnAppWindowClosing;

            var coordinator = this.Handler?.MauiContext?.Services.GetService<IAppCloseCoordinator>();
            coordinator?.ForceCloseRequested += OnForceCloseRequested;
        }
    }

    private async void OnAppWindowClosing(
        Microsoft.UI.Windowing.AppWindow sender,
        Microsoft.UI.Windowing.AppWindowClosingEventArgs args
        )
    {
        if (_closeApproved) // already confirmed — let it close
        {
            return;
        }      
        args.Cancel = true; // always cancel first; re-close programmatically if approved

        var coordinator = this.Handler?.MauiContext?.Services.GetService<IAppCloseCoordinator>();
        if (coordinator is null) 
        {
            _closeApproved = true;
            return; 
        }

        var (canClose, _) = await coordinator.RequestCloseAsync();

        if (canClose)
        {
            _closeApproved = true;
            _nativeWindow?.Close();
        }
        // Vetoed: stay open. MainLayout's dialog (listening to CloseVetoed) shows the reasons.
    }

    /// <summary>
    /// Called when the user chose "close anyway" in the Blazor dialog.
    /// Marshals to the window's UI thread (the call originates from Blazor) and closes.
    /// </summary>
    private void OnForceCloseRequested(
        object? sender,
        EventArgs e
        )
    {
        _closeApproved = true;
        _dispatcherQueue?.TryEnqueue(() => _nativeWindow?.Close());
    }
#endif

    /// <summary>
    /// This method is called when the application window is about to be closed.
    /// It's the safe place to perform cleanup and save data using the correct service provider.
    /// </summary>
    private void OnWindowDestroying(
        object? sender,
        EventArgs e
        )
    {
        // Get the app's running service provider from the handler's context.
        // This gives us the *correct* DI container with the live singleton instances.
        var services = this.Handler?.MauiContext?.Services;
        if (services is null)
        {
            return; 
        }

        // --- MANUALLY DISPOSE SINGLETONS ON SHUTDOWN ---

        // Save operating time data by disposing the service.
        var operatingTimeService = services.GetService<ILocoOperatingTimeService>();
        if (operatingTimeService is IDisposable disposableTimeService)
        {
            disposableTimeService.Dispose();
        }

        // Disconnect the Z21 client gracefully.
        var z21Client = services.GetService<IZ21Client>();
        if (z21Client is not null)
        {
            // Use Task.Run(...).Wait() to ensure the async operation completes
            // before the application process terminates.
            Task.Run(async () => await z21Client.DisconnectAsync()).Wait();
        }
    }
}
