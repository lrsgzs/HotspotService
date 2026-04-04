using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Controls;
using HotspotService.Models;
using HotspotService.Settings;

namespace HotspotService.Automation;

public sealed class SystemHotspotStateRuleSettingsControl : RuleSettingsControlBase<SystemHotspotStateRuleSettings>
{
    private readonly IReadOnlyList<OptionItem<HotspotActualState>> _options =
    [
        new("已开启", HotspotActualState.On),
        new("已关闭", HotspotActualState.Off)
    ];

    private readonly ComboBox _comboBox;
    private bool _updatingUi;

    public SystemHotspotStateRuleSettingsControl()
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(0, 12, 0, 0),
            Spacing = 8
        };
        panel.Children.Add(new TextBlock
        {
            Text = "匹配状态"
        });

        _comboBox = new ComboBox
        {
            ItemsSource = _options,
            HorizontalAlignment = HorizontalAlignment.Left,
            MinWidth = 220
        };
        _comboBox.SelectionChanged += (_, _) =>
        {
            if (_updatingUi)
            {
                return;
            }

            if (_comboBox.SelectedItem is OptionItem<HotspotActualState> option)
            {
                Settings.ExpectedState = option.Value;
            }
        };
        panel.Children.Add(_comboBox);
        Content = panel;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateUi();
    }

    private void UpdateUi()
    {
        _updatingUi = true;
        try
        {
            _comboBox.SelectedItem = _options.FirstOrDefault(x => x.Value == Settings.ExpectedState) ?? _options[0];
        }
        finally
        {
            _updatingUi = false;
        }
    }
}
