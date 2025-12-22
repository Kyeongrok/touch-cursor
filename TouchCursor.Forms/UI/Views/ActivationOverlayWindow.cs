using System.Windows;
using System.Windows.Controls;

namespace TouchCursor.Forms.UI.Views;

public enum ActivationState
{
    None,
    Waiting,
    Activated
}

public class ActivationOverlayWindow : Window
{
    static ActivationOverlayWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ActivationOverlayWindow),
            new FrameworkPropertyMetadata(typeof(ActivationOverlayWindow)));
    }

    public ActivationOverlayWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Topmost = true;
        ShowInTaskbar = false;
        IsHitTestVisible = false;
        Focusable = false;
        ShowActivated = false;

        // Default position: bottom-right corner
        SetPosition(TouchCursor.Support.Local.Helpers.OverlayPosition.BottomRight);
    }

    public void SetPosition(TouchCursor.Support.Local.Helpers.OverlayPosition position)
    {
        var workArea = SystemParameters.WorkArea;
        const int margin = 20;
        const int windowWidth = 40;
        const int windowHeight = 40;

        switch (position)
        {
            case TouchCursor.Support.Local.Helpers.OverlayPosition.BottomRight:
                Left = workArea.Right - windowWidth - margin;
                Top = workArea.Bottom - windowHeight - margin;
                break;
            case TouchCursor.Support.Local.Helpers.OverlayPosition.BottomLeft:
                Left = workArea.Left + margin;
                Top = workArea.Bottom - windowHeight - margin;
                break;
            case TouchCursor.Support.Local.Helpers.OverlayPosition.TopRight:
                Left = workArea.Right - windowWidth - margin;
                Top = workArea.Top + margin;
                break;
            case TouchCursor.Support.Local.Helpers.OverlayPosition.TopLeft:
                Left = workArea.Left + margin;
                Top = workArea.Top + margin;
                break;
        }
    }

    public static readonly DependencyProperty KeyNameProperty =
        DependencyProperty.Register(nameof(KeyName), typeof(string),
            typeof(ActivationOverlayWindow), new PropertyMetadata("Space"));

    public string KeyName
    {
        get => (string)GetValue(KeyNameProperty);
        set => SetValue(KeyNameProperty, value);
    }

    public static readonly DependencyProperty StateProperty =
        DependencyProperty.Register(nameof(State), typeof(ActivationState),
            typeof(ActivationOverlayWindow), new PropertyMetadata(ActivationState.None, OnStateChanged));

    public ActivationState State
    {
        get => (ActivationState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ActivationOverlayWindow window)
        {
            var state = (ActivationState)e.NewValue;
            if (state == ActivationState.None)
            {
                window.Hide();
            }
            else
            {
                window.Show();
            }
        }
    }

    // Legacy property for compatibility
    public static readonly DependencyProperty IsActivatedProperty =
        DependencyProperty.Register(nameof(IsActivated), typeof(bool),
            typeof(ActivationOverlayWindow), new PropertyMetadata(false, OnIsActivatedChanged));

    public bool IsActivated
    {
        get => (bool)GetValue(IsActivatedProperty);
        set => SetValue(IsActivatedProperty, value);
    }

    private static void OnIsActivatedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ActivationOverlayWindow window)
        {
            if ((bool)e.NewValue)
            {
                window.State = ActivationState.Activated;
            }
            else
            {
                window.State = ActivationState.None;
            }
        }
    }
}
