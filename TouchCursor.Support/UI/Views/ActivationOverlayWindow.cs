using System.Windows;
using System.Windows.Controls;
using TouchCursor.Support.Local.Helpers;

namespace TouchCursor.Support.UI.Views;

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
        Width = 40;
        Height = 40;
        SizeToContent = SizeToContent.Manual;

        SetPosition(OverlayPosition.TopLeft);
    }

    public void SetPosition(OverlayPosition position)
    {
        var workArea = SystemParameters.WorkArea;
        const int margin = 20;
        const int windowWidth = 40;
        const int windowHeight = 40;

        var centerX = workArea.Left + (workArea.Width - windowWidth) / 2;
        var centerY = workArea.Top + (workArea.Height - windowHeight) / 2;

        switch (position)
        {
            case OverlayPosition.TopLeft:
                Left = workArea.Left + margin;
                Top = workArea.Top + margin;
                break;
            case OverlayPosition.TopCenter:
                Left = centerX;
                Top = workArea.Top + margin;
                break;
            case OverlayPosition.TopRight:
                Left = workArea.Right - windowWidth - margin;
                Top = workArea.Top + margin;
                break;
            case OverlayPosition.MiddleLeft:
                Left = workArea.Left + margin;
                Top = centerY;
                break;
            case OverlayPosition.MiddleCenter:
                Left = centerX;
                Top = centerY;
                break;
            case OverlayPosition.MiddleRight:
                Left = workArea.Right - windowWidth - margin;
                Top = centerY;
                break;
            case OverlayPosition.BottomLeft:
                Left = workArea.Left + margin;
                Top = workArea.Bottom - windowHeight - margin;
                break;
            case OverlayPosition.BottomCenter:
                Left = centerX;
                Top = workArea.Bottom - windowHeight - margin;
                break;
            case OverlayPosition.BottomRight:
                Left = workArea.Right - windowWidth - margin;
                Top = workArea.Bottom - windowHeight - margin;
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
            if ((ActivationState)e.NewValue == ActivationState.None)
                window.Hide();
            else
                window.Show();
        }
    }

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
            window.State = (bool)e.NewValue ? ActivationState.Activated : ActivationState.None;
        }
    }
}
