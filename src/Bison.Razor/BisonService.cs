using Bison.Razor.Models;
using Bison.Razor.Repositories;

public record ObservationViewModel(int Id, string Author, string Message, string Timestamp);

public record CommentViewModel(int ObservationId, string Author, string Message, string Timestamp);

public record ProposalViewModel(int Id, string TaxonName, string Author, string Message, string Timestamp);

public record ObservationPage(
    List<ObservationViewModel> Observations,
    bool HasNextPage
);

public record CommentPage(
    List<CommentViewModel> Observations,
    bool HasNextPage
);

public record ProposalPage(
    List<ProposalViewModel> Observations,
    bool HasNextPage
);

public record ObservationDetailsPage(
    ObservationViewModel Observation, CommentPage Comments, ProposalPage Proposals);

public interface IObservationService
{
    public ObservationPage GetObservations(int page);
    public ObservationPage GetObservationsFromAuthor(string author, int page);
    public ObservationViewModel? GetObservation(int id);
}

public class ObservationService : IObservationService
{
    private const int PageSize = 32;

    private readonly IPostRepository _repository;

    public ObservationService(IPostRepository repository)
    {
        _repository = repository;
    }

    public ObservationPage GetObservations(int page)
    {
        // Ask for one extra row to find out whether there is a next page.
        var rows = _repository.GetObservations((page - 1) * PageSize, PageSize + 1);

        var hasNextPage = rows.Count > PageSize;

        var observations = rows
            .Take(PageSize)
            .Select(ToViewModel)
            .ToList();

        return new ObservationPage(observations, hasNextPage);
    }

    public ObservationPage GetObservationsFromAuthor(string author, int page)
    {
        var rows = _repository.GetObservationsFromAuthor(author, (page - 1) * PageSize, PageSize + 1);

        var hasNextPage = rows.Count > PageSize;

        var observations = rows
            .Take(PageSize)
            .Select(ToViewModel)
            .ToList();

        return new ObservationPage(observations, hasNextPage);
    }

    public ObservationViewModel? GetObservation(int id)
    {
        var observation = _repository.GetObservation(id);

        return observation == null ? null : ToViewModel(observation);
    }

    private static ObservationViewModel ToViewModel(ObservationDetailsDTO observation)
    {
        return new ObservationViewModel(
            observation.Id,
            observation.AuthorName,
            observation.Text,
            observation.Timestamp
        );
    }
    private static ObservationViewModel ToViewModel(ObservationListDTO observation)
    {
        return new ObservationViewModel(
            observation.Id,
            observation.AuthorName,
            observation.Text,
            observation.Timestamp
        );
    }
}

public interface ICommentService
{
    CommentPage GetComments(int observationId, int page);
}

public class CommentService : ICommentService
{
    private const int PageSize = 32;

    private readonly IPostRepository _repository;

    public CommentService(IPostRepository repository)
    {
        _repository = repository;
    }

    public CommentPage GetComments(int observationId, int page)
    {
        var rows = _repository.GetComments(observationId, (page - 1) * PageSize, PageSize + 1);

        var hasNextPage = rows.Count > PageSize;

        var comments = rows
            .Take(PageSize)
            .Select(ToViewModel)
            .ToList();

        return new CommentPage(comments, hasNextPage);
    }

    private static CommentViewModel ToViewModel(CommentDTO comment)
    {
        return new CommentViewModel(
            comment.Id,
            comment.AuthorName,
            comment.Text,
            comment.Timestamp
        );
    }
}

public interface IProposalService
{
    ProposalPage GetProposals(int observationId, int page);
}

public class ProposalService : IProposalService
{
    private const int PageSize = 32;

    private readonly IPostRepository _repository;

    public ProposalService(IPostRepository repository)
    {
        _repository = repository;
    }

    public ProposalPage GetProposals(int observationId, int page)
    {
        var rows = _repository.GetProposals(observationId, (page - 1) * PageSize, PageSize + 1);

        var hasNextPage = rows.Count > PageSize;

        var proposals = rows
            .Take(PageSize)
            .Select(ToViewModel)
            .ToList();

        return new ProposalPage(proposals, hasNextPage);
    }

    private static ProposalViewModel ToViewModel(ProposalDTO proposal)
    {
        return new ProposalViewModel(
            proposal.Id,
            proposal.TaxonName,
            proposal.AuthorName,
            proposal.Text,
            proposal.Timestamp
        );
    }
}

public static class Methods
{
    public static string DateTimeToString(DateTime dateTime)
    {
        return dateTime.ToString("MM/dd/yy H:mm:ss");
    }
}