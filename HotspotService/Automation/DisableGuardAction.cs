using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using HotspotService.Services;

namespace HotspotService.Automation;

[ActionInfo(PluginIds.DisableGuardAction, "关闭", PluginIds.WifiGlyph, true, PluginIds.ActionMenuGroup)]
public sealed class DisableGuardAction : ActionBase
{
    private readonly HotspotGuardCoordinator _coordinator;

    public DisableGuardAction(HotspotGuardCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    protected override async Task OnInvoke()
    {
        await base.OnInvoke();
        await _coordinator.SetGuardEnabledAsync(false, InterruptCancellationToken);
    }
}
