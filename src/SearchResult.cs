namespace SuperSearch;

record SearchResult(string Title, string Url, string Snippet)
{
    public double Score { get; set; }
}