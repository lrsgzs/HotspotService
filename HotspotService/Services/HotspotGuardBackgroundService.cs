using Microsoft.Extensions.Hosting;

namespace HotspotService.Services;

public sealed class HotspotGuardBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(10);
    private readonly HotspotGuardCoordinator _coordinator;

    public HotspotGuardBackgroundService(HotspotGuardCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _coordinator.InitializeAsync(stoppingToken);

        using var timer = new PeriodicTimer(CheckInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await _coordinator.RunPeriodicCheckAsync(stoppingToken);
        }
    }
}
