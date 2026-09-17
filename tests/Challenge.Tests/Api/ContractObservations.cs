using System.Text.Json;
using Challenge.Tests.Fixtures;
using NUnit.Framework;

namespace Challenge.Tests.Api;

[TestFixture, Category("observation"), Parallelizable(ParallelScope.All)]
public sealed class ContractObservations : Scenario
{
    // Deliberately outside smoke/regression/api gates: an undocumented policy is not a confirmed defect.
    [Test]
    public async Task EmptySearch_RecordsSemanticsWithoutInventingARequirement()
    {
        Act();
        var catalogue = (await Api.Catalogue()).Products();
        var reply = await Api.Search("");
        Assert.That(reply.Code, Is.AnyOf(200, 400), "Only a valid success or validation envelope is acceptable.");
        reply.RequireEnvelope(reply.Code!.Value);
        if (reply.Code == 400)
        {
            Assert.That(reply.Message, Is.Not.Empty);
            Log.Add("quality-risk", "Empty parameter is rejected. Missing and empty are distinct requests; policy remains undocumented.");
            return;
        }
        var returned = reply.Products();
        Assert.That(returned.Select(p => p.Id), Is.Unique);
        Assert.That(returned.Select(p => p.Id), Is.SubsetOf(catalogue.Select(p => p.Id)));
        var same = catalogue.Select(p => p.Id).Order().SequenceEqual(returned.Select(p => p.Id).Order());
        Log.Add("quality-risk", $"Empty search: HTTP {reply.HttpStatus}; application {reply.Code}; catalogue={catalogue.Count}; returned={returned.Count}; same ID set={same}.",
            JsonSerializer.Serialize(new { catalogueIds = catalogue.Select(p => p.Id), returnedIds = returned.Select(p => p.Id) }));
    }
}
