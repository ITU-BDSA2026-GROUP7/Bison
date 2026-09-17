using SimpleDB;

const string observeFile = "bison_observe_cli_db.csv";
const string commentFile = "bison_comment.csv";

IDatabaseRepository<Cheep> database = CSVDatabase<Cheep>.Instance(observeFile);
IDatabaseRepository<Comment> commentDatabase = CSVDatabase<Comment>.Instance(commentFile);

var observationService = new ObservationService(database);
var commentService = new CommentService(database, commentDatabase);

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/observation", (Cheep request) =>
{
    Console.WriteLine("POST /observations received");

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
    if ((database.Read().Any(o => o.Id == request.ObservationId))) {

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
    commentDatabase.Read().Where(comment => comment.ObservationId == observationId);
    return Results.Ok();
});
 

app.MapGet("/location", (string location) =>
{
    database.Read().Where(observation => observation.Location == location);
    return Results.Ok();
});

app.Run();

// public record ObservationRequest(string Author, string Message, long Timestamp, string? Location = null);

// public record CommentRequest(string Author, string Message, long Timestamp, int ObservationId, string? Location = null);
