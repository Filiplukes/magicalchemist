using System.Globalization;
using System.Text;

/// <summary>
/// Pravidla pro jméno hráče v žebříčku.
/// Unity Authentication odmítne jméno s mezerou nebo delší než 50 znaků (hráč pak v žebříčku
/// zůstane s náhodným jménem typu „DizzyInterestedApple“), proto se jméno před nahráním očistí.
/// Filtr sprostých slov je jednoduchý seznam (anglicky a česky), nezachytí všechno.
/// </summary>
public static class NameRules
{
    public const int MaxLength = 16;

    /// <summary>Jméno, které hráčům ukážeme místo zakázaného.</summary>
    public const string HiddenName = "Alchemist";

    // Jméno se normalizuje: malá písmena, bez diakritiky, 0→o, 1→i, 3→e, 4→a, 5→s, 7→t, @→a, $→s.

    // Hledá se kdekoli ve jménu (i „xXfuckerXx“). Jen slova, která v normálních jménech nejsou.
    private static readonly string[] BannedAnywhere =
    {
        // anglicky
        "fuck", "fck", "shit", "cunt", "bitch", "whore", "slut", "nigger", "nigga",
        "faggot", "retard", "penis", "vagina", "pussy", "dildo", "porn",
        "hitler", "asshole", "bastard", "wanker", "motherf",
        // česky
        "kurv", "kurev", "picus", "picovin", "kokot", "curak", "cural", "zmrd", "mrdk",
        "mrdat", "mrdal", "jebat", "jebal", "sracka", "hovno", "prdel", "debil", "kreten",
        "buzna", "buzerant", "cigan", "hajzl",
    };

    // Jen jako celé slovo (oddělené mezerou, číslem, podtržítkem…), protože jinak
    // by zablokovaly normální jména: Picard, Grape, Hancock, Fukuda, Nazir.
    private static readonly string[] BannedWords =
    {
        "fuk", "cock", "rape", "nazi", "fag",
        "pica", "pico", "picu", "pici", "jebe", "negr", "kunda",
    };

    /// <summary>Jméno bez mezer (nahradí je podtržítko), bez řídicích znaků a zkrácené.</summary>
    public static string Sanitize(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        var sb = new StringBuilder(name.Length);
        bool lastUnderscore = false;
        foreach (char c in name.Trim())
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastUnderscore && sb.Length > 0) sb.Append('_');
                lastUnderscore = true;
                continue;
            }
            if (char.IsControl(c) || c == '#') continue; // '#' používá Unity pro číslo za jménem
            sb.Append(c);
            lastUnderscore = c == '_';
        }
        string result = sb.ToString().Trim('_');
        return result.Length > MaxLength ? result.Substring(0, MaxLength) : result;
    }

    /// <summary>False, když jméno obsahuje sprosté slovo.</summary>
    public static bool IsAllowed(string name)
    {
        string normalized = Normalize(name);
        string joined = normalized.Replace(" ", "");
        foreach (var word in BannedAnywhere)
            if (joined.Contains(word)) return false;
        foreach (var token in normalized.Split(' '))
            foreach (var word in BannedWords)
                if (token == word) return false;
        return true;
    }

    /// <summary>Jméno pro zobrazení v žebříčku: zakázané se schová, dlouhé zkrátí.</summary>
    public static string ForDisplay(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        name = name.Split('#')[0].Replace('_', ' ').Trim(); // podtržítko místo mezery zase jako mezera
        if (!IsAllowed(name)) return HiddenName;
        return name.Length > MaxLength ? name.Substring(0, MaxLength - 1) + "…" : name;
    }

    private static string Normalize(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        var sb = new StringBuilder(name.Length);
        foreach (char ch in name.ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue; // diakritika
            char c = ch switch
            {
                '0' => 'o', '1' => 'i', '3' => 'e', '4' => 'a', '5' => 's', '7' => 't',
                '@' => 'a', '$' => 's', '!' => 'i',
                _ => ch
            };
            // Písmena zůstanou, cokoli jiného oddělí slova.
            if (c >= 'a' && c <= 'z') sb.Append(c);
            else if (sb.Length > 0 && sb[sb.Length - 1] != ' ') sb.Append(' ');
        }
        return sb.ToString().Trim();
    }
}
