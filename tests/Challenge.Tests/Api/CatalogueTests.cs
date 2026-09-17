using Challenge.Support.Browser;
using Challenge.Tests.Fixtures;
using NUnit.Framework;

namespace Challenge.Tests.Api;

[TestFixture, Category("api"), Parallelizable(ParallelScope.All)]
public sealed class CatalogueTests : Scenario
{
    [Test, Category("smoke")]
    public async Task Catalogue_HasDistinctUsableProducts()
    {
        Act();
        var products = (await Api.Catalogue()).Products();
        Assert.That(products, Is.Not.Empty);
        Assert.That(products.Select(p => p.Id), Is.Unique);
        Assert.That(products.All(p => Money.Parse(p.Price) > 0), Is.True, "Every product must have a positive usable price.");
    }

    [Test, Category("smoke")]
    public async Task Search_ReturnsOnlyRelevantProducts()
    {
        Act();
        const string query = "top";
        var products = (await Api.Search(query)).Products();
        Assert.That(products, Is.Not.Empty);
        Assert.That(products.Select(p => p.Id), Is.Unique);
        foreach (var product in products)
            Assert.That(new[] { product.Name, product.Brand, product.Category, product.Audience }
                .Any(value => value.Contains(query, StringComparison.OrdinalIgnoreCase)), Is.True,
                $"Product {product.Id} has no searchable field matching '{query}'.");
    }

    [Test, Category("regression")]
    public async Task Search_MissingParameter_HasAnExplicitError()
    {
        Act();
        var reply = await Api.Send(HttpMethod.Post, "searchProduct", new Dictionary<string, string>());
        reply.RequireEnvelope(400);
        Assert.That(reply.Message, Does.Contain("search_product").And.Contain("missing"));
    }

    [TestCase("POST", "productsList"), TestCase("PUT", "brandsList"), Category("regression")]
    public async Task UnsupportedMethod_IsRejectedByApplicationContract(string method, string resource)
    {
        Act();
        var reply = await Api.Send(new HttpMethod(method), resource);
        reply.RequireEnvelope(405);
        Assert.That(reply.Message, Does.Contain("not supported").IgnoreCase);
    }
}
