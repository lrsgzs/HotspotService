using HotspotService.Models;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;

namespace HotspotService.Services;

public sealed class WinRtHotspotController : IHotspotController
{
    public Task<HotspotActualState> GetStateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var manager = CreateManager();
        return Task.FromResult(MapState(manager.TetheringOperationalState));
    }

    public async Task SetStateAsync(GuardTargetState target, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var manager = CreateManager();
        var result = target == GuardTargetState.On
            ? await manager.StartTetheringAsync()
            : await manager.StopTetheringAsync();

        if (result.Status != TetheringOperationStatus.Success)
        {
            throw new InvalidOperationException($"移动热点操作失败：{result.Status}。");
        }
    }

    private static NetworkOperatorTetheringManager CreateManager()
    {
        var profile = NetworkInformation.GetInternetConnectionProfile();
        if (profile is null)
        {
            throw new InvalidOperationException("未找到可用于共享网络的当前连接。");
        }

        var capability = NetworkOperatorTetheringManager.GetTetheringCapabilityFromConnectionProfile(profile);
        if (capability != TetheringCapability.Enabled)
        {
            throw new InvalidOperationException($"当前网络环境不支持热点共享：{capability}。");
        }

        return NetworkOperatorTetheringManager.CreateFromConnectionProfile(profile);
    }

    private static HotspotActualState MapState(TetheringOperationalState state)
    {
        return state switch
        {
            TetheringOperationalState.On => HotspotActualState.On,
            TetheringOperationalState.Off => HotspotActualState.Off,
            TetheringOperationalState.InTransition => HotspotActualState.Transitioning,
            _ => HotspotActualState.Unknown
        };
    }
}
