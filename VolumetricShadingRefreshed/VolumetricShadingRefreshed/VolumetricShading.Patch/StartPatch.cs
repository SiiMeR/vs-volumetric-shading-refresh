using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace volumetricshadingupdated.VolumetricShading.Patch;

public class StartPatch : TargetedPatch
{
    public string Content;

    public static readonly Regex
        SkipLineRegex = new Regex("^\\s*?#\\s*?(?:version|extension)", RegexOptions.IgnoreCase);

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
        StringBuilder stringBuilder = new StringBuilder(code.Length);
        using (StringReader stringReader = new StringReader(code))
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