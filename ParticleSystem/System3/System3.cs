using System;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel;
using System.ComponentModel.Composition;
using OpenTK.Graphics.OpenGL;
using opentk.PropertyGridCustom;

namespace opentk.System3
{
	public partial class System3 : ParticleSystem
	{
		[Browsable(true)]
		public int PARTICLES_COUNT
		{
			get;
			set;
		}

		[Browsable(true)]
		public int VIEWPORT_WIDTH
		{
			get;
			set;
		}

		[Browsable(true)]
		public int NEAR
		{
			get;
			set;
		}


		[Editor(typeof(ContrastEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public int FAR
		{
			get;
			set;
		}

		[Browsable(true)]
		public double DT
		{
			get;
			set;
		}

		[Browsable(true)]
		public Vector3d PokusnyVector
		{
			get;
			set;
		}

		#region implemented abstract members of opentk._ParticleSystem
		protected override void HandleFrame (GameWindow window)
		{
			GL.ClearColor (Color4.Black);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			//GL.Clear (ClearBufferMask.DepthBufferBit);
			GL.Enable (EnableCap.Blend);
			GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
			GL.BlendEquation (BlendEquationMode.FuncAdd);
			
			SetCamera (window);
			PrepareState ();
			GL.DrawArrays (BeginMode.Points, 0, PARTICLES_COUNT);

			m_Grid.Render();
			
			window.SwapBuffers ();
		}

		protected override ParticleSystem GetInstanceInternal (GameWindow win)
		{
			var result = new System3 { PARTICLES_COUNT = 100000, VIEWPORT_WIDTH = 124, NEAR = 0, FAR = 10240, DT = 1 };
			return result;
		}
		
		#endregion
	}
}

