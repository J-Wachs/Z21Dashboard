using Z21Client.Interfaces;
using Z21Dashboard.Application.Interfaces;

namespace Z21Dashboard;

public partial class App : Microsoft.Maui.Controls.Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new MainPage())
        {
            Title = "Z21Dashboard"
        };

        // Subscribe to the window's Destroying event.
        // This is the correct, robust lifecycle event to handle application shutdown logic.
        window.Destroying += OnWindowDestroying;

        return window;
    }

    /// <summary>
    /// This method is called when the application window is about to be closed.
    /// It's the safe place to perform cleanup and save data using the correct service provider.
    /// </summary>
    private void OnWindowDestroying(object? sender, EventArgs e)
    {
        // Get the app's running service provider from the handler's context.
        // This gives us the *correct* DI container with the live singleton instances.
        var services = this.Handler?.MauiContext?.Services;
        if (services is null) return;

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