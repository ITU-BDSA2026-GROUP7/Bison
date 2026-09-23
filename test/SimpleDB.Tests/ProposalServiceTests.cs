using SimpleDB;

public class ProposalServiceTests : IDisposable
{
    private const string ValidTaxonId = "MSTSNM:Arter:c28811f4-f785-ea11-aa77-501ac539d1ea";

    private readonly string _observationFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");
    private readonly string _proposalFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");
    private readonly ProposalService _service;
    private readonly CSVDatabase<Proposal> _proposalDatabase;

    public ProposalServiceTests()
    {
        var observations = CSVDatabase<Cheep>.Instance(_observationFile);
        observations.Store(new Cheep(1, "Peter", "Another Heron", 1684229348, "Lake"));

        _proposalDatabase = CSVDatabase<Proposal>.Instance(_proposalFile);
        _service = new ProposalService(observations, _proposalDatabase, TaxonomyLoader.Load());
    }

    public void Dispose()
    {
        File.Delete(_observationFile);
        File.Delete(_proposalFile);
    }

    [Fact]
    public void AddProposal_StoresProposal_WhenObservationAndTaxonAreValid()
    {
        var result = _service.AddProposal(1, ValidTaxonId, "Peter", 1684229349, "Lake");

        Assert.Equal(ProposalResult.Stored, result);
        var stored = Assert.Single(_service.GetProposals(1));
        Assert.Equal(ValidTaxonId, stored.TaxonId);
        Assert.Equal(1, stored.Id);
    }

    [Fact]
    public void AddProposal_AssignsIncreasingIds()
    {
        _service.AddProposal(1, ValidTaxonId, "A", 1, "");
        _service.AddProposal(1, ValidTaxonId, "B", 2, "");

        Assert.Equal(new[] { 1, 2 }, _service.GetProposals(1).Select(p => p.Id));
    }

    [Fact]
    public void AddProposal_RejectsUnknownObservation()
    {
        var result = _service.AddProposal(999, ValidTaxonId, "Peter", 1, "");

        Assert.Equal(ProposalResult.UnknownObservation, result);
        Assert.Empty(_proposalDatabase.Read());
    }

    [Theory]
    [InlineData("not-a-taxon")]
    [InlineData("")]
    [InlineData(null)]
    public void AddProposal_RejectsInvalidTaxonId(string? taxonId)
    {
        var result = _service.AddProposal(1, taxonId!, "Peter", 1, "");

        Assert.Equal(ProposalResult.UnknownTaxon, result);
        Assert.Empty(_proposalDatabase.Read());
    }

    [Fact]
    public void GetProposals_ReturnsOnlyProposalsForRequestedObservation()
    {
        var observations = CSVDatabase<Cheep>.Instance(_observationFile);
        observations.Store(new Cheep(2, "Anna", "Another bird", 2, ""));

        _service.AddProposal(1, ValidTaxonId, "A", 1, "");
        _service.AddProposal(2, ValidTaxonId, "B", 2, "");

        var forFirst = Assert.Single(_service.GetProposals(1));
        Assert.Equal("A", forFirst.Author);
    }
}