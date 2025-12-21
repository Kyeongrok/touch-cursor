using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TouchCursor.Main.ViewModels;

namespace TouchCursor.Main.UI.Views;

public class KeyMappingEditor : Control
{
    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;

    static KeyMappingEditor()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(KeyMappingEditor),
            new FrameworkPropertyMetadata(typeof(KeyMappingEditor)));
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(KeyMappingEditorViewModel),
            typeof(KeyMappingEditor),
            new PropertyMetadata(null, OnViewModelChanged));

    public KeyMappingEditorViewModel? ViewModel
    {
        get => (KeyMappingEditorViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KeyMappingEditor control)
        {
            control.DataContext = e.NewValue;
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (ViewModel == null)
            return;

        if (!ViewModel.IsCapturingSource && !ViewModel.IsCapturingTarget)
            return;

        e.Handled = true;

        int vkCode = KeyInterop.VirtualKeyFromKey(e.Key);

        bool shiftPressed = (GetKeyState(VK_SHIFT) & 0x8000) != 0;
        bool ctrlPressed = (GetKeyState(VK_CONTROL) & 0x8000) != 0;
        bool altPressed = (GetKeyState(VK_MENU) & 0x8000) != 0;
        bool winPressed = (GetKeyState(VK_LWIN) & 0x8000) != 0 || (GetKeyState(VK_RWIN) & 0x8000) != 0;

        ViewModel.OnKeyPressed(vkCode, shiftPressed, ctrlPressed, altPressed, winPressed);
    }
}
