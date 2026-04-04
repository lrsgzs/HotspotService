using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ClassIsland.Core.Abstractions.Controls;
using HotspotService.Models;
using HotspotService.Settings;

namespace HotspotService.Automation;

public sealed class SetGuardTargetActionSettingsControl : ActionSettingsControlBase<SetGuardTargetActionSettings>
{
    private readonly IReadOnlyList<OptionItem<GuardTargetState>> _targetOptions =
    [
        new("开启热点", GuardTargetState.On),
        new("关闭热点", GuardTargetState.Off)
    ];

    private readonly ComboBox _comboBox;
    private bool _updatingUi;

    public SetGuardTargetActionSettingsControl()
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(0, 12, 0, 0),
            Spacing = 8
        };
        panel.Children.Add(new TextBlock
        {
            Text = "守护目标"
        });

        _comboBox = new ComboBox
        {
            ItemsSource = _targetOptions,
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
                Settings.Target = option.Value;
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
            var selected = _targetOptions.FirstOrDefault(x => x.Value == Settings.Target) ?? _targetOptions[0];
            _comboBox.SelectedItem = selected;
        }
        finally
        {
            _updatingUi = false;
        }
    }
}
