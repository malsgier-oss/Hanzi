namespace HanziOverlay.Core.Services.Pinyin;

public class NPinyinService : IPinyinService
{
    public string ToPinyinWithTones(string cnText)
    {
        if (string.IsNullOrWhiteSpace(cnText)) return "";

        try
        {
            string result = NPinyin.Pinyin.GetPinyin(cnText);
            return ConvertNumberTonesToMarks(result ?? "");
        }
        catch
        {
            return cnText;
        }
    }

    /// <summary>
    /// Converts pinyin with number tones (e.g. "ni3 hao3") to tone marks (e.g. "nǐ hǎo").
    /// </summary>
    private static string ConvertNumberTonesToMarks(string pinyin)
    {
        if (string.IsNullOrEmpty(pinyin)) return pinyin;

        var sb = new System.Text.StringBuilder();
        int i = 0;
        while (i < pinyin.Length)
        {
            char c = pinyin[i];
            if (i + 1 < pinyin.Length && pinyin[i + 1] >= '1' && pinyin[i + 1] <= '5')
            {
                int tone = pinyin[i + 1] - '0';
                if (tone == 5) tone = 0;
                char withTone = AddToneToVowel(c, tone);
                sb.Append(withTone);
                i += 2;
                continue;
            }
            sb.Append(c);
            i++;
        }
        return sb.ToString();
    }

    private static char AddToneToVowel(char c, int tone)
    {
        if (tone <= 0) return c;
        return c switch
        {
            'a' => tone switch { 1 => 'ā', 2 => 'á', 3 => 'ǎ', 4 => 'à', _ => c },
            'e' => tone switch { 1 => 'ē', 2 => 'é', 3 => 'ě', 4 => 'è', _ => c },
            'i' => tone switch { 1 => 'ī', 2 => 'í', 3 => 'ǐ', 4 => 'ì', _ => c },
            'o' => tone switch { 1 => 'ō', 2 => 'ó', 3 => 'ǒ', 4 => 'ò', _ => c },
            'u' => tone switch { 1 => 'ū', 2 => 'ú', 3 => 'ǔ', 4 => 'ù', _ => c },
            'v' or 'ü' => tone switch { 1 => 'ǖ', 2 => 'ǘ', 3 => 'ǚ', 4 => 'ǜ', _ => c },
            'A' => tone switch { 1 => 'Ā', 2 => 'Á', 3 => 'Ǎ', 4 => 'À', _ => c },
            'E' => tone switch { 1 => 'Ē', 2 => 'É', 3 => 'Ě', 4 => 'È', _ => c },
            'I' => tone switch { 1 => 'Ī', 2 => 'Í', 3 => 'Ǐ', 4 => 'Ì', _ => c },
            'O' => tone switch { 1 => 'Ō', 2 => 'Ó', 3 => 'Ǒ', 4 => 'Ò', _ => c },
            'U' => tone switch { 1 => 'Ū', 2 => 'Ú', 3 => 'Ǔ', 4 => 'Ù', _ => c },
            _ => c
        };
    }
}
