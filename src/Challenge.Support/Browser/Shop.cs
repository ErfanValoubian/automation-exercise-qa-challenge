using System.Globalization;
using System.Text.RegularExpressions;
using Challenge.Support.Data;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Challenge.Support.Browser;

public sealed record Selection(string Id, string Name, decimal Price);

// Locators remain in small capability objects; tests own business expectations.
public sealed class AccountScreen(IPage page)
{
    public ILocator LoggedInName => page.Locator("header li").Filter(new() { HasTextString = "Logged in as" }).Locator("b");
    public ILocator SignInLink => page.GetByRole(AriaRole.Link, new() { Name = "Signup / Login", Exact = true });
    public ILocator LogoutLink => page.GetByRole(AriaRole.Link, new() { Name = "Logout", Exact = true });
    public ILocator LoginError => page.GetByText("Your email or password is incorrect!", new() { Exact = true });
    public async Task Login(string email, string password)
    {
        await page.GotoAsync("/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-qa='login-email']").FillAsync(email);
        await page.Locator("[data-qa='login-password']").FillAsync(password);
        await page.Locator("[data-qa='login-button']").ClickAsync();
    }
    public async Task Logout()
    {
        await LogoutLink.ClickAsync();
        await Expect(SignInLink).ToBeVisibleAsync();
        await Expect(LogoutLink).ToHaveCountAsync(0);
    }
}

public sealed class CatalogueScreen(IPage page)
{
    private ILocator Cards => page.Locator(".features_items .productinfo");
    public async Task Search(string term)
    {
        await page.GotoAsync("/products", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("#search_product").FillAsync(term);
        await page.Locator("#submit_search").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Searched Products", Exact = true })).ToBeVisibleAsync();
    }
    public async Task<IReadOnlyList<Selection>> SelectFirst(int count)
    {
        await Expect(Cards.Nth(count - 1)).ToBeVisibleAsync();
        var items = new List<Selection>();
        for (var index = 0; index < count; index++)
        {
            var card = Cards.Nth(index);
            var id = await card.Locator(".add-to-cart").GetAttributeAsync("data-product-id")
                ?? throw new InvalidDataException("Product card lacks a data-product-id.");
            items.Add(new Selection(id, (await card.Locator("p").InnerTextAsync()).Trim(), Money.Parse(await card.Locator("h2").InnerTextAsync())));
        }
        return items;
    }
    public async Task Add(Selection product)
    {
        await Cards.Locator($".add-to-cart[data-product-id='{product.Id}']").ClickAsync();
        var close = page.GetByRole(AriaRole.Button, new() { Name = "Continue Shopping", Exact = true });
        await close.ClickAsync();
        await Expect(page.Locator("#cartModal")).ToBeHiddenAsync();
    }
}

public static class Money
{
    public static decimal Parse(string text)
    {
        var match = Regex.Match(text.Trim(), @"^Rs\.\s*(\d+(?:,\d{3})*(?:\.\d{1,2})?)$");
        if (!match.Success) throw new FormatException($"Unsupported price format: '{text}'.");
        return decimal.Parse(match.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture);
    }
}

public sealed class BasketScreen(IPage page)
{
    public ILocator Rows => page.Locator("#cart_info_table tbody tr[id^='product-']");
    public ILocator Row(Selection item) => page.Locator("#product-" + item.Id);
    public Task Open() => page.GotoAsync("/view_cart", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
    public async Task<(string Name, decimal Price, int Quantity, decimal Total)> Read(Selection item)
    {
        var row = Row(item);
        await Expect(row).ToBeVisibleAsync();
        return ((await row.Locator(".cart_description h4 a").InnerTextAsync()).Trim(),
            Money.Parse(await row.Locator(".cart_price p").InnerTextAsync()),
            int.Parse(await row.Locator(".cart_quantity button").InnerTextAsync(), CultureInfo.InvariantCulture),
            Money.Parse(await row.Locator(".cart_total_price").InnerTextAsync()));
    }
    public async Task Remove(Selection item)
    {
        await Row(item).Locator(".cart_quantity_delete").ClickAsync();
        await Expect(Row(item)).ToHaveCountAsync(0);
    }
    public Task Checkout() => page.GetByText("Proceed To Checkout", new() { Exact = true }).ClickAsync();
}

public sealed class CheckoutScreen(IPage page)
{
    public ILocator DeliveryAddress => page.Locator("#address_delivery");
    public ILocator Confirmation => page.GetByText("Congratulations! Your order has been confirmed!", new() { Exact = true });
    public ILocator Heading => page.Locator("[data-qa='order-placed']");
    public ILocator Invoice => page.GetByRole(AriaRole.Link, new() { Name = "Download Invoice", Exact = true });
    public async Task PayWithSyntheticCard(Customer customer, string correlation)
    {
        await page.Locator("textarea[name='message']").FillAsync("QA practice purchase " + correlation);
        await page.GetByRole(AriaRole.Link, new() { Name = "Place Order", Exact = true }).ClickAsync();
        await page.Locator("[data-qa='name-on-card']").FillAsync(customer.Name);
        await page.Locator("[data-qa='card-number']").FillAsync("4111111111111111");
        await page.Locator("[data-qa='cvc']").FillAsync("123");
        await page.Locator("[data-qa='expiry-month']").FillAsync("12");
        await page.Locator("[data-qa='expiry-year']").FillAsync((DateTime.UtcNow.Year + 2).ToString(CultureInfo.InvariantCulture));
        await page.Locator("[data-qa='pay-button']").ClickAsync();
    }
}
