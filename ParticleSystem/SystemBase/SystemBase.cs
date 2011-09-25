using System;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel;
using System.ComponentModel.Composition;
using OpenTK.Graphics.OpenGL;
using opentk.PropertyGridCustom;
using opentk.Scene;

namespace opentk.SystemBase
{
	public abstract partial class SystemBase: ParticleSystem
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
		public double Fov
		{
			get;
			set;
		}

		[Browsable(true)]
		public double NEAR
		{
			get;
			set;
		}

		[Editor(typeof(ContrastEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public double FAR
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

		public float ParticleScaleFactor
		{
			get;
			set;
		}

		public float ParticleBrightness
		{
			get;
			set;
		}

		public ProjectionType Projection
		{
			get;
			set;
		}

		#region implemented abstract members of opentk._ParticleSystem
		protected override void HandleFrame (GameWindow window)
		{
			GL.ClearColor (Color4.Black);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			PrepareState ();

			foreach(var pass in m_Passes)
				pass.Render(window);

			window.SwapBuffers ();
		}
		
		#endregion
	}
}

