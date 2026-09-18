using Challenge.Support.Diagnostics;
using Microsoft.Playwright;

namespace Challenge.Support.Browser;

public static class ConsentDialog
{
    public static Task Register(IPage page, Journal journal)
    {
        var root = page.Locator(".fc-consent-root");
        var overlay = root.Locator(".fc-dialog-overlay");
        // The CMP can appear after any navigation or between checkout actions.
        // Playwright checks this handler again before retrying an obstructed action.
        return page.AddLocatorHandlerAsync(overlay, async () =>
        {
            journal.Add("privacy", "Consent dialog appeared; opening existing privacy choices.");
            var manage = root.GetByRole(AriaRole.Button, new() { Name = "Manage options", Exact = true });
            if (await manage.IsVisibleAsync()) await manage.ClickAsync();

            // Only the active preferences panel is eligible; hidden vendor panels also
            // contain this button. Keep the site's current choices instead of Accept all.
            await root.Locator("button.fc-confirm-choices:visible").ClickAsync();
            journal.Add("privacy", "Confirmed existing privacy choices through the visible dialog.");
            // Default handler behaviour waits for the overlay to disappear before resuming.
        });
    }
}
