using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using opentk.GridRenderPass;
using opentk.Manipulators;
using opentk.PropertyGridCustom;
using opentk.Scene;
using opentk.ShadingSetup;

namespace opentk.Scene.ParticleSystem
{
	public abstract partial class ParticleSystemBase: ParticleSystem
	{
		//
		private IShadingSetup m_Shading;
		//
		private bool m_Initialized;
		//
		private Grid m_Grid;

		private OrbitManipulator m_Manip;
		//
		private BufferObject<Vector4> m_PositionBuffer;
		//
		private BufferObject<Vector4> m_DimensionBuffer;
		//
		private BufferObject<Vector4> m_ColorBuffer;
		//
		private int m_PublishCounter;

		//
		public ModelViewProjectionParameters CameraMvp
		{
			get; private set;
		}

		public BufferObject<Vector4> PositionBuffer
		{
			get{ return m_PositionBuffer;}
		}
		//
		public BufferObject<Vector4> DimensionBuffer
		{
			get{ return m_DimensionBuffer;}
		}
		//
		public BufferObject<Vector4> ColorBuffer
		{
			get{ return m_ColorBuffer;}
		}
		//
		public ArrayObject ParticleStateArrayObject
		{
			get; private set;
		}
		//
		public UniformState Uniforms
		{
			get; private set;
		}
		//
		public MatrixStack TransformationStack
		{
			get; private set;
		}
		//
		public MatrixStack ProjectionStack
		{
			get; private set;
		}

		[Browsable(false)]
		public Vector4[] Position
		{
			get; private set;
		}

		[Browsable(false)]
		public Vector4[] Dimension
		{
			get; private set;
		}

		[Browsable(false)]
		public Vector4[] Color
		{
			get; private set;
		}

		public int PublishSize
		{
			get; private set;
		}

		public Vector2 Viewport
		{
			get; private set;
		}

		public PublishMethod PublishMethod
		{
			get; set;
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

		public float ParticleScaleFactor
		{
			get;
			set;
		}

		public ProjectionType ProjectionType
		{
			get;
			set;
		}

		[Category("Map properties")]
		[TypeConverter(typeof(RenderSetupConverter))]
		[DescriptionAttribute("Expand to see the parameters of the shading.")]
		public IShadingSetup Shading
		{
			get { return m_Shading;}
			set { DoPropertyChange(ref m_Shading, value, "Shading"); }
		}
	}
}

