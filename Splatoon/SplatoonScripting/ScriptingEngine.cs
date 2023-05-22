using Newtonsoft.Json;

namespace Splatoon.SplatoonScripting;

public class ScriptingEngine
{
    /// <summary>
    /// Attempts to decode layout from string.
    /// </summary>
    /// <param name="s">Input string.</param>
    /// <param name="l">Resulting layout.</param>
    /// <returns>Whether operation succeeded.</returns>
    public static bool TryDecodeLayout(string s, out Layout l)
    {
        try
        {
            if (s.StartsWith("~Lv2~"))
            {
                s = s[5..];
                l = JsonConvert.DeserializeObject<Layout>(s);
                l.Name = l.Name.SanitizeName();
                return true;
            }
            else
            {
                l = DeserializeLegacyLayout(s);
                return true;
            }
        }
        catch (Exception e)
        {
            e.LogWarning();
            l = null;
            return false;
        }
    }


    /// <summary>
    /// Attempts to decode element from string.
    /// </summary>
    /// <param name="s">Input string.</param>
    /// <param name="element">Resulting element.</param>
    /// <returns>Whether operation succeeded.</returns>
    public static bool TryDecodeElement(string s, out Element element)
    {
        try
        {
            element = JsonConvert.DeserializeObject<Element>(s);
            return true;
        }
        catch (Exception e)
        {
            e.LogWarning();
            element = null;
            return false;
        }
    }
}
