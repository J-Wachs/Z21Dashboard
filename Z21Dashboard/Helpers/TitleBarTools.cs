using Z21Dashboard.Application.Interfaces;

namespace Z21Dashboard.Helpers;

#if WINDOWS
/// <summary>
/// Provides functionality to synchronize a title bar's title and subtitle with updates from the underlying platform
/// service.
/// </summary>
/// <remarks>This class is intended for internal use to ensure that the application's title bar reflects changes
/// from the platform-specific title bar service. It should be used to set up synchronization before the title bar is
/// displayed or interacted with.</remarks>
public static class TitleBarTools
{
    /// <summary>
    /// Configures the specified title bar to synchronize it's title and subtitle with changes from the underlying
    /// platform service.
    /// </summary>
    /// <remarks>This method attaches event handlers to update the title and subtitle properties of the
    /// provided title bar when corresponding changes occur in the platform-specific title bar service. It should be
    /// called once per title bar instance before use.</remarks>
    /// <param name="titleBar">The title bar instance to set up for synchronization. Cannot be null.</param>
    public static void SetupTitleTextListener(TitleBar titleBar)
    {
        titleBar.HandlerChanged += (sender, eventArgs) =>
        {
            if (titleBar.Handler?.MauiContext?.Services.GetService<ITitleBarService>() is ITitleBarService svc)
            {
                svc.TitleChanged += title =>
                    titleBar.Dispatcher.Dispatch(() =>
                        titleBar.Title = title is null ? string.Empty : title);

                svc.SubtitleChanged += subtitle =>
                    titleBar.Dispatcher.Dispatch(() =>
                        titleBar.Subtitle = subtitle is null ? string.Empty : subtitle);
            }
            else
            {
                // Looks like you forgot to register the service
                throw new ArgumentException($"Could not find {nameof(ITitleBarService)} in the service collection.");
            }
        };
    }

