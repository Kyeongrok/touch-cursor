using System.Windows;
using System.Windows.Controls;
using TouchCursor.Main.ViewModels;

namespace TouchCursor.Main.UI.Views;

[TemplatePart(Name = PART_ActivationKeyProfilesListBox, Type = typeof(ListBox))]
[TemplatePart(Name = PART_KeyMappingsDataGrid, Type = typeof(DataGrid))]
[TemplatePart(Name = PART_DisableProgsListBox, Type = typeof(ListBox))]
[TemplatePart(Name = PART_EnableProgsListBox, Type = typeof(ListBox))]
public class SettingsWindow : Control
{
    private const string PART_ActivationKeyProfilesListBox = "PART_ActivationKeyProfilesListBox";
    private const string PART_KeyMappingsDataGrid = "PART_KeyMappingsDataGrid";
    private const string PART_DisableProgsListBox = "PART_DisableProgsListBox";
    private const string PART_EnableProgsListBox = "PART_EnableProgsListBox";

    private ListBox? _activationKeyProfilesListBox;
    private DataGrid? _keyMappingsDataGrid;
    private ListBox? _disableProgsListBox;
    private ListBox? _enableProgsListBox;

    static SettingsWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SettingsWindow),
            new FrameworkPropertyMetadata(typeof(SettingsWindow)));
    }

    #region Dependency Properties

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(SettingsWindowViewModel),
            typeof(SettingsWindow),
            new PropertyMetadata(null, OnViewModelChanged));

    public SettingsWindowViewModel? ViewModel
    {
        get => (SettingsWindowViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty HeaderTextProperty =
        DependencyProperty.Register(
            nameof(HeaderText),
            typeof(string),
            typeof(SettingsWindow),
            new PropertyMetadata("Advanced Settings"));

    public string HeaderText
    {
        get => (string)GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    #endregion

    #region Localization Properties

    public static readonly DependencyProperty GeneralTabHeaderProperty =
        DependencyProperty.Register(nameof(GeneralTabHeader), typeof(string), typeof(SettingsWindow), new PropertyMetadata("General"));

    public string GeneralTabHeader
    {
        get => (string)GetValue(GeneralTabHeaderProperty);
        set => SetValue(GeneralTabHeaderProperty, value);
    }

    public static readonly DependencyProperty KeyMappingsTabHeaderProperty =
        DependencyProperty.Register(nameof(KeyMappingsTabHeader), typeof(string), typeof(SettingsWindow), new PropertyMetadata("Key Mappings"));

    public string KeyMappingsTabHeader
    {
        get => (string)GetValue(KeyMappingsTabHeaderProperty);
        set => SetValue(KeyMappingsTabHeaderProperty, value);
    }

    public static readonly DependencyProperty ProgramListsTabHeaderProperty =
        DependencyProperty.Register(nameof(ProgramListsTabHeader), typeof(string), typeof(SettingsWindow), new PropertyMetadata("Program Lists"));

    public string ProgramListsTabHeader
    {
        get => (string)GetValue(ProgramListsTabHeaderProperty);
        set => SetValue(ProgramListsTabHeaderProperty, value);
    }

    #endregion

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Disconnect old handlers
        if (_activationKeyProfilesListBox != null)
            _activationKeyProfilesListBox.SelectionChanged -= OnActivationKeyProfileSelectionChanged;
        if (_keyMappingsDataGrid != null)
        {
            _keyMappingsDataGrid.SelectionChanged -= OnKeyMappingSelectionChanged;
            _keyMappingsDataGrid.MouseDoubleClick -= OnKeyMappingsDoubleClick;
        }

        // Get template parts
        _activationKeyProfilesListBox = GetTemplateChild(PART_ActivationKeyProfilesListBox) as ListBox;
        _keyMappingsDataGrid = GetTemplateChild(PART_KeyMappingsDataGrid) as DataGrid;
        _disableProgsListBox = GetTemplateChild(PART_DisableProgsListBox) as ListBox;
        _enableProgsListBox = GetTemplateChild(PART_EnableProgsListBox) as ListBox;

        // Connect new handlers
        if (_activationKeyProfilesListBox != null)
            _activationKeyProfilesListBox.SelectionChanged += OnActivationKeyProfileSelectionChanged;
        if (_keyMappingsDataGrid != null)
        {
            _keyMappingsDataGrid.SelectionChanged += OnKeyMappingSelectionChanged;
            _keyMappingsDataGrid.MouseDoubleClick += OnKeyMappingsDoubleClick;
        }
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SettingsWindow control)
        {
            control.DataContext = e.NewValue;
        }
    }

    private void OnActivationKeyProfileSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_activationKeyProfilesListBox?.SelectedItem is ActivationKeyProfileViewModel profile)
        {
            if (ViewModel != null)
            {
                ViewModel.SelectedActivationKeyProfile = profile;
                ViewModel.SelectedActivationKeyForMappings = profile.VkCode;
            }
        }
    }

    private void OnKeyMappingSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_keyMappingsDataGrid?.SelectedItem is KeyMappingViewModel mapping && ViewModel != null)
        {
            ViewModel.SelectedKeyMapping = mapping;
        }
    }

    private void OnKeyMappingsDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ViewModel?.EditKeyMappingCommand.CanExecute(null) == true)
        {
            ViewModel.EditKeyMappingCommand.Execute(null);
        }
    }
}
