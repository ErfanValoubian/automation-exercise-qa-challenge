using System.Text.Json;
using Challenge.Support.Api;
using Challenge.Support.Browser;
using Challenge.Support.Diagnostics;
using NUnit.Framework;
using Challenge.Tests.Fixtures;

namespace Challenge.Tests.Harness;

[TestFixture, Category("harness"), Parallelizable(ParallelScope.All)]
public sealed class SupportTests : Scenario
{
    [TestCase("Rs. 500", 500), TestCase("Rs. 1,250.50", 1250.50)]
    public void PriceParser_PreservesNumericValue(string input, decimal expected) => Assert.That(Money.Parse(input), Is.EqualTo(expected));
    [TestCase("Rs. 0.50.20"), TestCase("USD 500"), TestCase("Rs. free")]
    public void PriceParser_RejectsAmbiguousInput(string input) => Assert.Throws<FormatException>(() => Money.Parse(input));
    [TestCase("<html>blocked</html>"), TestCase("{\"responseCode\":200}"), TestCase("{\"responseCode\":200,\"products\":null}")]
    public void ProductContract_CannotPassOnHtmlOrMissingCollection(string raw)
    {
        JsonElement? json = null;
        try { using var doc = JsonDocument.Parse(raw); json = doc.RootElement.Clone(); } catch (JsonException) { }
        Assert.Throws<ContractException>(() => new ApiReply(200, raw, json, 1).Products());
    }
    [Test]
    public void Redaction_CoversNestedFieldsAndRegisteredSecrets()
    {
        var redactor = new Redactor();
        redactor.Register("unique-secret");
        var clean = redactor.Body("{\"outer\":{\"password\":\"abc\",\"email\":\"qa@example.com\"},\"message\":\"unique-secret\"}");
        Assert.That(clean, Does.Not.Contain("abc").And.Not.Contain("qa@example.com").And.Not.Contain("unique-secret"));
    }
    [Test]
    public void ProductContract_RejectsStringResponseCode()
    {
        using var doc = JsonDocument.Parse("{\"responseCode\":\"200\",\"products\":[]}");
        Assert.Throws<ContractException>(() => new ApiReply(200, "", doc.RootElement, 1).Products());
    }
}
