namespace HotspotService.Models;

public sealed class SetGuardTargetActionSettings
{
    public GuardTargetState Target { get; set; } = GuardTargetState.On;
}
