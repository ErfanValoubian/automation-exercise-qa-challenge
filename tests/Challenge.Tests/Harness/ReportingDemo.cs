using Challenge.Tests.Fixtures;
using NUnit.Framework;

namespace Challenge.Tests.Harness;

[TestFixture, Category("demo"), NonParallelizable]
public sealed class ReportingDemo : Scenario
{
    [Test, Explicit("Offline intentional failure; use scripts/run.ps1 -Suite report-demo.")]
    public void IntentionalFailure_ProducesPortableDiagnostics()
    {
        Act();
        Log.Add("diagnostic-demo", "No network, account or application used; verifying assertion evidence and reporting only.");
        Assert.That(2 + 2, Is.EqualTo(5), "EXPECTED DEMO FAILURE: offline report plumbing demonstration.");
    }
}
