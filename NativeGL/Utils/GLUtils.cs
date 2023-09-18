using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

using OpenGL = OpenTK.Graphics.OpenGL;
using SDI = System.Drawing.Imaging;

namespace NativeGL
{
    public static class GLUtils
    {
        // Replaces gluPerspective. Sets the frustum to perspective mode.
        // fovY     - Field of vision in degrees in the y direction
        // aspect   - Aspect ratio of the viewport
        // zNear    - The near clipping distance
        // zFar     - The far clipping distance
        public static void GLUPerspective(double fovY, double aspect, double zNear, double zFar)
        {
            double fH = Math.Tan(fovY / 360 * Math.PI) * zNear;
            double fW = fH * aspect;
            GL.Frustum(-fW, fW, -fH, fH, zNear, zFar);
        }
        
        /// <summary>
        /// Creates a single framebuffer backed by an RGBA8 color + depth texture of the specified dimension.
        /// </summary>
        /// <param name="colortex">The color texture that will be generated</param>
        /// <param name="depthTex">The depth texture that will be generated</param>
        /// <param name="fbTarget">The framebuffer that will be generated</param>
        /// <param name="w">Width of thebuffer</param>
        /// <param name="h">Height of the buffer</param>
        public static void CreateFramebuffer(out int colorTex, out int depthTex, out int fbtarget, int w, int h)
        {
            // Create color texture
            GL.GenTextures(1, out colorTex);
            GL.BindTexture(TextureTarget.Texture2D, colorTex);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            // IntPtr.Zero means reserve texture memory, but texels are undefined
            GL.TexImage2D(TextureTarget.Texture2D, 0, OpenGL.PixelInternalFormat.Rgba8, w, h, 0, OpenGL.PixelFormat.Rgba, OpenGL.PixelType.Byte, IntPtr.Zero);

            // Create depth texture
            GL.GenTextures(1, out depthTex);
            GL.BindTexture(TextureTarget.Texture2D, depthTex);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge); // used to be TextureWrapMode.Repeat
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.DepthTextureMode, (int)All.Intensity);
            //gl.glTexParameteri(TextureTarget.Texture2D, GL2.GL_TEXTURE_COMPARE_MODE, GL2.GL_COMPARE_R_TO_TEXTURE);
            //gl.glTexParameteri(TextureTarget.Texture2D, GL2.GL_TEXTURE_COMPARE_FUNC, GL2.GL_LEQUAL);
            GL.TexImage2D(TextureTarget.Texture2D, 0, OpenGL.PixelInternalFormat.DepthComponent24, w, h, 0, OpenGL.PixelFormat.DepthComponent, OpenGL.PixelType.UnsignedByte, IntPtr.Zero);

            GL.GenFramebuffers(1, out fbtarget);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbtarget);
            //Attach 2D texture to this FBO
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, colorTex, 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthTex, 0);
            //Does the GPU support current FBO configuration?
            FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("Framebuffer bind error! " + status.ToString());
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
    }
}
