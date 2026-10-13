using System;

namespace VideoCropper;

// The encoder publishes from a worker thread; the UI samples only the newest position.
internal sealed class ProcessingPreviewPosition
{
    private readonly object gate = new();
    private TimeSpan? latest;
    private TimeSpan? lastRequested;
    private bool seeking;

    public void Report(TimeSpan position)
    {
        lock (gate) latest = position;
    }

    public bool TryTake(TimeSpan current, TimeSpan duration, out TimeSpan position)
    {
        lock (gate)
        {
            position = default;
            if (seeking || latest == null || duration <= TimeSpan.Zero) return false;
            position = TimeSpan.FromTicks(Math.Clamp(latest.Value.Ticks, 0, duration.Ticks - 1));
            if (position == lastRequested || (position - current).Duration() < TimeSpan.FromMilliseconds(100))
                return false;
            lastRequested = position;
            seeking = true;
            return true;
        }
    }

    public void CompleteSeek()
    {
        lock (gate) seeking = false;
    }

    public void Reset()
    {
        lock (gate)
        {
            latest = null;
            lastRequested = null;
        }
    }
}
