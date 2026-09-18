using System.Diagnostics;
using System.Text.Json;
using Challenge.Support.Diagnostics;
using Microsoft.Playwright;

namespace Challenge.Support.Api;

public sealed class ExerciseApi : IAsyncDisposable
{
    private readonly Settings _settings;
    private readonly Journal _journal;
    private IPlaywright? _driver;
    private IAPIRequestContext? _context;
    public ExerciseApi(Settings settings, Journal journal)
    {
        _journal = journal;
        _settings = settings;
    }

    public async Task<ApiReply> Send(HttpMethod method, string path, IReadOnlyDictionary<string, string>? form = null)
    {
        _journal.Add("request", $"{method} {path}", form is null ? null : JsonSerializer.Serialize(form));
        var clock = Stopwatch.StartNew();
        try
        {
            if (_context is null)
            {
                _driver = await Playwright.CreateAsync();
                _context = await _driver.APIRequest.NewContextAsync(new()
                {
                    BaseURL = _settings.BaseUrl + "/api/", Timeout = _settings.ApiTimeoutMs,
                    IgnoreHTTPSErrors = false, UserAgent = "QAChallenge/1.0",
                    ExtraHTTPHeaders = new Dictionary<string, string> { ["X-Correlation-ID"] = _journal.CorrelationId }
                });
            }
            using var encoded = form is null ? null : new FormUrlEncodedContent(form);
            var response = await _context.FetchAsync(path, new()
            {
                Method = method.Method, FailOnStatusCode = false, MaxRedirects = 0, MaxRetries = 0,
                Data = encoded is null ? null : await encoded.ReadAsStringAsync(),
                Headers = form is null ? null : new Dictionary<string, string> { ["Content-Type"] = "application/x-www-form-urlencoded" }
            });
            string raw;
            try { raw = await response.TextAsync(); }
            finally { await response.DisposeAsync(); }
            JsonElement? json = null;
            try { using var document = JsonDocument.Parse(raw); json = document.RootElement.Clone(); }
            catch (JsonException) { /* The contract assertion will report non-JSON with recorded evidence. */ }
            var reply = new ApiReply(response.Status, raw, json, clock.ElapsedMilliseconds);
            _journal.Add("response", $"HTTP {reply.HttpStatus}; application {reply.Code?.ToString() ?? "n/a"}; {reply.DurationMs} ms", raw);
            if (reply.HttpStatus >= 500 || reply.HttpStatus == 429)
                throw new ServiceUnavailableException($"Shared service returned HTTP {reply.HttpStatus}. No retry performed.");
            if (reply.Code is int code && code != reply.HttpStatus)
                _journal.Add("contract-observation", $"HTTP {reply.HttpStatus} differs from application responseCode {code}.");
            return reply;
        }
        catch (Exception ex) when (ex is PlaywrightException or TaskCanceledException)
        {
            _journal.Add("environment", $"{ex.GetType().Name} after {clock.ElapsedMilliseconds} ms", ex.ToString());
            throw new ServiceUnavailableException($"Request failed: {ex.GetType().Name}; see timeline. No retry performed.");
        }
    }

    public Task<ApiReply> Catalogue() => Send(HttpMethod.Get, "productsList");
    public Task<ApiReply> Search(string term) => Send(HttpMethod.Post, "searchProduct", new Dictionary<string, string> { ["search_product"] = term });
    public Task<ApiReply> Login(string email, string password) => Send(HttpMethod.Post, "verifyLogin", Credentials(email, password));
    public Task<ApiReply> Lookup(string email) => Send(HttpMethod.Get, "getUserDetailByEmail?email=" + Uri.EscapeDataString(email));
    public Task<ApiReply> Delete(string email, string password) => Send(HttpMethod.Delete, "deleteAccount", Credentials(email, password));
    private static Dictionary<string, string> Credentials(string email, string password) => new() { ["email"] = email, ["password"] = password };
    public async ValueTask DisposeAsync()
    {
        try { if (_context is not null) await _context.DisposeAsync(); }
        finally { _driver?.Dispose(); }
    }
}
