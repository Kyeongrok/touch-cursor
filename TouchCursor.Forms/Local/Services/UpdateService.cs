using Velopack;
using Velopack.Sources;

namespace TouchCursor.Forms.Local.Services;

public class UpdateService
{
    private readonly UpdateManager _updateManager;
    private Velopack.UpdateInfo? _pendingUpdate;

    public string? LastDiagnostic { get; private set; }

    public UpdateService()
    {
        _updateManager = new UpdateManager(
            new GithubSource("https://github.com/Kyeongrok/touch-cursor", null, false));
    }

    public bool IsInstalled => _updateManager.IsInstalled;

    public string? CurrentVersion => _updateManager.CurrentVersion?.ToString();

    public async Task<string?> CheckForUpdateAsync()
    {
        if (!IsInstalled)
        {
            LastDiagnostic = "IsInstalled=false (Setup.exe로 설치된 앱이 아님)";
            return null;
        }
        try
        {
            _pendingUpdate = await _updateManager.CheckForUpdatesAsync();
            if (_pendingUpdate == null)
            {
                LastDiagnostic = $"업데이트 없음 (현재 {CurrentVersion} = 최신)";
                return null;
            }
            LastDiagnostic = $"업데이트 감지: {CurrentVersion} → {_pendingUpdate.TargetFullRelease.Version}";
            return _pendingUpdate.TargetFullRelease.Version.ToString();
        }
        catch (Exception ex)
        {
            LastDiagnostic = $"예외: {ex.GetType().Name}: {ex.Message}";
            return null;
        }
    }

    public async Task DownloadUpdateAsync(Action<int>? onProgress = null)
    {
        if (_pendingUpdate == null) return;
        await _updateManager.DownloadUpdatesAsync(_pendingUpdate, onProgress);
    }

    public void ApplyUpdateAndRestart()
    {
        if (_pendingUpdate == null) return;
        _updateManager.ApplyUpdatesAndRestart(_pendingUpdate.TargetFullRelease);
    }
}
