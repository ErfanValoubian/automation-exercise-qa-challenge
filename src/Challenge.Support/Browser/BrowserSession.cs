using Challenge.Support.Diagnostics;
using Microsoft.Playwright;

namespace Challenge.Support.Browser;

public sealed class BrowserSession(Journal journal) : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    public IPage Page { get; private set; } = null!;

    public async Task Open(Settings settings)
    {
        _playwright = await Playwright.CreateAsync();
        var type = settings.Browser switch
        {
            "firefox" => _playwright.Firefox,
            "webkit" => _playwright.Webkit,
            _ => _playwright.Chromium
        };
        _browser = await type.LaunchAsync(new() { Headless = settings.Headless });
        _context = await _browser.NewContextAsync(new()
        {
            BaseURL = settings.BaseUrl, ViewportSize = new() { Width = 1440, Height = 1000 },
            Locale = "en-US"
        });
        _context.SetDefaultTimeout(settings.TimeoutMs);
        _context.SetDefaultNavigationTimeout(settings.TimeoutMs * 2);
        // Trace is powerful, but raw: ONLY disposable, synthetic credentials are used in this suite.
        await _context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = false });
        Page = await _context.NewPageAsync();
        Page.PageError += (_, error) => journal.Add("browser-error", error);
        Page.Console += (_, msg) => { if (msg.Type == "error") journal.Add("browser-console", msg.Text); };
        journal.Add("browser", $"Isolated {settings.Browser} context started.");
    }

    public async Task Capture(string folder)
    {
        if (Page is null) return;
        // Collect independently: a failed screenshot must not prevent the trace or DOM capture.
        try { await Page.ScreenshotAsync(new() { Path = Path.Combine(folder, "failure.png"), FullPage = true, Timeout = 5000 }); }
        catch (Exception ex) { journal.Add("evidence-warning", "Screenshot unavailable", ex.Message); }
        try { File.WriteAllText(Path.Combine(folder, "failure-dom.html"), journal.Redactor.Clean(await Page.ContentAsync())); }
        catch (Exception ex) { journal.Add("evidence-warning", "DOM unavailable", ex.Message); }
        journal.Add("browser", "Failure page URL", Page.Url);
        if (_context is null) return;
        try { await _context.Tracing.StopAsync(new() { Path = Path.Combine(folder, "trace.zip") }); }
        catch (Exception ex) { journal.Add("evidence-warning", "Trace unavailable", ex.Message); }
    }

    public async ValueTask DisposeAsync()
    {
        try { if (_context is not null) await _context.CloseAsync(); }
        finally
        {
            try { if (_browser is not null) await _browser.CloseAsync(); }
            finally { _playwright?.Dispose(); }
        }
    }
}
