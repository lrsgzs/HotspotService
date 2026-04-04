using HotspotService.Models;

namespace HotspotService.Services;

public sealed class HotspotGuardCoordinator
{
    private readonly IHotspotController _hotspotController;
    private readonly HotspotPluginSettingsStore _settingsStore;
    private readonly HotspotGuardRuntimeState _runtimeState;
    private readonly IGuardStatusNotifier _guardStatusNotifier;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _syncGate = new(1, 1);
    private int _initialized;

    public HotspotGuardCoordinator(
        IHotspotController hotspotController,
        HotspotPluginSettingsStore settingsStore,
        HotspotGuardRuntimeState runtimeState,
        IGuardStatusNotifier guardStatusNotifier,
        TimeProvider timeProvider)
    {
        _hotspotController = hotspotController;
        _settingsStore = settingsStore;
        _runtimeState = runtimeState;
        _guardStatusNotifier = guardStatusNotifier;
        _timeProvider = timeProvider;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1)
        {
            return;
        }

        var targetChanged = _runtimeState.SetGuardTarget(_settingsStore.StartupTarget);
        var guardChanged = _runtimeState.SetGuardEnabled(_settingsStore.AutoStartGuard);
        var syncChanged = await RequestSyncCoreAsync(_runtimeState.GuardEnabled, cancellationToken);

        if (guardChanged || (_runtimeState.GuardEnabled && (targetChanged || syncChanged)))
        {
            _guardStatusNotifier.NotifyGuardStatusChanged();
        }
    }

    public async Task SetGuardEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        var changed = _runtimeState.SetGuardEnabled(enabled);
        var syncChanged = false;

        if (enabled)
        {
            syncChanged = await RequestSyncCoreAsync(true, cancellationToken);
        }

        if (changed || syncChanged)
        {
            _guardStatusNotifier.NotifyGuardStatusChanged();
        }
    }

    public async Task SetGuardTargetAsync(
        GuardTargetState target,
        bool applyImmediately = false,
        CancellationToken cancellationToken = default)
    {
        var changed = _runtimeState.SetGuardTarget(target);
        var syncChanged = false;

        if (applyImmediately)
        {
            syncChanged = await RequestSyncCoreAsync(true, cancellationToken);
        }
        else if (changed && _runtimeState.GuardEnabled)
        {
            syncChanged = await RequestSyncCoreAsync(false, cancellationToken);
        }

        if (changed || syncChanged)
        {
            _guardStatusNotifier.NotifyGuardStatusChanged();
        }
    }

    public Task SetGuardTargetAsync(GuardTargetState target, CancellationToken cancellationToken)
    {
        return SetGuardTargetAsync(target, applyImmediately: false, cancellationToken: cancellationToken);
    }

    public async Task RunPeriodicCheckAsync(CancellationToken cancellationToken)
    {
        if (await RequestSyncCoreAsync(false, cancellationToken))
        {
            _guardStatusNotifier.NotifyGuardStatusChanged();
        }
    }

    public Task RequestSyncAsync(CancellationToken cancellationToken)
    {
        return RequestSyncAsync(forceApply: false, cancellationToken: cancellationToken);
    }

    public async Task RequestSyncAsync(bool forceApply = false, CancellationToken cancellationToken = default)
    {
        if (await RequestSyncCoreAsync(forceApply, cancellationToken))
        {
            _guardStatusNotifier.NotifyGuardStatusChanged();
        }
    }

    private async Task<bool> RequestSyncCoreAsync(bool forceApply, CancellationToken cancellationToken)
    {
        await _syncGate.WaitAsync(cancellationToken);
        try
        {
            return await PerformSyncAsync(forceApply, cancellationToken);
        }
        finally
        {
            _syncGate.Release();
        }
    }

    private async Task<bool> PerformSyncAsync(bool forceApply, CancellationToken cancellationToken)
    {
        var changed = false;

        try
        {
            var actualState = await _hotspotController.GetStateAsync(cancellationToken);
            changed |= _runtimeState.SetLastKnownHotspotState(actualState);
            _runtimeState.SetLastCheckAt(_timeProvider.GetUtcNow());

            if (!forceApply && !_runtimeState.GuardEnabled)
            {
                _runtimeState.SetLastError(null);
                return changed;
            }

            if (actualState == HotspotActualState.Transitioning)
            {
                _runtimeState.SetLastError(null);
                return changed;
            }

            var targetState = _runtimeState.GuardTarget.ToActualState();
            if (actualState != targetState)
            {
                await _hotspotController.SetStateAsync(_runtimeState.GuardTarget, cancellationToken);
                actualState = await _hotspotController.GetStateAsync(cancellationToken);
                changed |= _runtimeState.SetLastKnownHotspotState(actualState);
                _runtimeState.SetLastCheckAt(_timeProvider.GetUtcNow());
            }

            _runtimeState.SetLastError(null);
        }
        catch (Exception ex)
        {
            _runtimeState.SetLastCheckAt(_timeProvider.GetUtcNow());
            _runtimeState.SetLastError(ex.Message);
        }

        return changed;
    }
}
