using HotspotService.Models;
using HotspotService.Services;

namespace HotspotService.Automation;

public static class GuardEnabledRuleEvaluator
{
    public static bool Evaluate(HotspotGuardRuntimeState runtimeState, GuardEnabledRuleSettings? settings)
    {
        return runtimeState.GuardEnabled == (settings?.ExpectedEnabled ?? true);
    }

    public static bool EvaluateSystemHotspotState(
        HotspotGuardRuntimeState runtimeState,
        SystemHotspotStateRuleSettings? settings)
    {
        return runtimeState.LastKnownHotspotState == (settings?.ExpectedState ?? HotspotActualState.On);
    }

    public static bool EvaluateGuardTarget(
        HotspotGuardRuntimeState runtimeState,
        GuardTargetRuleSettings? settings)
    {
        return runtimeState.GuardTarget == (settings?.ExpectedTarget ?? GuardTargetState.On);
    }
}
