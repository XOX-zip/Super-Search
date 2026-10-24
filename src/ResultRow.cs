using System.ComponentModel;
using System.Windows;

namespace SuperSearch;

sealed class ResultRow : INotifyPropertyChanged
{
    bool _showScore;

    public ResultRow(SearchResult result) => Result = result;

    public SearchResult Result { get; }

    public string Title => Result.Title;

    public string Url => Result.Url;

    public string Snippet => Result.Snippet.Length == 0 ? "（无摘要）" : Result.Snippet;

    public string Host
    {
        get
        {
            try { return new Uri(Result.Url).Host; }
            catch { return ""; }
        }
    }

    public string ScoreText => _showScore ? $"评分 {Result.Score:F1}" : "";

    public Visibility ScoreVisibility => _showScore ? Visibility.Visible : Visibility.Collapsed;

    public bool ShowScore
    {
        get => _showScore;
        set
        {
            if (_showScore == value) return;
            _showScore = value;
            Raise(nameof(ScoreText));
            Raise(nameof(ScoreVisibility));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