    /// <summary>
    /// Creates a title bar button view with the specified text, optional tooltip, and click action.
    /// </summary>
    /// <remarks>The returned button is styled and behaves as a title bar button (simulated button). The click action,
    /// if provided, is invoked each time the button is tapped. The caller is responsible for adding the returned view
    /// to the visual tree.</remarks>
    /// <param name="buttonIcon">The icon to display on the button. This value is required and cannot be null or empty.</param>
    /// <param name="toolTip">The tooltip text to display when the user hovers over the button, or null to omit the tooltip.</param>
    /// <param name="onClickAction">An action to invoke when the button is clicked. The action receives the button's border element as a parameter.
    /// If null, no click behavior is attached.</param>
    /// <returns>An object that implements <see cref="IView"/> representing the configured title bar button.</returns>
    public static IView CreateTitleBarButton(string buttonIcon, string? toolTip, Action<Border?> onClickAction)
    {
        var simulatedButton = BuildTitleBarButton(buttonIcon);

        SetupPresentationBehavior(simulatedButton);

        SetupToolTip(simulatedButton, toolTip);

        if (onClickAction is not null)
        {
            simulatedButton.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => onClickAction(simulatedButton)),
                CommandParameter = simulatedButton
            });
        }

        return simulatedButton;
    }

    /// <summary>
    /// Creates a styled title bar button with the specified text for use in a custom window title bar.
    /// </summary>
    /// <remarks>The returned button uses the 'AppWindowTitleBarButtonForeground' dynamic resource for its
    /// text color and is sized to fit typical title bar button dimensions. The caller is responsible for handling user
    /// interaction and adding the button to the UI.</remarks>
    /// <param name="buttonIcon">The icon to display on the title bar button. This value is shown centered within the button.</param>
    /// <returns>A <see cref="Border"/> containing a centered label with the specified text, styled for use as a title bar
    /// button.</returns>
    private static Border BuildTitleBarButton(string buttonIcon)
    {
        var label = new Label
        {
            Text = buttonIcon,
            FontFamily = "FluentSystemIcons",
            FontSize = 16,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center
        };
        label.SetDynamicResource(Label.TextColorProperty, "AppWindowTitleBarButtonForeground");

        return new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Colors.Transparent,
            WidthRequest = 46,
            HeightRequest = 32,
            Content = label
        };
    }

    /// <summary>
    /// Configures visual state behaviors for the specified UI element to provide visual feedback for normal, hover, and
    /// pressed states.
    /// </summary>
    /// <remarks>This method sets up a VisualStateGroup named "CommonStates" on the provided UI element,
    /// enabling it to visually respond to pointer interactions such as hovering and pressing. The method uses system
    /// resources for hover and pressed states when available, providing a consistent look with the application's title
    /// bar. If the required resources are not found, fallback colors are used. This setup is typically used to enhance
    /// the user experience by providing immediate visual feedback on interactive elements.</remarks>
    /// <param name="uiElement">The UI element to which the visual state behaviors will be applied. Must not be null.</param>
    private static void SetupPresentationBehavior(View uiElement)
    {
        // Create Visual States for Hover and Normal
        var commonGroup = new VisualStateGroup { Name = "CommonStates" };

        // 1. Normal State
        var normalState = new VisualState { Name = "Normal" };
        normalState.Setters.Add(new Setter { Property = Button.BackgroundColorProperty, Value = Colors.Transparent });

        // 2. PointerOver (Hover) State
        var pointerOverState = new VisualState { Name = "PointerOver" };

        // Use Windows system-constant for title bar-hover
        if (Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue("AppWindowTitleBarButtonHoverFill", out var hoverBrush))
        {
            pointerOverState.Setters.Add(new Setter { Property = Button.BackgroundProperty, Value = hoverBrush });
        }
        else
        {
            // Fallback if the resource does not exists
            pointerOverState.Setters.Add(new Setter { Property = Button.BackgroundColorProperty, Value = Colors.Gainsboro });
        }

        // 3. Pressed State (when user clicks)
        var pressedState = new VisualState { Name = "Pressed" };
        if (Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue("AppWindowTitleBarButtonPressedFill", out var pressedBrush))
        {
            pressedState.Setters.Add(new Setter { Property = Button.BackgroundProperty, Value = pressedBrush });
        }

        commonGroup.States.Add(normalState);
        commonGroup.States.Add(pointerOverState);
        commonGroup.States.Add(pressedState);

        VisualStateManager.SetVisualStateGroups(uiElement, [commonGroup]);
    }

    /// <summary>
    /// Attaches a tooltip to the specified UI element if a non-empty tooltip string is provided.
    /// </summary>
    /// <remarks>This method subscribes to the HandlerChanged event of the UI element to ensure the tooltip is
    /// set when the platform view becomes available. Tooltips are only attached on platforms where the platform view is
    /// a Microsoft.UI.Xaml.FrameworkElement.</remarks>
    /// <param name="uiElement">The UI element to which the tooltip will be attached. Must not be null.</param>
    /// <param name="toolTip">The text to display in the tooltip. If null or empty, no tooltip is attached.</param>
    /// <exception cref="ArgumentException">Thrown if the UI element's platform view is null or not a FrameworkElement when attempting to set the tooltip.</exception>
    private static void SetupToolTip(View uiElement, string? toolTip)
    {
        if (string.IsNullOrEmpty(toolTip) is false)
        {
            uiElement.HandlerChanged += (sender, eventArg) =>
            {
                if (uiElement.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement platformView)
                {
                    // The tooltip-object
                    var tooltip = new Microsoft.UI.Xaml.Controls.ToolTip
                    {
                        Content = toolTip,
                        Placement = Microsoft.UI.Xaml.Controls.Primitives.PlacementMode.Bottom,
                    };

                    Microsoft.UI.Xaml.Controls.ToolTipService.SetToolTip(platformView, tooltip);
                }
                else
                {
                    throw new ArgumentException("Could not set tooltip - PlatformView is null or not a FrameworkElement.");
                }
            };
        }
    }
}
#endif
