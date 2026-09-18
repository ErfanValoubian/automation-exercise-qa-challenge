using Challenge.Tests.Fixtures;
using Microsoft.Playwright;
using NUnit.Framework;
using static Microsoft.Playwright.Assertions;

namespace Challenge.Tests.Harness;

[TestFixture, Category("browser-harness"), Parallelizable(ParallelScope.All)]
public sealed class ConsentDialogTests : Scenario
{
    // Minimal helper fixtures verify overlay timing, not a replacement of the public shop.
    private const string Fixture = """
        <input aria-label="Order comment" oninput="document.querySelector('.fc-consent-root').hidden=false">
        <button onclick="document.querySelector('#result').textContent='Order clicked'">Place Order</button>
        <p id="result"></p>
        <div class="fc-consent-root" hidden>
          <div class="fc-dialog-overlay" style="position:fixed;inset:0;background:#8888;z-index:10"></div>
          <div style="position:fixed;top:100px;left:100px;z-index:11;background:white;padding:20px">
            <section id="intro">
              <button onclick="document.querySelector('#intro').hidden=true;document.querySelector('#choices').hidden=false">Manage options</button>
              <button onclick="document.querySelector('#result').textContent='WRONG: accepted all'">Consent</button>
            </section>
            <section id="choices" hidden>
              <button class="fc-confirm-choices" onclick="document.querySelector('.fc-consent-root').hidden=true">Confirm choices</button>
            </section>
            <section hidden><button class="fc-confirm-choices">Confirm choices</button></section>
          </div>
        </div>
        """;

    [Test]
    public async Task DialogAppearingBetweenActions_IsHandledBeforeCheckoutClick()
    {
        var page = await OpenBrowser();
        await page.SetContentAsync(Fixture);
        Act();
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Order comment" }).FillAsync("ready");
        await page.GetByRole(AriaRole.Button, new() { Name = "Place Order", Exact = true }).ClickAsync();
        await Expect(page.Locator("#result")).ToHaveTextAsync("Order clicked");
        Assert.That(Log.Events.Count(e => e.Kind == "privacy"), Is.EqualTo(2));
    }

    [Test]
    public async Task Handler_RemainsAvailableAfterASecondDocument()
    {
        var page = await OpenBrowser();
        Act();
        for (var document = 0; document < 2; document++)
        {
            await page.SetContentAsync(Fixture);
            await page.GetByRole(AriaRole.Textbox, new() { Name = "Order comment" }).FillAsync("ready");
            await page.GetByRole(AriaRole.Button, new() { Name = "Place Order", Exact = true }).ClickAsync();
            await Expect(page.Locator("#result")).ToHaveTextAsync("Order clicked");
        }
        Assert.That(Log.Events.Count(e => e.Kind == "privacy"), Is.EqualTo(4));
    }

    [Test]
    public async Task NoOverlay_DoesNotClickAnUnrelatedConsentButton()
    {
        var page = await OpenBrowser();
        await page.SetContentAsync("""
            <button onclick="document.querySelector('p').textContent='WRONG'">Consent</button>
            <button onclick="document.querySelector('p').textContent='Order clicked'">Place Order</button><p></p>
            """);
        Act();
        await page.GetByRole(AriaRole.Button, new() { Name = "Place Order", Exact = true }).ClickAsync();
        await Expect(page.Locator("p")).ToHaveTextAsync("Order clicked");
        Assert.That(Log.Events.Any(e => e.Kind == "privacy"), Is.False);
    }
}
