namespace SYSi.Services.UpdateService
{
    public enum UpdateStatus
    {
        Idle,
        Checking,
        UpdateAvailable,
        Downloading,
        ReadyToInstall,
        UpToDate,
        Error,
    }
}
