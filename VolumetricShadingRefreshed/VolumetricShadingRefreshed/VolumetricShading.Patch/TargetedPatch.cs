using System;

namespace volumetricshadingupdated.VolumetricShading.Patch;

public abstract class TargetedPatch : IShaderPatch
{
    public bool ExactFilename;
    public string TargetFile;

    public bool ShouldPatch(string filename, string code)
    {
        var text = TargetFile ?? "";
        if (!ExactFilename)
        {
            return filename.ToLowerInvariant().Contains(text.ToLowerInvariant());
        }

        return text.Equals(filename, StringComparison.InvariantCultureIgnoreCase);
    }

    public abstract string Patch(string filename, string code);
}