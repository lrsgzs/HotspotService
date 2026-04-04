using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using HotspotService.Services;

namespace HotspotService.Automation;

[ActionInfo(PluginIds.EnableGuardAction, "开启", PluginIds.WifiGlyph, true, PluginIds.ActionMenuGroup)]
public sealed class EnableGuardAction : ActionBase
{
    private readonly HotspotGuardCoordinator _coordinator;

    public EnableGuardAction(HotspotGuardCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    protected override async Task OnInvoke()
    {
        await base.OnInvoke();
        await _coordinator.SetGuardEnabledAsync(true, InterruptCancellationToken);
    }
}
