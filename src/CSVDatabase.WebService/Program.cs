using SimpleDB;

const string observeFile = "bison_observe_cli_db.csv";
const string commentFile = "bison_comment.csv";

IDatabaseRepository<Cheep> database = CSVDatabase<Cheep>.Instance(observeFile);
IDatabaseRepository<Comment> commentDatabase = CSVDatabase<Comment>.Instance(commentFile);

var observationService = new ObservationService(database);
var commentService = new CommentService(database, commentDatabase);

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// POST /observation — store a new observation.
// Body: {"Author":"Peter","Message":"Another Heron","Timestamp":1684229348}
// "Location" is optional; it defaults to an empty string if left out.
app.MapPost("/observation", (ObservationRequest request) =>
{
    observationService.AddObservation(
        request.Message,
        request.Author,
        request.Timestamp,
        request.Location ?? string.Empty);

    return Results.Ok();
});

// GET /observations — list every stored observation as JSON.
app.MapGet("/observations", () => database.Read());

// POST /comment — store a new comment on an existing observation.
// Body: {"Author":"Peter","Message":"Nice!","Timestamp":1684229400,"ObservationId":3}
app.MapPost("/comment", (CommentRequest request) =>
{
    commentService.AddComment(
        request.ObservationId,
        request.Message,
        request.Author,
        request.Timestamp,
        request.Location ?? string.Empty);

    return Results.Ok();
});

// GET /comments?observationId=3 — list every comment for one observation.
app.MapGet("/comments", (int observationId) =>
    commentDatabase.Read().Where(comment => comment.ObservationId == observationId));

app.Run();

// The shapes the two POST endpoints accept over the wire.
// These are separate from Cheep/Comment because the caller never sends an Id
// (the service assigns the next one, exactly like Bison.CLI already does).
public record ObservationRequest(string Author, string Message, long Timestamp, string? Location = null);

public record CommentRequest(string Author, string Message, long Timestamp, int ObservationId, string? Location = null);
