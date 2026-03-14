using Microsoft.UI;
using Microsoft.UI.Windowing;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT.Interop;

namespace Z21Dashboard.Helpers;

/// <summary>
/// Builds a Windows-specific title bar with a Super Maximize button and the specified title and subtitle.
/// </summary>
/// <remarks>The Super Maximize button allows users to toggle a custom maximize mode for the window. This method
/// is intended for use on Windows platforms and relies on platform-specific services. The returned title bar includes
/// the provided title, subtitle, and a trailing button with the specified tooltip.</remarks>
#if WINDOWS
public static class SuperMaximizeForWindows
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    private static nint _windowHandle;
    private static AppWindow? _appWindow;
    private static RectInt32 _originalBounds;
    private static RectInt32 superMaxBounds;

    /// <summary>
    /// Creates a new title bar with the specified title, subtitle, and a trailing button for toggling the super
    /// maximize state.
    /// </summary>
    /// <remarks>The trailing button in the title bar allows users to toggle the super maximize state of the
    /// window. The title bar will automatically update its title text if the underlying data changes.</remarks>
    /// <param name="title">The main text to display as the title in the title bar. Cannot be null.</param>
    /// <param name="subTitle">The secondary text to display as the subtitle in the title bar. Can be null or empty if no subtitle is needed.</param>
    /// <param name="superMaximizeToolTip">The tooltip text to display when hovering over the super maximize button. null to omit the tooltip.</param>
    /// <returns>A new instance of the TitleBar control configured with the provided title, subtitle, and a super maximize
    /// button.</returns>
    public static TitleBar BuildTitleBar(string title, string subTitle, string? superMaximizeToolTip)
    {
        //                                               Expand icon
        var button = TitleBarTools.CreateTitleBarButton($"\uEE8B", superMaximizeToolTip, PerformSuperMaximize);

        var titleBar = new TitleBar
        {
            Title = title,
            Subtitle = subTitle,
            TrailingContent = button
        };

        // Setup listener to update title text changes when change is initiated
        // from the Blazor part of the applicaiton
        TitleBarTools.SetupTitleTextListener(titleBar);

        return titleBar;
    }

    /// <summary>
    /// Initializes the component using the specified native window instance.
    /// </summary>
    /// <param name="nativeWindow">The native window to associate with the component. Cannot be null.</param>
    public static void Initialize(Microsoft.UI.Xaml.Window nativeWindow)
    {
        _windowHandle = WindowNative.GetWindowHandle(nativeWindow);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(_windowHandle);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        _originalBounds = new RectInt32(
            _appWindow.Position.X,
            _appWindow.Position.Y,
            _appWindow.Size.Width,
            _appWindow.Size.Height
        );
    }

    /// <summary>
    /// Get the current monitor setup's max size for the window,
    /// which is the size of the virtual desktop, including all monitors, minus the taskbar.
    /// 
    /// It is a prerequisite that the method 'Initialize' has been called before this method,
    /// otherwise it will return null.
    /// </summary>
    /// <returns></returns>
    public static RectInt32? GetSuperMaxBounds()
    {
        if (_appWindow is null){
            return null;
        }

        // Set OverlappedPresenter and keep TitleBar visible
        _appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            // This is to fix an issue where the window will not redraw correctly
            // when going from Maximized to SuperMaximized
            if (presenter.State is OverlappedPresenterState.Maximized)
            {
                presenter.Restore();
            }
        }

        // Get the extended frame that surround the window
        var frame = GetExtendedFrameBounds(_windowHandle);

        RectInt32 screenVirtualSpace = CalculateTotalScreenArea();

        // Calculate offsets to account for window frame
        int offsetLeft = frame.Left - _appWindow.Position.X;
        int offsetTop = frame.Top - _appWindow.Position.Y;
        int offsetRight = (_appWindow.Position.X + _appWindow.Size.Width) - frame.Right;
        int offsetBottom = (_appWindow.Position.Y + _appWindow.Size.Height) - frame.Bottom;

        // The final bounds are calcualted and set
        superMaxBounds = new RectInt32(screenVirtualSpace.X - offsetLeft,
            screenVirtualSpace.Y - offsetTop,
            screenVirtualSpace.Width + offsetLeft + offsetRight,
            screenVirtualSpace.Height + offsetTop + offsetBottom);

        return superMaxBounds;
    }

    /// <summary>
    /// Sets the internal super maximum bounds to the current value returned by GetSuperMaxBounds, if available.
    /// </summary>
    /// <remarks>The reason for this method is that 'Super Maximise' is not a build-in method in Windows, hence it must
    /// be handled manually. By calling this metode the Super Maximize functionality will handle going back to 
    /// 'normal' (Restored mode) as expected.
    /// 
    /// This method updates the internal super maximum bounds only if GetSuperMaxBounds returns a
    /// non-null value. It does not throw exceptions if the value is null, and leaves the existing bounds unchanged in
    /// that case.</remarks>
    public static void SetInternalSuperMaxBounds()
    {
        var localSuperMaxBounds = GetSuperMaxBounds();
        if (localSuperMaxBounds is not null)
        {
            superMaxBounds = (RectInt32)localSuperMaxBounds;
        }
    }

    /// <summary>
    /// Toggles the window between its current state and the super maximized state.
    /// </summary>
    /// <remarks>A super maximized window occupies the available screen space acress screeens,
    /// provided that the screens are set for extend of the desktop. Calling this method when the window is already super
    /// maximized will restore it to its previous size and position.</remarks>
    /// <param name="myButton">The button that triggered the action. This parameter is not used in the method's logic but
    /// required in CreateButton.</param>
    private static void PerformSuperMaximize(Border? myButton)
    {
        if (_appWindow is null)
        {
            return;
        }

        // Figure out if we are maximized or not
        if (superMaxBounds.X != _appWindow.Position.X ||
            superMaxBounds.Y != _appWindow.Position.Y ||
            superMaxBounds.Width != _appWindow.Size.Width ||
            superMaxBounds.Height != _appWindow.Size.Height)
        {
            SuperMaximize();
        }
        else
        {
            Restore();
        }
    }

    /// <summary>
    /// Maximizes the application window to fill the entire screen, including all monitors, while preserving the
    /// window's title bar and borders.
    /// </summary>
    /// <remarks>This method adjusts the window's size and position so that it covers the full virtual desktop
    /// area, which may span multiple monitors. The original window bounds are saved to allow restoration if needed. The
    /// window remains resizable, minimizable, and maximizable after this operation.</remarks>
    private static void SuperMaximize()
    {
        if (_appWindow == null)
        {
            return;
        }

        SaveWindowsCurrentBounds();

        var localSuperMaxBounds = GetSuperMaxBounds();
        if (localSuperMaxBounds is not null)
        {
            superMaxBounds = (RectInt32)localSuperMaxBounds;
        }
        else
        {
            return;
        }

        // Move and resize the window
        _appWindow.MoveAndResize(superMaxBounds);
    }

    /// <summary>
    /// Calculates the bounding rectangle that encompasses the work areas of all connected displays, excluding areas
    /// occupied by system taskbars.
    /// </summary>
    /// <remarks>The returned rectangle is based on the union of the work areas of all detected displays,
    /// which may not be contiguous if displays are arranged with gaps. The work area excludes regions reserved by the
    /// operating system, such as taskbars or docked toolbars.</remarks>
    /// <returns>A <see cref="RectInt32"/> representing the total virtual screen area available for application windows across
    /// all displays.</returns>
    private static RectInt32 CalculateTotalScreenArea()
    {
        // Get all screencs
        var allDisplays = DisplayArea.FindAll();

        // Initialize virtuel coordinates
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        // For each screen, find Work Area without the Taskbar
        for (int i = 0; i < allDisplays.Count; i++)
        {
            var display = allDisplays[i];
            var area = display.WorkArea;

            if (area.X < minX) minX = area.X;
            if (area.Y < minY) minY = area.Y;

            int right = area.X + area.Width;
            int bottom = area.Y + area.Height;

            if (right > maxX) maxX = right;
            if (bottom > maxY) maxY = bottom;
        }

        // Calculate the complete width and height
        int totalVirtualWidth = maxX - minX;
        int totalVirtualHeight = maxY - minY;

        return new RectInt32(minX, minY, totalVirtualWidth, totalVirtualHeight);
    }

    /// <summary>
    /// Saves the current position and size of the application window for later use.
    /// </summary>
    /// <remarks>This method does not perform any action if the application window is not available. The saved
    /// bounds can be used to restore the window to its previous state.</remarks>
    private static void SaveWindowsCurrentBounds()
    {
        if (_appWindow is null)
        {
            return;
        }
        // Get current bounds
        _originalBounds = new RectInt32(
            _appWindow.Position.X,
            _appWindow.Position.Y,
            _appWindow.Size.Width,
            _appWindow.Size.Height
        );
    }

    /// <summary>
    /// Restores the application window to its original size and state, enabling standard window controls such as
    /// resizing, minimizing, and maximizing.
    /// </summary>
    /// <remarks>This method is only effective on Windows platforms. It re-enables the window's border and
    /// title bar, and restores the window's ability to be resized, minimized, and maximized. The window is moved and
    /// resized to its previously stored bounds.</remarks>
    private static void Restore()
    {
        if (_appWindow is null)
        {
            return;
        }

        _appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        _appWindow.MoveAndResize(_originalBounds);
    }

    /// <summary>
    /// Retrieves the extended frame bounds of the specified window.
    /// </summary>
    /// <remarks>The extended frame bounds include the area occupied by the window's drop shadow and other
    /// non-client rendering effects. This method requires that Desktop Window Manager (DWM) composition is enabled on
    /// the system.</remarks>
    /// <param name="hwnd">A handle to the window for which to retrieve the extended frame bounds.</param>
    /// <returns>A RECT structure that contains the extended frame bounds of the specified window, in screen coordinates.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the underlying DwmGetWindowAttribute call fails.</exception>
    private static RECT GetExtendedFrameBounds(IntPtr hwnd)
    {
        int hr = DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT rect, Marshal.SizeOf<RECT>());
        if (hr is not 0)
        {
            throw new InvalidOperationException($"DwmGetWindowAttribute failed: {hr}");
        }

        return rect;
    }
}
#endif
