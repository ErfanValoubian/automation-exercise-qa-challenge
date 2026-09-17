using System.Diagnostics;
using System.Text.Json;
using Challenge.Support.Diagnostics;

namespace Challenge.Support.Api;

public sealed class ExerciseApi : IDisposable
{
    private readonly HttpClient _http;
    private readonly Journal _journal;
    public ExerciseApi(Settings settings, Journal journal)
    {
        _journal = journal;
        _http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        { BaseAddress = new Uri(settings.BaseUrl + "/api/"), Timeout = TimeSpan.FromMilliseconds(settings.ApiTimeoutMs) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("QAChallenge/1.0");
        _http.DefaultRequestHeaders.Add("X-Correlation-ID", journal.CorrelationId);
    }

    public async Task<ApiReply> Send(HttpMethod method, string path, IReadOnlyDictionary<string, string>? form = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (form is not null) request.Content = new FormUrlEncodedContent(form);
        _journal.Add("request", $"{method} {path}", form is null ? null : JsonSerializer.Serialize(form));
        var clock = Stopwatch.StartNew();
        try
        {
            using var response = await _http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            JsonElement? json = null;
            try { using var document = JsonDocument.Parse(raw); json = document.RootElement.Clone(); }
            catch (JsonException) { /* The contract assertion will report non-JSON with recorded evidence. */ }
            var reply = new ApiReply((int)response.StatusCode, raw, json, clock.ElapsedMilliseconds);
            _journal.Add("response", $"HTTP {reply.HttpStatus}; application {reply.Code?.ToString() ?? "n/a"}; {reply.DurationMs} ms", raw);
            if (reply.HttpStatus >= 500 || reply.HttpStatus == 429)
                throw new ServiceUnavailableException($"Shared service returned HTTP {reply.HttpStatus}. No retry performed.");
            if (reply.Code is int code && code != reply.HttpStatus)
                _journal.Add("contract-observation", $"HTTP {reply.HttpStatus} differs from application responseCode {code}.");
            return reply;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
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
    public void Dispose() => _http.Dispose();
}
