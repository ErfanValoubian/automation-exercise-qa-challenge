using System.Net;
using System.Text;
using System.Text.Json;

namespace Challenge.Support.Diagnostics;

public sealed record TestReport(string Name, string Outcome, string Classification, string Environment,
    string Target, string Browser, string Worker, string CorrelationId, long DurationMs,
    string Cleanup, string Failure, string Retry, string[] Categories, IReadOnlyList<Event> Events, string[] Files);

// The only shared mutable state is a locked report index; no test data or browser is shared.
public static class Reports
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, TestReport> Completed = [];
    public static string Root { get; } = Path.GetFullPath(System.Environment.GetEnvironmentVariable("AE_ARTIFACTS")
        ?? Path.Combine(AppContext.BaseDirectory, "artifacts", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8]));
    public static string ForTest(string id) => Directory.CreateDirectory(Path.Combine(Root, id)).FullName;
    public static void Write(TestReport report)
    {
        var folder = ForTest(report.CorrelationId);
        File.WriteAllText(Path.Combine(folder, "diagnostics.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(folder, "details.html"), Details(report));
        lock (Gate)
        {
            Completed[report.CorrelationId] = report;
            var rows = Completed.Values.OrderBy(r => r.Name).ToArray();
            var body = new StringBuilder("<h1>Automation Exercise — Test report</h1><p>Completed tests in this test process. No automatic retries.</p>");
            body.Append($"<p><b>{rows.Length}</b> completed · <b>{rows.Count(r => r.Outcome == "Passed")}</b> passed · <b>{rows.Count(r => r.Outcome == "Failed")}</b> failed · <b>{rows.Count(r => r.Outcome is not ("Passed" or "Failed"))}</b> other</p>");
            body.Append("<table><thead><tr><th>Test</th><th>Outcome</th><th>Duration</th><th>Classification</th><th>Cleanup</th></tr></thead><tbody>");
            foreach (var row in rows)
                body.Append($"<tr><td><a href='{row.CorrelationId}/details.html'>{H(row.Name)}</a></td><td class='{H(row.Outcome)}'>{H(row.Outcome)}</td><td>{row.DurationMs} ms</td><td>{H(row.Classification)}</td><td>{H(row.Cleanup)}</td></tr>");
            body.Append("</tbody></table><p>TRX is the runner's authoritative result, including discovery/setup failures. Each detail page includes an event timeline and artifact links.</p>");
            File.WriteAllText(Path.Combine(Root, "index.html"), Page("Test run", body.ToString()));
        }
    }
    private static string Details(TestReport r)
    {
        var body = new StringBuilder($"<a href='../index.html'>← Run</a><h1>{H(r.Name)}</h1><p class='{H(r.Outcome)}'>{H(r.Outcome)} · {H(r.Classification)}</p>");
        body.Append($"<dl><dt>Correlation</dt><dd>{H(r.CorrelationId)}</dd><dt>Environment / target</dt><dd>{H(r.Environment)} / {H(r.Target)}</dd><dt>Browser / worker</dt><dd>{H(r.Browser)} / {H(r.Worker)}</dd><dt>Duration</dt><dd>{r.DurationMs} ms</dd><dt>Cleanup</dt><dd>{H(r.Cleanup)}</dd><dt>Retry</dt><dd>{H(r.Retry)}</dd><dt>Categories</dt><dd>{H(string.Join(", ", r.Categories))}</dd></dl>");
        if (!string.IsNullOrWhiteSpace(r.Failure)) body.Append($"<h2>Failure</h2><pre>{H(r.Failure)}</pre>");
        body.Append("<h2>Artifacts</h2><ul><li><a href='diagnostics.json'>Diagnostics JSON</a></li>");
        foreach (var file in r.Files) body.Append($"<li><a href='{Uri.EscapeDataString(file)}'>{H(file)}</a></li>");
        body.Append("</ul><h2>Timeline</h2>");
        foreach (var entry in r.Events)
            body.Append($"<details><summary>+{entry.ElapsedMs} ms · {H(entry.Kind)} · {H(entry.Message)}</summary><pre>{H(entry.Detail ?? "")}</pre></details>");
        return Page(r.Name, body.ToString());
    }
    private static string H(string text) => WebUtility.HtmlEncode(text);
    private static string Page(string title, string body) => "<!doctype html><html lang='en'><meta charset='utf-8'><meta name='viewport' content='width=device-width'><title>" + H(title) + "</title><style>body{font:15px/1.6 system-ui;margin:32px auto;padding:0 24px;max-width:1200px;color:#183044;background:#f6f8fb}h1{font-size:27px}table{width:100%;border-collapse:collapse;background:white}td,th{padding:12px;text-align:left;border-bottom:1px solid #d8e0e8}a{color:#075cab}pre{white-space:pre-wrap;overflow-wrap:anywhere;background:#fff;padding:14px;border:1px solid #dae2ec}details{margin:10px 0}.Passed{color:#16703c}.Failed{color:#b42318}dt{font-weight:bold}dd{margin:0 0 8px}summary{cursor:pointer}</style><body>" + body + "</body></html>";
}
