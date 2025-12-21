using System.Linq;
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
            // Unsubscribe from old ViewModel
            if (e.OldValue is SettingsWindowViewModel oldVm)
            {
                oldVm.AddActivationKeyRequested -= control.OnAddActivationKeyRequested;
            }

            control.DataContext = e.NewValue;

            // Subscribe to new ViewModel
            if (e.NewValue is SettingsWindowViewModel newVm)
            {
                newVm.AddActivationKeyRequested += control.OnAddActivationKeyRequested;
            }
        }
    }

    private ActivationKeyProfileViewModel? OnAddActivationKeyRequested()
    {
        var dialog = new ActivationKeyDialog();
        ActivationKeyProfileViewModel? result = null;

        var window = new Window
        {
            Title = "프로파일 추가",
            Content = dialog,
            Width = 350,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Owner = Window.GetWindow(this)
        };

        dialog.OkClicked += (s, key) =>
        {
            if (key != null)
            {
                // Check for duplicates
                if (ViewModel?.ActivationKeyProfiles.Any(p => p.VkCode == key.VkCode) == true)
                {
                    MessageBox.Show("이 키는 이미 활성화 키로 등록되어 있습니다.", "중복",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                result = new ActivationKeyProfileViewModel
                {
                    VkCode = key.VkCode,
                    KeyName = key.DisplayName,
                    MappingCount = 0
                };
                window.DialogResult = true;
            }
        };

        dialog.CancelClicked += (s, _) =>
        {
            window.DialogResult = false;
        };

        return window.ShowDialog() == true ? result : null;
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
