namespace HotspotService.Models;

public sealed class GuardTargetRuleSettings
{
    public GuardTargetState ExpectedTarget { get; set; } = GuardTargetState.On;
}
