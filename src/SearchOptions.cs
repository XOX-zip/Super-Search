namespace SuperSearch;

class SearchOptions
{
    public int Limit { get; set; } = 10;
    public bool NoAi { get; set; }
    public bool Verbose { get; set; }
    public string? Site { get; set; }
}