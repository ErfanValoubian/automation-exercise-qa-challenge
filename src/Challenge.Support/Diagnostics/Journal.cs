using System.Diagnostics;

namespace Challenge.Support.Diagnostics;

public sealed record Event(long ElapsedMs, string Kind, string Message, string? Detail);

public sealed class Journal
{
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly List<Event> _events = [];
    public string CorrelationId { get; } = Guid.NewGuid().ToString("N");
    public Redactor Redactor { get; } = new();
    public long DurationMs => _clock.ElapsedMilliseconds;
    public IReadOnlyList<Event> Events { get { lock (_events) return _events.ToArray(); } }

    public void Add(string kind, string message, string? detail = null)
    {
        var safe = detail is null ? null : Redactor.Body(detail);
        if (safe?.Length > 12000) safe = safe[..12000] + " [truncated]";
        lock (_events) _events.Add(new Event(_clock.ElapsedMilliseconds, kind, Redactor.Clean(message), safe));
    }
}
