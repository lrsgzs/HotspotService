namespace HotspotService.Models;

public static class HotspotStateExtensions
{
    public static HotspotActualState ToActualState(this GuardTargetState target)
    {
        return target == GuardTargetState.On ? HotspotActualState.On : HotspotActualState.Off;
    }

    public static string ToDisplayText(this GuardTargetState target)
    {
        return target == GuardTargetState.On ? "开启热点" : "关闭热点";
    }

    public static string ToDisplayText(this HotspotActualState state)
    {
        return state switch
        {
            HotspotActualState.On => "已开启",
            HotspotActualState.Off => "已关闭",
            HotspotActualState.Transitioning => "切换中",
            _ => "未知"
        };
    }
}
