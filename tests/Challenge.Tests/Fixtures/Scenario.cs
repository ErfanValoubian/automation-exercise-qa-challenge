using Challenge.Support;
using Challenge.Support.Api;
using Challenge.Support.Browser;
using Challenge.Support.Data;
using Challenge.Support.Diagnostics;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System.Reflection;

namespace Challenge.Tests.Fixtures;

public abstract class Scenario
{
    protected Settings Config { get; private set; } = null!;
    protected Journal Log { get; private set; } = null!;
    protected ExerciseApi Api { get; private set; } = null!;
    protected Customers Data { get; private set; } = null!;
    protected BrowserSession? Browser { get; private set; }
    protected string Worker => TestContext.CurrentContext.WorkerId ?? "serial";
    private string _phase = "arrange";

    [SetUp]
    public void Begin()
    {
        Config = Settings.Load(TestContext.CurrentContext.TestDirectory);
        Log = new Journal();
        Log.Redactor.Register("4111111111111111");
        Api = new ExerciseApi(Config, Log);
        Data = new Customers(Api, Log);
        Log.Add("test", TestContext.CurrentContext.Test.FullName + "; worker=" + Worker);
        TestContext.Out.WriteLine("Correlation: " + Log.CorrelationId);
    }
    protected async Task<Customer> NewCustomer() => await Data.Create(Worker);
    protected void Act() { _phase = "act/assert"; Log.Add("phase", _phase); }
    protected async Task<Microsoft.Playwright.IPage> OpenBrowser()
    {
        Browser = new BrowserSession(Log);
        await Browser.Open(Config);
        return Browser.Page;
    }

    [TearDown]
    public async Task Finish()
    {
        if (Log is null) return;
        var result = TestContext.CurrentContext.Result;
        var failed = result.Outcome.Status == TestStatus.Failed;
        var folder = Reports.ForTest(Log.CorrelationId);
        var cleanup = "Not completed";
        try
        {
            if (failed && Browser is not null) await Browser.Capture(folder);
            // NUnit's final body verdict is available here, including assertion exceptions.
            cleanup = await Data.Cleanup(failed && Config.RetainFailedAccounts,
                Path.GetFullPath(System.Environment.GetEnvironmentVariable("AE_PRIVATE_DATA_DIR")
                    ?? Path.Combine(AppContext.BaseDirectory, ".local-retained")));
        }
        catch (Exception ex)
        {
            cleanup = "WARNING: teardown error; inspect timeline";
            Log.Add("teardown-warning", ex.GetType().Name, ex.Message);
        }
        finally
        {
            if (Browser is not null)
                try { await Browser.DisposeAsync(); }
                catch (Exception ex) { Log.Add("teardown-warning", "Browser disposal failed", ex.Message); }
            try { await Api.DisposeAsync(); }
            catch (Exception ex) { Log.Add("teardown-warning", "API context disposal failed", ex.Message); }
            var message = Log.Redactor.Clean((result.Message ?? "") + "\n" + (result.StackTrace ?? ""));
            var categories = GetType().GetCustomAttributes<CategoryAttribute>(true).Select(c => c.Name)
                .Concat(TestContext.CurrentContext.Test.Properties["Category"].OfType<string>()).Distinct().ToArray();
            var classification = OutcomeClassifier.Classify(failed, result.Outcome.Status.ToString(), _phase,
                categories.Contains("demo"), categories.Contains("observation"), message);
            var files = Directory.GetFiles(folder).Select(Path.GetFileName).OfType<string>().ToArray();
            Reports.Write(new TestReport(TestContext.CurrentContext.Test.FullName, result.Outcome.Status.ToString(),
                classification, Config.Environment, Config.BaseUrl, Browser is null ? "N/A (no browser)" : Config.Browser + (Config.BrowserChannel is null ? "" : "/" + Config.BrowserChannel),
                Worker, Log.CorrelationId, Log.DurationMs, cleanup, message, "Not retried", categories, Log.Events, files));
            foreach (var file in Directory.GetFiles(folder)) TestContext.AddTestAttachment(file);
            TestContext.Out.WriteLine("Report: " + Path.Combine(Reports.Root, "index.html"));
            TestContext.Out.WriteLine("Cleanup: " + cleanup);
        }
    }
}
