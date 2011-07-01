using System;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel;
using System.ComponentModel.Composition;
using OpenTK.Graphics.OpenGL;

namespace opentk.System21
{
	public partial class System21: ParticleSystem
	{
		public enum ProjectionType
		{
			Ortho,
			Frustum
		}

		public enum ParticleShapeType
		{
			SmoothDot = 0x1,
			SolidSpere = 0x2,
			TextureSmoothDot = 0x3
		}

		[Flags]
		public enum ParticleAttributes
		{
			None = 0x0,
			Normal = 0x1,
			Tangent = 0x2
		}

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

		public int TrailSize
		{
			get;
			set;
		}

		public int StepsPerFrame
		{
			get;
			set;
		}

		public float ParticleScaleFactor
		{
			get;
			set;
		}

		public ParticleShapeType ParticleShape
		{
			get;
			set;
		}

		public ProjectionType Projection
		{
			get;
			set;
		}

		[Category("Aoc properties")]
		[TypeConverter(typeof(AocParametersConverter))]
		[DescriptionAttribute("Expand to see the parameters of the ssao.")]
		public AocParameters AocParameters
		{
			get { return m_AocParameters; }
			set { DoPropertyChange (ref m_AocParameters, value, "AocParameters"); }
		}

		#region implemented abstract members of opentk._ParticleSystem
		protected override void HandleFrame (GameWindow window)
		{
			GL.ClearColor (Color4.Black);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			//
			PrepareState ();

			//
			if (m_Manip != null)
			{
				m_Manip.HandleInput (window);
			}

			foreach(var pass in m_Passes)
				pass.Render(window);

			m_Grid.Render (window);
			m_DebugView.Render(window);

			window.SwapBuffers ();
		}

		protected override ParticleSystem GetInstanceInternal (GameWindow win)
		{
			var result = new System21 { PARTICLES_COUNT = 2000, VIEWPORT_WIDTH = 20, Fov = 0.5, NEAR = 1, FAR = 1000 };
			return result;
		}

		#endregion
	}
}

