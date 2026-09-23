namespace SimpleDB;

public enum ProposalResult { Stored, UnknownObservation, UnknownTaxon }



public class ProposalService
{
    private readonly IDatabaseRepository<Cheep> _observationDatabase;
    private readonly IDatabaseRepository<Proposal> _proposalDatabase;
    private readonly TaxonomyRepository _taxonomy;

    public ProposalService(
        IDatabaseRepository<Cheep> observationDatabase,
        IDatabaseRepository<Proposal> proposalDatabase,
        TaxonomyRepository taxonomy)
    {
        _observationDatabase = observationDatabase;
        _proposalDatabase = proposalDatabase;
        _taxonomy = taxonomy;
    }

    public ProposalResult AddProposal(
        int observationId,
        string taxonId,
        string author,
        long timestamp,
        string location)
    {
        if (!_observationDatabase.Read().Any(o => o.Id == observationId))
        {
            return ProposalResult.UnknownObservation;
        }

        if (!_taxonomy.Contains(taxonId))
        {
            return ProposalResult.UnknownTaxon;
        }

        var existing = _proposalDatabase.Read();

        int nextId = existing.Any()
            ? existing.Max(p => p.Id) + 1
            : 1;

        _proposalDatabase.Store(
            new Proposal(nextId, author, taxonId, timestamp, observationId, location));

        return ProposalResult.Stored;
    }

    public IEnumerable<Proposal> GetProposals(int observationId)
    {
        return _proposalDatabase.Read().Where(p => p.ObservationId == observationId);
    }
}