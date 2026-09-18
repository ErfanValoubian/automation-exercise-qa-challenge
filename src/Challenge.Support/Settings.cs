using System.Text.Json;

namespace Challenge.Support;

public sealed record Settings
{
    public string BaseUrl { get; init; } = "";
    public string Environment { get; init; } = "public-practice";
    public string Browser { get; init; } = "chromium";
    public string? BrowserChannel { get; init; }
    public bool Headless { get; init; } = true;
    public int TimeoutMs { get; init; } = 20000;
    public int ApiTimeoutMs { get; init; } = 30000;
    public bool RetainFailedAccounts { get; init; }

    public static Settings Load(string directory)
    {
        var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(Path.Combine(directory, "settings.json")))
            ?? throw new InvalidDataException("settings.json must contain a settings object.");
        string Read(string key, string fallback) => System.Environment.GetEnvironmentVariable("AE_" + key) ?? fallback;
        settings = settings with
        {
            BaseUrl = Read("BASE_URL", settings.BaseUrl).TrimEnd('/'),
            Environment = Read("ENVIRONMENT", settings.Environment),
            Browser = Read("BROWSER", settings.Browser),
            BrowserChannel = System.Environment.GetEnvironmentVariable("AE_BROWSER_CHANNEL") ?? settings.BrowserChannel,
            Headless = bool.Parse(Read("HEADLESS", settings.Headless.ToString())),
            TimeoutMs = int.Parse(Read("TIMEOUT_MS", settings.TimeoutMs.ToString())),
            ApiTimeoutMs = int.Parse(Read("API_TIMEOUT_MS", settings.ApiTimeoutMs.ToString())),
            RetainFailedAccounts = bool.Parse(Read("RETAIN_FAILED_ACCOUNTS", settings.RetainFailedAccounts.ToString()))
        };
        if (!Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var url) || url.Scheme != "https")
            throw new InvalidDataException("AE_BASE_URL must be an absolute HTTPS URL.");
        if (settings.Browser is not ("chromium" or "firefox" or "webkit"))
            throw new InvalidDataException("AE_BROWSER must be chromium, firefox or webkit.");
        if (settings.BrowserChannel is not null && (settings.Browser != "chromium" || settings.BrowserChannel is not ("msedge" or "chrome")))
            throw new InvalidDataException("AE_BROWSER_CHANNEL supports msedge or chrome with AE_BROWSER=chromium only.");
        if (settings.TimeoutMs <= 0 || settings.ApiTimeoutMs <= 0)
            throw new InvalidDataException("Timeouts must be positive.");
        return settings;
    }
}
