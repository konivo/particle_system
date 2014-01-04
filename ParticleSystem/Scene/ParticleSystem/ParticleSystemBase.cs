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
		private BufferObject<Matrix4> m_RotationLocalBuffer;
		//
		private BufferObject<Matrix4> m_RotationBuffer;
		//
		private BufferObject<Vector4> m_ColorBuffer;
		//
		private int m_PublishCounter;
		[Browsable(false)]
		public ModelViewProjectionParameters CameraMvp
		{
			get; private set;
		}
		[Browsable(false)]
		public BufferObject<Vector4> PositionBuffer
		{
			get{ return m_PositionBuffer;}
		}
		[Browsable(false)]
		public BufferObject<Vector4> DimensionBuffer
		{
			get{ return m_DimensionBuffer;}
		}
		[Browsable(false)]
		public BufferObject<Vector4> ColorBuffer
		{
			get{ return m_ColorBuffer;}
		}
		[Browsable(false)]
		public BufferObject<Matrix4> RotationBuffer
		{
			get{ return m_RotationBuffer;}
		}
		[Browsable(false)]
		public BufferObject<Matrix4> RotationLocalBuffer
		{
			get{ return m_RotationLocalBuffer;}
		}
		[Browsable(false)]
		public ArrayObject ParticleStateArrayObject
		{
			get; private set;
		}
		[Browsable(false)]
		public UniformState Uniforms
		{
			get; private set;
		}
		[Browsable(false)]
		public MatrixStack TransformationStack
		{
			get; private set;
		}
		[Browsable(false)]
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

		[Browsable(false)]
		public Matrix4[] Rotation
		{
			get; private set;
		}

		[Browsable(false)]
		public Matrix4[] RotationLocal
		{
			get; private set;
		}

		public int PublishSize
		{
			get; private set;
		}
		[Browsable(false)]
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

		public float ModelScaleFactor
		{
			get;
			set;
		}

		public ProjectionType ProjectionType
		{
			get;
			set;
		}

		public bool ShowGrid
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

