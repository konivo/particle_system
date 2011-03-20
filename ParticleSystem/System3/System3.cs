using System;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel.Composition;
using OpenTK.Graphics.OpenGL;

namespace opentk.System3
{
	public partial class System3: ParticleSystem
	{
		private const int PARTICLES_COUNT = 2000;

		#region implemented abstract members of opentk._ParticleSystem
		protected override void HandleFrame (GameWindow window)
		{
			GL.ClearColor (Color4.Black);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.Enable (EnableCap.Blend);
			GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
			GL.BlendEquation (BlendEquationMode.FuncAdd);
			
			SetCamera (window);
			PrepareState ();
			GL.DrawArrays (BeginMode.Points, 0, PARTICLES_COUNT);

			window.SwapBuffers ();
		}

		protected override ParticleSystem GetInstanceInternal (GameWindow win)
		{
			var result = new System3 ();
			return result;
		}

		#endregion
	}
}

