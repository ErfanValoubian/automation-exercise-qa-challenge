using System.Text.Json;
using Challenge.Support.Api;
using Challenge.Support.Diagnostics;

namespace Challenge.Support.Data;

public sealed record Customer(string Name, string Email, string Password, string Address, string City)
{
    public static Customer Unique(string worker) => new("QA " + Guid.NewGuid().ToString("N")[..10],
        $"qa-{new string(worker.Where(char.IsLetterOrDigit).ToArray())}-{Guid.NewGuid():N}@example.com", "Qa!" + Guid.NewGuid().ToString("N"), "42 Test Street", "Test City");
    public Dictionary<string, string> Form() => new()
    {
        ["name"] = Name, ["email"] = Email, ["password"] = Password, ["title"] = "Mr",
        ["birth_date"] = "15", ["birth_month"] = "6", ["birth_year"] = "1990",
        ["firstname"] = Name, ["lastname"] = "Automation", ["company"] = "QA Practice",
        ["address1"] = Address, ["address2"] = "", ["country"] = "Canada", ["zipcode"] = "A1A1A1",
        ["state"] = "Test Province", ["city"] = City, ["mobile_number"] = "5550100123"
    };
}

public sealed class Customers(ExerciseApi api, Journal journal)
{
    private readonly List<Customer> _owned = [];
    public async Task<Customer> Create(string worker)
    {
        var user = Customer.Unique(worker);
        journal.Redactor.Register(user.Email, user.Password);
        // Register ownership BEFORE the request: a lost response may still have created the account.
        _owned.Add(user);
        var reply = await api.Send(HttpMethod.Post, "createAccount", user.Form());
        reply.RequireEnvelope(201);
        journal.Add("arrange", "Created an isolated synthetic customer.");
        return user;
    }

    public async Task<string> Cleanup(bool retain, string privateRoot)
    {
        if (_owned.Count == 0) return "Not required (no accounts created)";
        var results = new List<string>();
        foreach (var user in _owned)
        {
            if (retain)
            {
                Directory.CreateDirectory(privateRoot);
                File.WriteAllText(Path.Combine(privateRoot, journal.CorrelationId + ".json"), JsonSerializer.Serialize(_owned));
                results.Add("Retained: credentials saved only in .local-retained (not published)");
                continue;
            }
            try
            {
                var result = await api.Delete(user.Email, user.Password);
                if (result.Code is not (200 or 404)) result.RequireEnvelope(200);
                var lookup = await api.Lookup(user.Email);
                lookup.RequireEnvelope(404);
                results.Add("Deleted / confirmed absent");
            }
            catch (Exception ex)
            {
                results.Add("WARNING: cleanup not confirmed; inspect timeline");
                journal.Add("cleanup-warning", ex.GetType().Name, ex.Message);
            }
        }
        var summary = string.Join("; ", results);
        journal.Add("cleanup", summary);
        return summary;
    }
}
