using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using HotspotService.Models;
using HotspotService.Services;

namespace HotspotService.Automation;

[ActionInfo(PluginIds.SetGuardTargetAction, "调整守护设置", PluginIds.WifiGlyph, true, PluginIds.ActionMenuGroup)]
public sealed class SetGuardTargetAction : ActionBase<SetGuardTargetActionSettings>
{
    private readonly HotspotGuardCoordinator _coordinator;

    public SetGuardTargetAction(HotspotGuardCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    protected override async Task OnInvoke()
    {
        await base.OnInvoke();
        await _coordinator.SetGuardTargetAsync(Settings.Target, applyImmediately: true, cancellationToken: InterruptCancellationToken);
    }
}
