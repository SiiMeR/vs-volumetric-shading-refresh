using System.Text;
using System.Text.RegularExpressions;

namespace volumetricshadingupdated.VolumetricShading.Patch;

public class TokenPatch : RegexPatch
{
    private const string TokenSeparators = ".,+-*/;{}[]()=:|^&?#";

    private const string StartToken = "(^|[\\.,+\\-*/;{}[\\]()=:|^&?#\\s])";

    private const string EndToken = "($|[\\.,+\\-*/;{}[\\]()=:|^&?#\\s])";

    private const string OptionalRegexSeparator = "\\s*?";

    private const string RegexSeparator = "\\s+?";

    private static Regex BuildRegex(string tokenStr)
    {
        StringBuilder stringBuilder = new StringBuilder(tokenStr.Length);
        bool flag = false;
        bool flag2 = false;
        string text = tokenStr;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c))
            {
                if (!(flag || flag2))
                {
                    stringBuilder.Append(' ');
                    flag = true;
                }
            }
            else if (".,+-*/;{}[]()=:|^&?#".Contains(c.ToString()))
            {
                if (flag)
                {
                    stringBuilder.Remove(stringBuilder.Length - 1, 1);
                }

                stringBuilder.Append(c);
                flag = false;
                flag2 = true;
            }
            else
            {
                stringBuilder.Append(c);
                flag = false;
                flag2 = false;
            }
        }

        string text2 = stringBuilder.ToString().Trim();
        stringBuilder.Clear();
        stringBuilder.Append("(^|[\\.,+\\-*/;{}[\\]()=:|^&?#\\s])");
        bool flag3 = true;
        int num = 0;
        text = text2;
        for (int i = 0; i < text.Length; i++)
        {
            char c2 = text[i];
            string text3 = Regex.Escape(c2.ToString());
            if (c2 == ' ')
            {
                if (flag3 && num > 0)
                {
                    stringBuilder.Remove(stringBuilder.Length - num, num);
                }

                stringBuilder.Append("\\s+?");
                flag3 = true;
                num = "\\s+?".Length;
            }
            else if (".,+-*/;{}[]()=:|^&?#".Contains(c2.ToString()))
            {
                if (num != 0 && !flag3)
                {
                    stringBuilder.Append("\\s*?");
                }

                stringBuilder.Append(text3);
                stringBuilder.Append("\\s*?");
                flag3 = true;
                num = "\\s*?".Length;
            }
            else
            {
                stringBuilder.Append(text3);
                flag3 = false;
                num = text3.Length;
            }
        }

        if (flag3 && num > 0)
        {
            stringBuilder.Remove(stringBuilder.Length - num, num);
        }

        stringBuilder.Append("($|[\\.,+\\-*/;{}[\\]()=:|^&?#\\s])");
        return new Regex(stringBuilder.ToString(), RegexOptions.IgnoreCase);
    }

    public TokenPatch(string tokenString)
        : base(BuildRegex(tokenString))
    {
        DoReplace = TokenReplace;
    }

    public TokenPatch(string filename, string tokenString)
        : base(filename, BuildRegex(tokenString))
    {
        DoReplace = TokenReplace;
    }

    private void TokenReplace(StringBuilder sb, Match match)
    {
        sb.Append(match.Groups[1].Value);
        sb.Append(ReplacementString ?? "");
        sb.Append(match.Groups[2].Value);
    }
}