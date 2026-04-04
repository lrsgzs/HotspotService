namespace HotspotService.Models;

public sealed class SystemHotspotStateRuleSettings
{
    public HotspotActualState ExpectedState { get; set; } = HotspotActualState.On;
}
