using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Controls;
using HotspotService.Models;
using HotspotService.Settings;

namespace HotspotService.Automation;

public sealed class GuardTargetRuleSettingsControl : RuleSettingsControlBase<GuardTargetRuleSettings>
{
    private readonly IReadOnlyList<OptionItem<GuardTargetState>> _options =
    [
        new("开", GuardTargetState.On),
        new("关", GuardTargetState.Off)
    ];

    private readonly ComboBox _comboBox;
    private bool _updatingUi;

    public GuardTargetRuleSettingsControl()
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(0, 12, 0, 0),
            Spacing = 8
        };
        panel.Children.Add(new TextBlock
        {
            Text = "匹配目标"
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

            if (_comboBox.SelectedItem is OptionItem<GuardTargetState> option)
            {
                Settings.ExpectedTarget = option.Value;
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
            _comboBox.SelectedItem = _options.FirstOrDefault(x => x.Value == Settings.ExpectedTarget) ?? _options[0];
        }
        finally
        {
            _updatingUi = false;
        }
    }
}
