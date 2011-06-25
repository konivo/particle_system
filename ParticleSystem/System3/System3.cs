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
		public enum ColorSchemeType
		{
			Distance,
			Color
		}

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

		[Browsable(true)]
		public Vector3d PokusnyVector
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

		public bool MapMode
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

		public ColorSchemeType ColorScheme
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

		public float SmoothShapeSharpness
		{
			get;
			set;
		}

		[Category("Aoc properties")]
		[TypeConverter(typeof(AocParametersConverter))]
		[DescriptionAttribute("Expand to see the parameters of the ssao.")]
		public AocParameters AocParameters
		{
			get;
			private set;
		}

		[Category("Map properties")]
		[TypeConverter(typeof(ChaoticMapConverter))]
		[DescriptionAttribute("Expand to see the parameters of the map.")]
		public ChaoticMap ChaoticMap
		{
			get { return m_ChaoticMap; }
			set { DoPropertyChange (ref m_ChaoticMap, value, "ChaoticMap"); }
		}

		#region implemented abstract members of opentk._ParticleSystem
		protected override void HandleFrame (GameWindow window)
		{
			GL.ClearColor (Color4.Black);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			PrepareState ();

			foreach(var pass in m_Passes)
				pass.Render(window);

			m_Grid.Render (window);

			window.SwapBuffers ();
		}

		protected override ParticleSystem GetInstanceInternal (GameWindow win)
		{
			var result = new System3 {
				PARTICLES_COUNT = 6000, VIEWPORT_WIDTH = 324, NEAR = 1, FAR = 10240, DT = 0.0001,
				Fov = 0.2,
				Projection = ProjectionType.Frustum,
				ParticleScaleFactor = 600, ParticleBrightness = 1, ParticleShape = System3.ParticleShapeType.SolidSpere, MapMode = false};
			return result;
		}
		
		#endregion
	}
}

