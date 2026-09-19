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
 

app.MapGet("/location", (string location) =>
{
    return Results.Ok(observationService.GetObservationsByLocation(location));
});

app.Run();
