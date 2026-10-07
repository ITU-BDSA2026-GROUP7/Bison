public class ObservationListDTO
{
    public int Id {get; set;}
    public string Text {get; set;} = ""; 
    public string AuthorName {get; set;} = ""; 
    public string TaxonName {get; set;} = ""; 
    public string Timestamp {get; set;} = ""; 

    public List<CommentDTO> Comments {get; set;} = new();

    public List<ProposalDTO> Proposals {get; set;} = new(); 
}