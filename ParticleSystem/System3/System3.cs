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
			Bubble = 0x3
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

			if (ParticleShape == System3.ParticleShapeType.SolidSpere)
			{
				GL.Enable (EnableCap.DepthTest);
				GL.DepthMask(true);
				GL.DepthFunc (DepthFunction.Less);
				GL.Disable (EnableCap.Blend);
			}
			else
			{
				GL.Disable (EnableCap.DepthTest);
				GL.Enable (EnableCap.Blend);
				GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
				GL.BlendEquation (BlendEquationMode.FuncAdd);
			}

			SetCamera (window);
			PrepareState ();
			GL.DrawArrays (BeginMode.Points, 0, PARTICLES_COUNT);
			m_Grid.Render ();
			window.SwapBuffers ();
		}

		protected override ParticleSystem GetInstanceInternal (GameWindow win)
		{
			var result = new System3 {
				PARTICLES_COUNT = 300000, VIEWPORT_WIDTH = 124, NEAR = 1, FAR = 10240, DT = 0.01,
				Fov = 0.6,
				ParticleScaleFactor = 30, ParticleBrightness = 1, ParticleShape = System3.ParticleShapeType.SolidSpere, MapMode = false };
			return result;
		}
		
		#endregion
	}
}

