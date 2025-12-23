using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TouchCursor.Support.Local.Helpers;

namespace TouchCursor.Support.UI.Units;

[TemplatePart(Name = PART_TopLeft, Type = typeof(ToggleButton))]
[TemplatePart(Name = PART_TopCenter, Type = typeof(ToggleButton))]
[TemplatePart(Name = PART_TopRight, Type = typeof(ToggleButton))]
[TemplatePart(Name = PART_MiddleLeft, Type = typeof(ToggleButton))]
[TemplatePart(Name = PART_MiddleCenter, Type = typeof(ToggleButton))]
[TemplatePart(Name = PART_MiddleRight, Type = typeof(ToggleButton))]
[TemplatePart(Name = PART_BottomLeft, Type = typeof(ToggleButton))]
[TemplatePart(Name = PART_BottomCenter, Type = typeof(ToggleButton))]
[TemplatePart(Name = PART_BottomRight, Type = typeof(ToggleButton))]
public class OverlayPositionPicker : Control
{
    private const string PART_TopLeft = "PART_TopLeft";
    private const string PART_TopCenter = "PART_TopCenter";
    private const string PART_TopRight = "PART_TopRight";
    private const string PART_MiddleLeft = "PART_MiddleLeft";
    private const string PART_MiddleCenter = "PART_MiddleCenter";
    private const string PART_MiddleRight = "PART_MiddleRight";
    private const string PART_BottomLeft = "PART_BottomLeft";
    private const string PART_BottomCenter = "PART_BottomCenter";
    private const string PART_BottomRight = "PART_BottomRight";

    private ToggleButton? _topLeft;
    private ToggleButton? _topCenter;
    private ToggleButton? _topRight;
    private ToggleButton? _middleLeft;
    private ToggleButton? _middleCenter;
    private ToggleButton? _middleRight;
    private ToggleButton? _bottomLeft;
    private ToggleButton? _bottomCenter;
    private ToggleButton? _bottomRight;

    private bool _isUpdating;

    static OverlayPositionPicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(OverlayPositionPicker),
            new FrameworkPropertyMetadata(typeof(OverlayPositionPicker)));
    }

    public static readonly DependencyProperty SelectedPositionProperty =
        DependencyProperty.Register(
            nameof(SelectedPosition),
            typeof(OverlayPosition),
            typeof(OverlayPositionPicker),
            new FrameworkPropertyMetadata(
                OverlayPosition.BottomRight,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedPositionChanged));

    public OverlayPosition SelectedPosition
    {
        get => (OverlayPosition)GetValue(SelectedPositionProperty);
        set => SetValue(SelectedPositionProperty, value);
    }

    private static void OnSelectedPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OverlayPositionPicker picker)
        {
            picker.UpdateToggleButtons();
        }
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        UnsubscribeEvents();

        _topLeft = GetTemplateChild(PART_TopLeft) as ToggleButton;
        _topCenter = GetTemplateChild(PART_TopCenter) as ToggleButton;
        _topRight = GetTemplateChild(PART_TopRight) as ToggleButton;
        _middleLeft = GetTemplateChild(PART_MiddleLeft) as ToggleButton;
        _middleCenter = GetTemplateChild(PART_MiddleCenter) as ToggleButton;
        _middleRight = GetTemplateChild(PART_MiddleRight) as ToggleButton;
        _bottomLeft = GetTemplateChild(PART_BottomLeft) as ToggleButton;
        _bottomCenter = GetTemplateChild(PART_BottomCenter) as ToggleButton;
        _bottomRight = GetTemplateChild(PART_BottomRight) as ToggleButton;

        SubscribeEvents();
        UpdateToggleButtons();
    }

    private void SubscribeEvents()
    {
        if (_topLeft != null) _topLeft.Checked += (s, e) => OnPositionSelected(OverlayPosition.TopLeft);
        if (_topCenter != null) _topCenter.Checked += (s, e) => OnPositionSelected(OverlayPosition.TopCenter);
        if (_topRight != null) _topRight.Checked += (s, e) => OnPositionSelected(OverlayPosition.TopRight);
        if (_middleLeft != null) _middleLeft.Checked += (s, e) => OnPositionSelected(OverlayPosition.MiddleLeft);
        if (_middleCenter != null) _middleCenter.Checked += (s, e) => OnPositionSelected(OverlayPosition.MiddleCenter);
        if (_middleRight != null) _middleRight.Checked += (s, e) => OnPositionSelected(OverlayPosition.MiddleRight);
        if (_bottomLeft != null) _bottomLeft.Checked += (s, e) => OnPositionSelected(OverlayPosition.BottomLeft);
        if (_bottomCenter != null) _bottomCenter.Checked += (s, e) => OnPositionSelected(OverlayPosition.BottomCenter);
        if (_bottomRight != null) _bottomRight.Checked += (s, e) => OnPositionSelected(OverlayPosition.BottomRight);
    }

    private void UnsubscribeEvents()
    {
        // Events are unsubscribed when template is reapplied
    }

    private void OnPositionSelected(OverlayPosition position)
    {
        if (_isUpdating) return;
        SelectedPosition = position;
    }

    private void UpdateToggleButtons()
    {
        _isUpdating = true;

        SetToggleState(_topLeft, SelectedPosition == OverlayPosition.TopLeft);
        SetToggleState(_topCenter, SelectedPosition == OverlayPosition.TopCenter);
        SetToggleState(_topRight, SelectedPosition == OverlayPosition.TopRight);
        SetToggleState(_middleLeft, SelectedPosition == OverlayPosition.MiddleLeft);
        SetToggleState(_middleCenter, SelectedPosition == OverlayPosition.MiddleCenter);
        SetToggleState(_middleRight, SelectedPosition == OverlayPosition.MiddleRight);
        SetToggleState(_bottomLeft, SelectedPosition == OverlayPosition.BottomLeft);
        SetToggleState(_bottomCenter, SelectedPosition == OverlayPosition.BottomCenter);
        SetToggleState(_bottomRight, SelectedPosition == OverlayPosition.BottomRight);

        _isUpdating = false;
    }

    private void SetToggleState(ToggleButton? button, bool isChecked)
    {
        if (button != null)
        {
            button.IsChecked = isChecked;
        }
    }
}
