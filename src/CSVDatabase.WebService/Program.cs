using SimpleDB;

string observeFile = Environment.GetEnvironmentVariable("OBSERVE_FILE") ?? "bison_observe_cli_db.csv";
string commentFile = Environment.GetEnvironmentVariable("COMMENT_FILE") ?? "bison_comment.csv";
string proposalFile = Environment.GetEnvironmentVariable("PROPOSAL_FILE") ?? "bison_proposal.csv";

Console.WriteLine(Environment.GetEnvironmentVariable("OBSERVE_FILE"));

IDatabaseRepository<Cheep> database = CSVDatabase<Cheep>.Instance(observeFile);
IDatabaseRepository<Comment> commentDatabase = CSVDatabase<Comment>.Instance(commentFile);
IDatabaseRepository<Proposal> proposalDatabase = CSVDatabase<Proposal>.Instance(proposalFile);

var taxonomy = TaxonomyLoader.Load();

var observationService = new ObservationService(database);
var commentService = new CommentService(database, commentDatabase);
var proposalService = new ProposalService(database, proposalDatabase, taxonomy);

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/observation", (Cheep request) =>
{
    observationService.AddObservation(
        request.Message,
        request.Author,
        request.Timestamp,
        request.Location ?? string.Empty);

    return Results.Ok();
});

app.MapGet("/observations", () => 
{
    return Results.Ok(database.Read());
});
    
app.MapPost("/comment", (Comment request) =>
{
    if (database.Read().Any(o => o.Id == request.ObservationId)) {

        commentService.AddComment(
            request.ObservationId,
            request.Message,
            request.Author,
            request.Timestamp,
            request.Location ?? string.Empty);

        return Results.Ok();
    }
    
    else return Results.BadRequest("Observation ID does not exist.");

});

app.MapGet("/comments", (int observationId) => 
{
    return Results.Ok(commentDatabase.Read().Where(comment => comment.ObservationId == observationId));
});
 
app.MapPost("/proposal", (Proposal request) =>
{
    var result = proposalService.AddProposal(
        request.ObservationId,
        request.TaxonId,
        request.Author,
        request.Timestamp,
        request.Location ?? string.Empty);

    return result switch
    {
        ProposalResult.Stored => Results.Ok(),
        ProposalResult.UnknownObservation => Results.BadRequest("Observation ID does not exist."),
        _ => Results.BadRequest("Taxon ID does not exist.")
    };
});

app.MapGet("/proposals", (int observationId) =>
{
    return Results.Ok(proposalService.GetProposals(observationId));
});

app.MapGet("/location", (string location) =>
{
    return Results.Ok(observationService.GetObservationsByLocation(location));
});

app.Run();
