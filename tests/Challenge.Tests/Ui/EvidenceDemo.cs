using Challenge.Tests.Fixtures;
using NUnit.Framework;

namespace Challenge.Tests.Ui;

[TestFixture, Category("demo"), NonParallelizable]
public sealed class EvidenceDemo : Scenario
{
    [Test, Explicit("Intentional failure. Run by exact fully qualified test name using scripts/run.ps1 -Suite demo.")]
    public async Task IntentionalFailure_CapturesBrowserEvidence()
    {
        var page = await OpenBrowser();
        // No external request or account required; this tests artifact plumbing, not product behaviour.
        await page.SetContentAsync("<!doctype html><html><title>Diagnostics demonstration</title><body><h1>Evidence demo</h1><p>This local page deliberately precedes a failed assertion.</p></body></html>");
        Act();
        Assert.That(await page.TitleAsync(), Is.EqualTo("Intentionally incorrect title"), "EXPECTED DEMO FAILURE: verify screenshot, trace, JSON and HTML attachments.");
    }
}
