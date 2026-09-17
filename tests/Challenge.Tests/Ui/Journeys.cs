using Challenge.Support.Browser;
using Challenge.Tests.Fixtures;
using NUnit.Framework;
using static Microsoft.Playwright.Assertions;

namespace Challenge.Tests.Ui;

[TestFixture, Category("ui"), Parallelizable(ParallelScope.All)]
public sealed class Journeys : Scenario
{
    [Test, Category("smoke")]
    public async Task AccountAccess_LoginAndLogoutChangeTheSession()
    {
        var customer = await NewCustomer();
        var page = await OpenBrowser();
        var account = new AccountScreen(page);
        Act();
        await account.Login(customer.Email, customer.Password);
        await Expect(account.LoggedInName).ToHaveTextAsync(customer.Name);
        await Expect(account.LogoutLink).ToBeVisibleAsync();
        await account.Logout();
        await page.ReloadAsync(new() { WaitUntil = Microsoft.Playwright.WaitUntilState.DOMContentLoaded });
        await Expect(account.SignInLink).ToBeVisibleAsync();
        await Expect(account.LoggedInName).ToHaveCountAsync(0);
        await Expect(account.LogoutLink).ToHaveCountAsync(0);
    }

    [Test, Category("regression")]
    public async Task SearchAndCart_TwoProductsKeepTheirPricesAndRemovalIsSelective()
    {
        var page = await OpenBrowser();
        var catalogue = new CatalogueScreen(page);
        var basket = new BasketScreen(page);
        Act();
        await catalogue.Search("top");
        var selected = await catalogue.SelectFirst(2);
        foreach (var product in selected) await catalogue.Add(product);
        await basket.Open();
        await Expect(basket.Rows).ToHaveCountAsync(2);
        foreach (var product in selected)
        {
            var actual = await basket.Read(product);
            Assert.Multiple(() =>
            {
                Assert.That(actual.Name, Is.EqualTo(product.Name));
                Assert.That(actual.Price, Is.EqualTo(product.Price));
                Assert.That(actual.Quantity, Is.EqualTo(1));
                Assert.That(actual.Total, Is.EqualTo(product.Price * actual.Quantity));
            });
        }
        await basket.Remove(selected[0]);
        await Expect(basket.Rows).ToHaveCountAsync(1);
        Assert.That((await basket.Read(selected[1])).Name, Is.EqualTo(selected[1].Name));
    }

    [Test, Category("smoke"), Category("hybrid")]
    public async Task Purchase_ApiCustomerSeesCorrectCheckoutAndConfirmedOrder()
    {
        var customer = await NewCustomer();
        var page = await OpenBrowser();
        var account = new AccountScreen(page);
        var catalogue = new CatalogueScreen(page);
        var basket = new BasketScreen(page);
        var checkout = new CheckoutScreen(page);
        Act();
        await account.Login(customer.Email, customer.Password);
        await Expect(account.LoggedInName).ToHaveTextAsync(customer.Name);
        await catalogue.Search("dress");
        var selected = (await catalogue.SelectFirst(1)).Single();
        await catalogue.Add(selected);
        await basket.Open();
        await Expect(basket.Rows).ToHaveCountAsync(1);
        var cart = await basket.Read(selected);
        Assert.That(cart, Is.EqualTo((selected.Name, selected.Price, 1, selected.Price)));
        await basket.Checkout();
        await Expect(checkout.DeliveryAddress).ToContainTextAsync(customer.Address);
        await Expect(checkout.DeliveryAddress).ToContainTextAsync(customer.City);
        await Expect(checkout.DeliveryAddress).ToContainTextAsync(customer.Name);
        await checkout.PayWithSyntheticCard(customer, Log.CorrelationId);
        await Expect(checkout.Heading).ToHaveTextAsync("Order Placed!");
        await Expect(checkout.Confirmation).ToBeVisibleAsync();
        await Expect(checkout.Invoice).ToBeVisibleAsync();
        Log.Add("business", $"UI confirmed purchase of product {selected.Id} for {selected.Price}. Order persistence cannot be checked through the published API.");
        (await Api.Lookup(customer.Email)).RequireEnvelope(200);
    }

    [Test, Category("regression")]
    public async Task InvalidLogin_ShowsAnErrorAndDoesNotCreateASession()
    {
        var customer = await NewCustomer();
        var page = await OpenBrowser();
        var account = new AccountScreen(page);
        Act();
        await account.Login(customer.Email, "wrong-" + customer.Password);
        await Expect(account.LoginError).ToBeVisibleAsync();
        await Expect(account.SignInLink).ToBeVisibleAsync();
        await Expect(account.LoggedInName).ToHaveCountAsync(0);
    }
}
