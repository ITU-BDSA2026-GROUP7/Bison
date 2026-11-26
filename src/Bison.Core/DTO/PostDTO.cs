public abstract class PostDTO
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public int UpVotes { get; set; }
    public int DownVotes { get; set; }
}