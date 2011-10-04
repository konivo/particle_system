using System;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel.Composition;
using OpenTK.Graphics.OpenGL;
using opentk.Scene.ParticleSystem;

namespace opentk.System1
{
	public partial class System1: ParticleSystem
	{
		private const int PARTICLES_COUNT = 600;

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
			GL.DrawArraysInstanced (BeginMode.TriangleFan, 0, 4, PARTICLES_COUNT);
			
			window.SwapBuffers ();
		}

		protected override ParticleSystem GetInstanceInternal (GameWindow win)
		{
			var result = new System1 ();
			return result;
		}

		#endregion
	}
}

