using System;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading
{
    // Token: 0x02000005 RID: 5
    public static class Framebuffers
    {
        // Token: 0x06000016 RID: 22 RVA: 0x00003BDC File Offset: 0x00001DDC
        public static void SetupDepthTexture(this FrameBufferRef fbRef)
        {
            GL.BindTexture(TextureTarget.Texture2D, fbRef.DepthTextureId);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, fbRef.Width, fbRef.Height,
                0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9728);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9728);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33071);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33071);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                TextureTarget.Texture2D, fbRef.DepthTextureId, 0);
        }

        // Token: 0x06000017 RID: 23 RVA: 0x00003C90 File Offset: 0x00001E90
        public static void SetupVertexTexture(this FrameBufferRef fbRef, int textureId)
        {
            GL.BindTexture(TextureTarget.Texture2D, fbRef.ColorTextureIds[textureId]);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, fbRef.Width, fbRef.Height, 0,
                PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor,
                new float[] { 1f, 1f, 1f, 1f });
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33069);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33069);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + textureId,
                TextureTarget.Texture2D, fbRef.ColorTextureIds[textureId], 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9728);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9728);
        }

        // Token: 0x06000018 RID: 24 RVA: 0x00003DA0 File Offset: 0x00001FA0
        public static void SetupColorTexture(this FrameBufferRef fbRef, int textureId)
        {
            GL.BindTexture(TextureTarget.Texture2D, fbRef.ColorTextureIds[textureId]);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, fbRef.Width, fbRef.Height, 0,
                PixelFormat.Rgba, PixelType.UnsignedShort, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9728);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9728);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + textureId,
                TextureTarget.Texture2D, fbRef.ColorTextureIds[textureId], 0);
        }

        // Token: 0x06000019 RID: 25 RVA: 0x00003E34 File Offset: 0x00002034
        public static void SetupSingleColorTexture(this FrameBufferRef fbRef, int textureId)
        {
            GL.BindTexture(TextureTarget.Texture2D, fbRef.ColorTextureIds[textureId]);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, fbRef.Width, fbRef.Height, 0,
                PixelFormat.Red, PixelType.UnsignedShort, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9728);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9728);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + textureId,
                TextureTarget.Texture2D, fbRef.ColorTextureIds[textureId], 0);
        }

        // Token: 0x0600001A RID: 26 RVA: 0x00003EC8 File Offset: 0x000020C8
        public static void CheckStatus()
        {
            FramebufferErrorCode errorCode = GL.Ext.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (errorCode != FramebufferErrorCode.FramebufferComplete)
            {
                throw new Exception("Could not create framebuffer: " + errorCode.ToString());
            }
        }

        // Token: 0x0600001B RID: 27 RVA: 0x00002110 File Offset: 0x00000310
        public static void Blit(this ClientPlatformWindows platform, MeshRef quad, int source)
        {
            ShaderProgramBlit blit = ShaderPrograms.Blit;
            blit.Use();
            blit.Scene2D = source;
            platform.RenderFullscreenTriangle(quad);
            blit.Stop();
        }
    }
}