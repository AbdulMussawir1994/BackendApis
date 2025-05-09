using System.Collections.Concurrent;

namespace DAL.ServiceLayer.Helpers;

public class RequestTracker
{
    private readonly ConcurrentQueue<DateTime> _timestamps = new();
    private DateTime? _spammerUntil;

    public int Count => _timestamps.Count;
    public bool IsMarkedSpammer => _spammerUntil.HasValue;

    public void AddRequest(DateTime timestamp) => _timestamps.Enqueue(timestamp);

    public void TrimOldRequests(DateTime now, TimeSpan window)
    {
        while (_timestamps.TryPeek(out var ts) && (now - ts) > window)
        {
            _timestamps.TryDequeue(out _);
        }
    }

    public bool IsBlocked(DateTime now, TimeSpan blockTime)
    {
        return _spammerUntil.HasValue && now < _spammerUntil.Value.Add(blockTime);
    }

    public void MarkAsSpammer(DateTime now) => _spammerUntil = now;
}
