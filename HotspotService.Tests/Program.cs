using HotspotService.Automation;
using HotspotService.Models;
using HotspotService.Services;

namespace HotspotService.Tests;

public static class Program
{
    private static readonly List<string> Failures = [];

    public static async Task<int> Main()
    {
        await RunTestAsync("Settings store roundtrip persists values", TestSettingsStoreRoundtripAsync);
        await RunTestAsync("Initialize applies startup target and auto-start sync", TestInitializeAppliesStartupConfigurationAsync);
        await RunTestAsync("Enable guard keeps target and syncs existing target", TestEnableGuardKeepsExistingTargetAsync);
        await RunTestAsync("Disable guard does not change hotspot state", TestDisableGuardDoesNotTouchHotspotAsync);
        await RunTestAsync("Changing target syncs only when guard is enabled", TestChangingTargetSyncsOnlyWhenEnabledAsync);
        await RunTestAsync("Guard-enabled rule evaluates from runtime state", TestGuardEnabledRuleEvaluationAsync);
        await RunTestAsync("Failures are retried and cleared after success", TestFailureRetryAsync);
        await RunTestAsync("Concurrent sync requests do not overlap", TestConcurrentSyncDoesNotOverlapAsync);

        if (Failures.Count == 0)
        {
            Console.WriteLine("All tests passed.");
            return 0;
        }

        Console.Error.WriteLine("Test failures:");
        foreach (var failure in Failures)
        {
            Console.Error.WriteLine($"- {failure}");
        }

        return 1;
    }

    private static async Task RunTestAsync(string name, Func<Task> test)
    {
        try
        {
            await test();
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            Failures.Add($"{name}: {ex.Message}");
            Console.WriteLine($"FAIL {name}");
        }
    }

