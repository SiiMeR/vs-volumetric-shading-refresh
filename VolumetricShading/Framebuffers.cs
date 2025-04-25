using System;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShading;

public static class Framebuffers
{
	public static void SetupDepthTexture(this FrameBufferRef fbRef)
	{
		GL.BindTexture((TextureTarget)3553, fbRef.DepthTextureId);
		GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)33191, fbRef.Width, fbRef.Height, 0, (PixelFormat)6402, (PixelType)5126, (IntPtr)IntPtr.Zero);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9728);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9728);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10242, 33071);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10243, 33071);
		GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36096, (TextureTarget)3553, fbRef.DepthTextureId, 0);
	}

	public static void SetupVertexTexture(this FrameBufferRef fbRef, int textureId)
	{
		GL.BindTexture((TextureTarget)3553, fbRef.ColorTextureIds[textureId]);
		GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)34842, fbRef.Width, fbRef.Height, 0, (PixelFormat)6408, (PixelType)5126, (IntPtr)IntPtr.Zero);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9729);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9729);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)4100, new float[4] { 1f, 1f, 1f, 1f });
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10242, 33069);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10243, 33069);
		GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)(36064 + textureId), (TextureTarget)3553, fbRef.ColorTextureIds[textureId], 0);
		GL.BindTexture((TextureTarget)3553, 0);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9728);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9728);
	}

	public static void SetupColorTexture(this FrameBufferRef fbRef, int textureId)
	{
		GL.BindTexture((TextureTarget)3553, fbRef.ColorTextureIds[textureId]);
		GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)32856, fbRef.Width, fbRef.Height, 0, (PixelFormat)6408, (PixelType)5123, (IntPtr)IntPtr.Zero);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9728);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9728);
		GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)(36064 + textureId), (TextureTarget)3553, fbRef.ColorTextureIds[textureId], 0);
	}

	public static void SetupSingleColorTexture(this FrameBufferRef fbRef, int textureId)
	{
		GL.BindTexture((TextureTarget)3553, fbRef.ColorTextureIds[textureId]);
		GL.TexImage2D((TextureTarget)3553, 0, (PixelInternalFormat)33321, fbRef.Width, fbRef.Height, 0, (PixelFormat)6403, (PixelType)5123, (IntPtr)IntPtr.Zero);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10241, 9728);
		GL.TexParameter((TextureTarget)3553, (TextureParameterName)10240, 9728);
		GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)(36064 + textureId), (TextureTarget)3553, fbRef.ColorTextureIds[textureId], 0);
	}

	public unsafe static void CheckStatus()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		FramebufferErrorCode val = Ext.CheckFramebufferStatus((FramebufferTarget)36160);
		if ((int)val != 36053)
		{
			throw new Exception("Could not create framebuffer: " + ((object)(*(FramebufferErrorCode*)(&val))/*cast due to .constrained prefix*/).ToString());
		}
	}

	public static void Blit(this ClientPlatformWindows platform, MeshRef quad, int source)
	{
		ShaderProgramBlit blit = ShaderPrograms.Blit;
		((ShaderProgramBase)blit).Use();
		blit.Scene2D = source;
		platform.RenderFullscreenTriangle(quad);
		((ShaderProgramBase)blit).Stop();
	}
}
