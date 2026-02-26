using HanziOverlay.Core.Models;

namespace HanziOverlay.Core.Services.Stabilization;

public class SubtitleStabilizer : ISubtitleStabilizer
{
    private readonly int _requiredConsecutiveFrames;
    private readonly double _similarityThreshold;
    private readonly double _highConfidenceThreshold;

    private string _candidateText = "";
    private int _candidateCount;
    private string _lastStableText = "";

    public SubtitleStabilizer(
        int requiredConsecutiveFrames = 2,
        double similarityThreshold = 0.80,
        double highConfidenceThreshold = 0.85)
    {
        _requiredConsecutiveFrames = requiredConsecutiveFrames;
        _similarityThreshold = similarityThreshold;
        _highConfidenceThreshold = highConfidenceThreshold;
    }

    public event EventHandler<StableSubtitle>? StableSubtitleChanged;

    public void PushOcrResult(OcrResult result)
    {
        string normalized = NormalizeText(result.Text);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            _candidateCount = 0;
            return;
        }

        if (IsSimilar(normalized, _candidateText))
        {
            _candidateCount++;
            if (_candidateCount >= _requiredConsecutiveFrames || result.Confidence >= _highConfidenceThreshold)
            {
                if (!IsSimilar(normalized, _lastStableText))
                {
                    _lastStableText = normalized;
                    StableSubtitleChanged?.Invoke(this, new StableSubtitle(normalized, result.Confidence));
                }
                _candidateText = normalized;
                _candidateCount = 0;
            }
        }
        else
        {
            _candidateText = normalized;
            _candidateCount = 1;
        }
    }

    public void Reset()
    {
        _candidateText = "";
        _candidateCount = 0;
        _lastStableText = "";
    }

    public static string NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        string t = text.Trim();
        t = System.Text.RegularExpressions.Regex.Replace(t, @"\s+", " ");
        t = t.Replace("　", " ");
        return t.Trim();
    }

    private bool IsSimilar(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        if (a == b) return true;
        int distance = LevenshteinDistance(a, b);
        int maxLen = Math.Max(a.Length, b.Length);
        return (1.0 - (double)distance / maxLen) >= _similarityThreshold;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        int[,] d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[a.Length, b.Length];
    }
}
