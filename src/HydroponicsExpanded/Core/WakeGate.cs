namespace HydroponicsExpanded.Core;

/// <summary>
/// When the table check runs: once after each wake, then again every second or so until a pass says it is done. A
/// check that is done waits for the next wake (the game loading its tables or a language, or a setting change).
/// </summary>
public sealed class WakeGate
{
    private readonly double _retrySeconds;
    private int _seenWakes = -1;
    private bool _isDone;
    private double _nextTryAt;

    public WakeGate(double retrySeconds) => _retrySeconds = retrySeconds;

    /// <summary>True when the check should run now. <paramref name="wakes"/> is how many wakes there have been so far.</summary>
    public bool IsDue(double now, int wakes)
    {
        if (wakes != _seenWakes)
        {
            _seenWakes = wakes;
            _isDone = false;
            _nextTryAt = now;
        }

        if (_isDone || now < _nextTryAt) return false;

        _nextTryAt = now + _retrySeconds;

        return true;
    }

    /// <summary>The pass found the tables it needed; nothing more runs until the next wake.</summary>
    public void Done() => _isDone = true;
}
