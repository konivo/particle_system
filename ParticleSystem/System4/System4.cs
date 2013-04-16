using System;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel;
using System.ComponentModel.Composition;
using OpenTK.Graphics.OpenGL;
using opentk.PropertyGridCustom;
using opentk.Scene.ParticleSystem;

namespace opentk.System4
{
	public partial class System4 : ParticleSystem
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

		public float ParticleScaleFactor
		{
			get;
			set;
		}

		public ColorSchemeType ColorScheme
		{
			get;
			set;
		}

		public ProjectionType Projection
		{
			get;
			set;
		}
		
		public float RayMarchStepFactor
		{
			get;
			set;
		}
		
		[Editor(typeof(ContrastEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[RefreshProperties(RefreshProperties.All)]
		public float K1
		{
			get;
			set;
		}
		[Editor(typeof(ContrastEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public float K2
		{
			get;
			set;
		}
		[Editor(typeof(ContrastEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public float K3
		{
			get;
			set;
		}
		[Editor(typeof(ContrastEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public float K4
		{
			get;
			set;
		}
		private DateTime m_Start = DateTime.UtcNow;
		
		[Browsable(false)]
		public float Time
		{
			get{ return (float)(DateTime.UtcNow - m_Start).TotalSeconds;}
		}		

		[Category("Aoc properties")]
		[TypeConverter(typeof(ParametersConverter<AocParameters>))]
		[DescriptionAttribute("Expand to see the parameters of the ssao.")]
		public AocParameters AocParameters
		{
			get;
			private set;
		}

		#region implemented abstract members of opentk._ParticleSystem
		protected override void HandleFrame (GameWindow window)
		{
			GL.ClearColor (Color4.Black);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			PrepareState ();

			foreach(var pass in m_Passes)
				pass.Render(window);

			//m_Grid.Render (window);

			window.SwapBuffers ();
		}

		protected override ParticleSystem GetInstanceInternal (GameWindow win)
		{
			var result = new System4 {
				Projection = System4.ProjectionType.Frustum,
				VIEWPORT_WIDTH = 324, NEAR = 1, FAR = 10240,
				RayMarchStepFactor = 0.14f,
				K1 = 1, K2 = 1, K3 = 1, K4 = 1,
				Fov = 0.6,
				AocParameters = new AocParameters()};
			return result;
		}
		
		#endregion
	}
}

