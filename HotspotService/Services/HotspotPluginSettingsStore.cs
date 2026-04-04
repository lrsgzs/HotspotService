using HotspotService.Infrastructure;
using HotspotService.Models;

namespace HotspotService.Services;

public sealed class HotspotPluginSettingsStore : ObservableObject
{
    private readonly string _settingsFilePath;
    private readonly object _fileLock = new();
    private bool _autoStartGuard = true;
    private GuardTargetState _startupTarget = GuardTargetState.On;

    public HotspotPluginSettingsStore(string settingsFilePath)
    {
        _settingsFilePath = settingsFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);
        Load();
    }

    public bool AutoStartGuard
    {
        get => _autoStartGuard;
        set
        {
            if (SetProperty(ref _autoStartGuard, value))
            {
                Save();
            }
        }
    }

    public GuardTargetState StartupTarget
    {
        get => _startupTarget;
        set
        {
            if (SetProperty(ref _startupTarget, value))
            {
                Save();
            }
        }
    }

    private void Load()
    {
        if (!File.Exists(_settingsFilePath))
        {
            Save();
            return;
        }

        try
        {
            foreach (var line in File.ReadAllLines(_settingsFilePath))
            {
                var parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2)
                {
                    continue;
                }

                if (parts[0].Equals(nameof(HotspotPluginSettingsDocument.AutoStartGuard), StringComparison.OrdinalIgnoreCase)
                    && bool.TryParse(parts[1], out var autoStartGuard))
                {
                    _autoStartGuard = autoStartGuard;
                }

                if (parts[0].Equals(nameof(HotspotPluginSettingsDocument.StartupTarget), StringComparison.OrdinalIgnoreCase)
                    && Enum.TryParse<GuardTargetState>(parts[1], true, out var startupTarget))
                {
                    _startupTarget = startupTarget;
                }
            }
        }
        catch
        {
            _autoStartGuard = true;
            _startupTarget = GuardTargetState.On;
        }
    }

    private void Save()
    {
        lock (_fileLock)
        {
            var lines = new[]
            {
                $"{nameof(HotspotPluginSettingsDocument.AutoStartGuard)}={_autoStartGuard}",
                $"{nameof(HotspotPluginSettingsDocument.StartupTarget)}={_startupTarget}"
            };
            File.WriteAllLines(_settingsFilePath, lines);
        }
    }
}
