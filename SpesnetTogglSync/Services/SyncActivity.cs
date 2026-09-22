namespace SpesnetTogglSync.Services;

/// <summary>Keeps manual and automatic sync from running at the same time.</summary>
internal sealed class SyncActivity
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private int _busy;

    public bool IsBusy => Volatile.Read(ref _busy) == 1;

    public bool TryBegin()
    {
        if (!_gate.Wait(0))
        {
            return false;
        }

        Volatile.Write(ref _busy, 1);
        return true;
    }

    public void End()
    {
        Volatile.Write(ref _busy, 0);
        _gate.Release();
    }
}
