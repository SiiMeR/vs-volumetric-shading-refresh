using System;
using System.Text;
using System.Text.RegularExpressions;

namespace volumetricshadingupdated.VolumetricShading.Patch;

public class RegexPatch : TargetedPatch
{
    public delegate void ReplacementFunction(StringBuilder sb, Match match);

    public ReplacementFunction DoReplace;

    public bool Multiple;

    public bool Optional;

    public string ReplacementString;

    public RegexPatch(Regex regex)
    {
        Regex = regex;
        DoReplace = DefaultReplace;
    }

    public RegexPatch(string filename, Regex regex)
        : this(regex)
    {
        TargetFile = filename;
        ExactFilename = true;
    }

    public RegexPatch(string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        : this(new Regex(pattern, options))
    {
    }

    public RegexPatch(string filename, string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        : this(filename, new Regex(pattern, options))
    {
    }

    public Regex Regex { get; }

    public override string Patch(string filename, string code)
    {
        var match = Regex.Match(code);
        if (!match.Success)
        {
            if (!Optional)
            {
                throw new InvalidOperationException(
                    $"Could not execute non-optional patch: Regex {Regex} not matched in file {filename}");
            }

            return code;
        }

        var stringBuilder = new StringBuilder(code.Length);
        var num = 0;
        do
        {
            if (match.Index != num)
            {
                stringBuilder.Append(code, num, match.Index - num);
            }

            num = match.Index + match.Length;
            DoReplace(stringBuilder, match);
            match = match.NextMatch();
        } while (match.Success && Multiple);

        if (match.Success)
        {
            throw new InvalidOperationException($"Multiple regex matches, but only one wanted: {Regex}");
        }

        if (num < code.Length)
        {
            stringBuilder.Append(code, num, code.Length - num);
        }

        return stringBuilder.ToString();
    }

    private void DefaultReplace(StringBuilder sb, Match match)
    {
        sb.Append(ReplacementString ?? "");
    }
}