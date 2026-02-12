using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TouchCursor.Main.UI.Views;

[TemplatePart(Name = PART_SearchTextBox, Type = typeof(TextBox))]
[TemplatePart(Name = PART_ProcessListView, Type = typeof(ListView))]
[TemplatePart(Name = PART_OkButton, Type = typeof(Button))]
[TemplatePart(Name = PART_CancelButton, Type = typeof(Button))]
public class ProcessSelectorDialog : Control
{
    private const string PART_SearchTextBox = "PART_SearchTextBox";
    private const string PART_ProcessListView = "PART_ProcessListView";
    private const string PART_OkButton = "PART_OkButton";
    private const string PART_CancelButton = "PART_CancelButton";

    private TextBox? _searchTextBox;
    private ListView? _processListView;
    private Button? _okButton;
    private Button? _cancelButton;

    private readonly ObservableCollection<ProcessInfo> _processes = new();
    private ICollectionView? _processesView;

    public event EventHandler<string?>? OkClicked;
    public event EventHandler? CancelClicked;

    public string? SelectedProcessName =>
        (_processListView?.SelectedItem as ProcessInfo)?.ProcessName;

    static ProcessSelectorDialog()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ProcessSelectorDialog),
            new FrameworkPropertyMetadata(typeof(ProcessSelectorDialog)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_searchTextBox != null)
            _searchTextBox.TextChanged -= OnSearchTextChanged;
        if (_okButton != null)
            _okButton.Click -= OnOkButtonClick;
        if (_cancelButton != null)
            _cancelButton.Click -= OnCancelButtonClick;

        _searchTextBox = GetTemplateChild(PART_SearchTextBox) as TextBox;
        _processListView = GetTemplateChild(PART_ProcessListView) as ListView;
        _okButton = GetTemplateChild(PART_OkButton) as Button;
        _cancelButton = GetTemplateChild(PART_CancelButton) as Button;

        if (_searchTextBox != null)
            _searchTextBox.TextChanged += OnSearchTextChanged;
        if (_okButton != null)
            _okButton.Click += OnOkButtonClick;
        if (_cancelButton != null)
            _cancelButton.Click += OnCancelButtonClick;

        LoadProcesses();

        if (_processListView != null)
        {
            _processListView.ItemsSource = _processes;
            _processesView = CollectionViewSource.GetDefaultView(_processes);
            _processesView.Filter = FilterProcess;
        }
    }

    private void LoadProcesses()
    {
        _processes.Clear();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    var name = process.ProcessName;
                    if (seen.Add(name))
                    {
                        string windowTitle;
                        try { windowTitle = process.MainWindowTitle; }
                        catch { windowTitle = ""; }

                        _processes.Add(new ProcessInfo
                        {
                            ProcessName = name,
                            WindowTitle = windowTitle
                        });
                    }
                }
                catch { /* skip inaccessible processes */ }
                finally { process.Dispose(); }
            }
        }
        catch { /* ignore enumeration errors */ }

        // Sort by process name
        var sorted = _processes.OrderBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase).ToList();
        _processes.Clear();
        foreach (var item in sorted)
            _processes.Add(item);
    }

    private bool FilterProcess(object obj)
    {
        if (obj is not ProcessInfo info) return false;
        var filter = _searchTextBox?.Text;
        if (string.IsNullOrWhiteSpace(filter)) return true;
        return info.ProcessName.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || info.WindowTitle.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _processesView?.Refresh();
    }

    private void OnOkButtonClick(object sender, RoutedEventArgs e)
    {
        OkClicked?.Invoke(this, SelectedProcessName);
    }

    private void OnCancelButtonClick(object sender, RoutedEventArgs e)
    {
        CancelClicked?.Invoke(this, EventArgs.Empty);
    }
}

public class ProcessInfo
{
    public string ProcessName { get; set; } = "";
    public string WindowTitle { get; set; } = "";
}
