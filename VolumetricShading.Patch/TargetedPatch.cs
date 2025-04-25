using System;

namespace VolumetricShading.Patch;

public abstract class TargetedPatch : IShaderPatch
{
	public string TargetFile;

	public bool ExactFilename;

	public bool ShouldPatch(string filename, string code)
	{
		string text = TargetFile ?? "";
		if (!ExactFilename)
		{
			return filename.ToLowerInvariant().Contains(text.ToLowerInvariant());
		}
		return text.Equals(filename, StringComparison.InvariantCultureIgnoreCase);
	}

	public abstract string Patch(string filename, string code);
}
