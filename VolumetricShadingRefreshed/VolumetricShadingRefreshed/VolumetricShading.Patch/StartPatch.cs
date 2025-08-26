using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace volumetricshadingupdated.VolumetricShading.Patch;

public class StartPatch : TargetedPatch
{
    public static readonly Regex
        SkipLineRegex = new("^\\s*?#\\s*?(?:version|extension)", RegexOptions.IgnoreCase);

    public string Content;

    public StartPatch()
    {
    }

    public StartPatch(string filename)
    {
        TargetFile = filename;
        ExactFilename = true;
    }

    public override string Patch(string filename, string code)
    {
        var stringBuilder = new StringBuilder(code.Length);
        using (var stringReader = new StringReader(code))
        {
            string text;
            while ((text = stringReader.ReadLine()) != null)
            {
                if (SkipLineRegex.IsMatch(text))
                {
                    stringBuilder.AppendLine(text);
                    continue;
                }

                stringBuilder.AppendLine(Content);
                stringBuilder.AppendLine(text);
                break;
            }

            stringBuilder.Append(stringReader.ReadToEnd());
        }

        return stringBuilder.ToString();
    }
}