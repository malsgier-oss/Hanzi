using System.Collections.Generic;

namespace HanziOverlay.Core.Services.Translation;

public class TranslationCache
{
    private const int MaxEntries = 500;
    private readonly Dictionary<int, string> _cache = new();
    private readonly LinkedList<int> _order = new();

    public bool TryGet(int key, out string? value)
    {
        lock (_cache)
        {
            if (_cache.TryGetValue(key, out value))
            {
                _order.Remove(key);
                _order.AddLast(key);
                return true;
            }
        }
        value = null;
        return false;
    }

    public void Set(int key, string value)
    {
        lock (_cache)
        {
            if (_cache.Count >= MaxEntries && _order.First is { } first)
            {
                _cache.Remove(first.Value);
                _order.RemoveFirst();
            }
            _cache[key] = value;
            _order.Remove(key);
            _order.AddLast(key);
        }
    }

    public static int HashCnText(string cnText)
    {
        if (string.IsNullOrEmpty(cnText)) return 0;
        return cnText.GetHashCode(StringComparison.Ordinal);
    }
}
