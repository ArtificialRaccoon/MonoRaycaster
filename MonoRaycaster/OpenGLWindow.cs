using System;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Drawing.Imaging;

namespace OpenGLTest
{
	sealed class OpenGLWindow : OpenTK.GameWindow
	{
		int textureId;
		Raycaster objRaycaster = new Raycaster(320, 240);

		public OpenGLWindow() : base(640, 480, GraphicsMode.Default, "C# Raycaster", GameWindowFlags.Default, DisplayDevice.Default, 2, 0, GraphicsContextFlags.ForwardCompatible)
		{
			Console.WriteLine("OpenGL Version: " + OpenTK.Graphics.OpenGL.GL.GetString(OpenTK.Graphics.OpenGL.StringName.Version));

			textureId = GL.GenTexture();
			GL.Enable(EnableCap.Texture2D);
			VSync = VSyncMode.On;
			this.KeyDown += OpenGLWindow_KeyDown;
			this.KeyUp += OpenGLWindow_KeyUp;
		}

		private void OpenGLWindow_KeyDown(object sender, KeyboardKeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.A:
					objRaycaster.k_rot_l = true;
					break;
				case Key.D:
					objRaycaster.k_rot_r = true;
					break;
				case Key.W:
					objRaycaster.k_fw = true;
					break;
				case Key.S:
					objRaycaster.k_bk = true;
					break;
				default:
					break;
			}
		}

		private void OpenGLWindow_KeyUp(object sender, KeyboardKeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.A:
					objRaycaster.k_rot_l = false;
					break;
				case Key.D:
					objRaycaster.k_rot_r = false;
					break;
				case Key.W:
					objRaycaster.k_fw = false;
					break;
				case Key.S:
					objRaycaster.k_bk = false;
					break;
				default:
					break;
			}
		}

		protected override void OnResize(EventArgs e)
		{
			GL.Viewport(0, 0, this.Width, this.Height);
		}

		protected override void OnLoad(EventArgs e)
		{
			objRaycaster.LoadLevel("TestData", "SampleMap.xml");
		}

		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			objRaycaster.UpdateInput();
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			objRaycaster.PaintFrame();

			BitmapData bmpData = objRaycaster.buffer.LockBits(new Rectangle(0, 0, objRaycaster.buffer.Width, objRaycaster.buffer.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			GL.BindTexture(TextureTarget.Texture2D, textureId);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, objRaycaster.buffer.Width, objRaycaster.buffer.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);
			objRaycaster.buffer.UnlockBits(bmpData);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);

			GL.BindTexture(TextureTarget.Texture2D, textureId);
			GL.Begin(PrimitiveType.Triangles);

			GL.TexCoord2(0, 0); GL.Vertex2(-1, 1);
			GL.TexCoord2(1, 1); GL.Vertex2(1, -1);
			GL.TexCoord2(0, 1); GL.Vertex2(-1, -1);
			GL.TexCoord2(0, 0); GL.Vertex2(-1, 1);
			GL.TexCoord2(1, 0); GL.Vertex2(1, 1);
			GL.TexCoord2(1, 1); GL.Vertex2(1, -1);

			GL.End();

			this.SwapBuffers();
		}
	}
}
