using Challenge.Support.Api;
using Challenge.Support.Data;
using Challenge.Tests.Fixtures;
using NUnit.Framework;

namespace Challenge.Tests.Api;

[TestFixture, Category("api"), Parallelizable(ParallelScope.All)]
public sealed class AccountTests : Scenario
{
    [Test, Category("smoke")]
    public async Task CreatedCustomer_CanAuthenticateAndBeRetrieved()
    {
        var customer = await NewCustomer();
        Act();
        var login = await Api.Login(customer.Email, customer.Password);
        login.RequireEnvelope(200);
        Assert.That(login.Message, Does.Contain("User exists"));
        var user = (await Api.Lookup(customer.Email)).RequireEnvelope(200).GetProperty("user");
        Assert.Multiple(() =>
        {
            Assert.That(Product.Text(user, "name"), Is.EqualTo(customer.Name));
            Assert.That(Product.Text(user, "email"), Is.EqualTo(customer.Email));
        });
    }

    [Test, Category("regression")]
    public async Task Lifecycle_UpdatePersistsAndDeletionRevokesAccess()
    {
        var customer = await NewCustomer();
        Act();
        var revised = customer with { Name = "Updated QA " + Log.CorrelationId[..8], City = "Updated City" };
        (await Api.Send(HttpMethod.Put, "updateAccount", revised.Form())).RequireEnvelope(200);
        var stored = (await Api.Lookup(customer.Email)).RequireEnvelope(200).GetProperty("user");
        Assert.Multiple(() =>
        {
            Assert.That(Product.Text(stored, "name"), Is.EqualTo(revised.Name));
            Assert.That(Product.Text(stored, "city"), Is.EqualTo(revised.City));
            Assert.That(Product.Text(stored, "email"), Is.EqualTo(customer.Email));
        });
        (await Api.Delete(customer.Email, customer.Password)).RequireEnvelope(200);
        (await Api.Lookup(customer.Email)).RequireEnvelope(404);
        (await Api.Login(customer.Email, customer.Password)).RequireEnvelope(404);
    }

    [Test, Category("regression")]
    public async Task DuplicateEmail_IsRejectedWithoutChangingTheOriginal()
    {
        var customer = await NewCustomer();
        Act();
        var duplicate = customer with { Name = "Unwanted replacement" };
        var reply = await Api.Send(HttpMethod.Post, "createAccount", duplicate.Form());
        reply.RequireEnvelope(400);
        Assert.That(reply.Message, Does.Contain("already exists").IgnoreCase);
        var stored = (await Api.Lookup(customer.Email)).RequireEnvelope(200).GetProperty("user");
        Assert.That(Product.Text(stored, "name"), Is.EqualTo(customer.Name));
    }

    [Test, Category("regression")]
    public async Task WrongPassword_IsRejectedForAnExistingCustomer()
    {
        var customer = await NewCustomer();
        Act();
        var reply = await Api.Login(customer.Email, "wrong-" + customer.Password);
        reply.RequireEnvelope(404);
        Assert.That(reply.Message, Does.Contain("not found").IgnoreCase);
    }

    [Test, Category("regression")]
    public async Task UnknownCustomer_CannotAuthenticate()
    {
        var customer = Customer.Unique(Worker);
        Log.Redactor.Register(customer.Email, customer.Password);
        Act();
        var reply = await Api.Login(customer.Email, customer.Password);
        reply.RequireEnvelope(404);
        Assert.That(reply.Message, Does.Contain("not found").IgnoreCase);
    }

    [Test, Category("regression")]
    public async Task Authentication_MissingEmail_IsRejected()
    {
        Act();
        var reply = await Api.Send(HttpMethod.Post, "verifyLogin", new Dictionary<string, string> { ["password"] = "synthetic-invalid" });
        reply.RequireEnvelope(400);
        Assert.That(reply.Message, Does.Contain("missing").IgnoreCase);
    }
}
