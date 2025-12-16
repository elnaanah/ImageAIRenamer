namespace ImageAIRenamer.Models
{
    public class SearchResult
    {
        public bool IsMatch { get; set; }
        public string? Confidence { get; set; }
        public string? SuggestedName { get; set; }
        public string? Reason { get; set; }
    }
}