    private static Task TestSettingsStoreRoundtripAsync()
    {
        var root = CreateTempDirectory();
        try
        {
            var path = Path.Combine(root, "settings.json");
            var first = new HotspotPluginSettingsStore(path)
            {
                AutoStartGuard = true,
                StartupTarget = GuardTargetState.Off
            };

            var second = new HotspotPluginSettingsStore(path);
            AssertTrue(second.AutoStartGuard, "AutoStartGuard should roundtrip as true.");
            AssertEqual(GuardTargetState.Off, second.StartupTarget, "StartupTarget should roundtrip as Off.");
            return Task.CompletedTask;
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static async Task TestInitializeAppliesStartupConfigurationAsync()
    {
        var context = CreateContext(controllerState: HotspotActualState.Off, autoStartGuard: true, startupTarget: GuardTargetState.On);

        await context.Coordinator.InitializeAsync(CancellationToken.None);

        AssertTrue(context.RuntimeState.GuardEnabled, "Guard should be enabled after initialization.");
        AssertEqual(GuardTargetState.On, context.RuntimeState.GuardTarget, "Guard target should come from persisted startup target.");
        AssertEqual(1, context.Controller.StartCallCount, "Initialization should immediately reconcile to the startup target.");
        AssertEqual(HotspotActualState.On, context.RuntimeState.LastKnownHotspotState, "Runtime state should reflect the latest hotspot state.");
        AssertEqual(1, context.Notifier.NotificationCount, "Guard status change should notify the ruleset service once.");
    }

    private static async Task TestEnableGuardKeepsExistingTargetAsync()
    {
        var context = CreateContext(controllerState: HotspotActualState.On, autoStartGuard: false, startupTarget: GuardTargetState.Off);

        await context.Coordinator.InitializeAsync(CancellationToken.None);
        await context.Coordinator.SetGuardEnabledAsync(true, CancellationToken.None);

        AssertTrue(context.RuntimeState.GuardEnabled, "Guard should be enabled.");
        AssertEqual(GuardTargetState.Off, context.RuntimeState.GuardTarget, "Enable should not rewrite the existing guard target.");
        AssertEqual(1, context.Controller.StopCallCount, "Enable should reconcile using the existing target.");
        AssertEqual(1, context.Notifier.NotificationCount, "Enabling the guard should raise one status notification.");
    }

    private static async Task TestDisableGuardDoesNotTouchHotspotAsync()
    {
        var context = CreateContext(controllerState: HotspotActualState.On, autoStartGuard: true, startupTarget: GuardTargetState.On);

        await context.Coordinator.InitializeAsync(CancellationToken.None);
        context.Controller.ResetCounts();

        await context.Coordinator.SetGuardEnabledAsync(false, CancellationToken.None);

        AssertFalse(context.RuntimeState.GuardEnabled, "Guard should be disabled.");
        AssertEqual(0, context.Controller.SetStateCallCount, "Disabling the guard must not touch the hotspot.");
        AssertEqual(2, context.Notifier.NotificationCount, "Initialization and disabling should each notify once.");
    }

    private static async Task TestChangingTargetSyncsOnlyWhenEnabledAsync()
    {
        var enabledContext = CreateContext(controllerState: HotspotActualState.On, autoStartGuard: true, startupTarget: GuardTargetState.On);
        await enabledContext.Coordinator.InitializeAsync(CancellationToken.None);
        enabledContext.Controller.ResetCounts();

        await enabledContext.Coordinator.SetGuardTargetAsync(GuardTargetState.Off, CancellationToken.None);

        AssertEqual(GuardTargetState.Off, enabledContext.RuntimeState.GuardTarget, "Changing target should update runtime target.");
        AssertEqual(1, enabledContext.Controller.StopCallCount, "Changing target should reconcile immediately when enabled.");

        var disabledContext = CreateContext(controllerState: HotspotActualState.On, autoStartGuard: false, startupTarget: GuardTargetState.On);
        await disabledContext.Coordinator.InitializeAsync(CancellationToken.None);
        disabledContext.Controller.ResetCounts();

        await disabledContext.Coordinator.SetGuardTargetAsync(GuardTargetState.Off, CancellationToken.None);

        AssertEqual(GuardTargetState.Off, disabledContext.RuntimeState.GuardTarget, "Changing target should still update runtime target while disabled.");
        AssertEqual(0, disabledContext.Controller.SetStateCallCount, "Changing target while disabled must not touch the hotspot.");
    }

    private static Task TestGuardEnabledRuleEvaluationAsync()
    {
        var runtimeState = new HotspotGuardRuntimeState();
        runtimeState.SetGuardEnabled(true);

        var enabled = GuardEnabledRuleEvaluator.Evaluate(runtimeState, new GuardEnabledRuleSettings { ExpectedEnabled = true });
        var disabled = GuardEnabledRuleEvaluator.Evaluate(runtimeState, new GuardEnabledRuleSettings { ExpectedEnabled = false });

        AssertTrue(enabled, "Rule should match when guard state is enabled.");
        AssertFalse(disabled, "Rule should not match the opposite guard state.");
        return Task.CompletedTask;
    }

    private static async Task TestFailureRetryAsync()
    {
        var context = CreateContext(controllerState: HotspotActualState.Off, autoStartGuard: true, startupTarget: GuardTargetState.On);
        context.Controller.FailNextSet = true;

        await context.Coordinator.InitializeAsync(CancellationToken.None);

        AssertEqual(1, context.Controller.StartCallCount, "Initialization should attempt the first sync.");
        AssertTrue(!string.IsNullOrWhiteSpace(context.RuntimeState.LastError), "A failed sync should record an error.");

        await context.Coordinator.RunPeriodicCheckAsync(CancellationToken.None);

        AssertEqual(2, context.Controller.StartCallCount, "Periodic check should retry after a failure.");
        AssertTrue(string.IsNullOrWhiteSpace(context.RuntimeState.LastError), "A successful retry should clear the last error.");
        AssertEqual(HotspotActualState.On, context.RuntimeState.LastKnownHotspotState, "Runtime state should recover after retry success.");
    }

    private static async Task TestConcurrentSyncDoesNotOverlapAsync()
    {
        var context = CreateContext(controllerState: HotspotActualState.On, autoStartGuard: true, startupTarget: GuardTargetState.On);
        await context.Coordinator.InitializeAsync(CancellationToken.None);

        context.Controller.CurrentState = HotspotActualState.Off;
        context.Controller.SetDelay = TimeSpan.FromMilliseconds(150);
        context.Controller.ResetCounts();

        await Task.WhenAll(
            context.Coordinator.RequestSyncAsync(CancellationToken.None),
            context.Coordinator.RequestSyncAsync(CancellationToken.None),
            context.Coordinator.RequestSyncAsync(CancellationToken.None));

        AssertEqual(1, context.Controller.StartCallCount, "Only the first queued sync should need to turn the hotspot back on.");
        AssertEqual(1, context.Controller.MaxConcurrentSetCalls, "Hotspot sync should never overlap.");
    }

    private static TestContext CreateContext(HotspotActualState controllerState, bool autoStartGuard, GuardTargetState startupTarget)
    {
        var root = CreateTempDirectory();
        var settingsStore = new HotspotPluginSettingsStore(Path.Combine(root, "settings.json"))
        {
            AutoStartGuard = autoStartGuard,
            StartupTarget = startupTarget
        };
        var runtimeState = new HotspotGuardRuntimeState();
        var controller = new FakeHotspotController(controllerState);
        var notifier = new RecordingGuardStatusNotifier();
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 3, 27, 0, 0, 0, TimeSpan.Zero));
        var coordinator = new HotspotGuardCoordinator(controller, settingsStore, runtimeState, notifier, timeProvider);

        return new TestContext(root, settingsStore, runtimeState, controller, notifier, coordinator);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "HotspotService.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertFalse(bool condition, string message)
    {
        if (condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Expected: {expected}; Actual: {actual}.");
        }
    }

    private sealed record TestContext(
        string RootDirectory,
        HotspotPluginSettingsStore SettingsStore,
        HotspotGuardRuntimeState RuntimeState,
        FakeHotspotController Controller,
        RecordingGuardStatusNotifier Notifier,
        HotspotGuardCoordinator Coordinator);

    private sealed class RecordingGuardStatusNotifier : IGuardStatusNotifier
    {
        public int NotificationCount { get; private set; }

        public void NotifyGuardStatusChanged()
        {
            NotificationCount++;
        }
    }

    private sealed class FakeHotspotController : IHotspotController
    {
        private int _concurrentSetCalls;

        public FakeHotspotController(HotspotActualState initialState)
        {
            CurrentState = initialState;
        }

        public HotspotActualState CurrentState { get; set; }

        public bool FailNextSet { get; set; }

        public TimeSpan SetDelay { get; set; }

        public int GetStateCallCount { get; private set; }

        public int SetStateCallCount { get; private set; }

        public int StartCallCount { get; private set; }

        public int StopCallCount { get; private set; }

        public int MaxConcurrentSetCalls { get; private set; }

        public Task<HotspotActualState> GetStateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetStateCallCount++;
            return Task.FromResult(CurrentState);
        }

        public async Task SetStateAsync(GuardTargetState target, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SetStateCallCount++;
            if (target == GuardTargetState.On)
            {
                StartCallCount++;
            }
            else
            {
                StopCallCount++;
            }

            var concurrent = Interlocked.Increment(ref _concurrentSetCalls);
            MaxConcurrentSetCalls = Math.Max(MaxConcurrentSetCalls, concurrent);
            try
            {
                if (SetDelay > TimeSpan.Zero)
                {
                    await Task.Delay(SetDelay, cancellationToken);
                }

                if (FailNextSet)
                {
                    FailNextSet = false;
                    throw new InvalidOperationException("Simulated hotspot failure.");
                }

                CurrentState = target.ToActualState();
            }
            finally
            {
                Interlocked.Decrement(ref _concurrentSetCalls);
            }
        }

        public void ResetCounts()
        {
            GetStateCallCount = 0;
            SetStateCallCount = 0;
            StartCallCount = 0;
            StopCallCount = 0;
            MaxConcurrentSetCalls = 0;
            _concurrentSetCalls = 0;
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public ManualTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}
