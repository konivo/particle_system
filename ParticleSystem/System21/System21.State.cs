using System;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.GridRenderPass;
using opentk.Manipulators;

namespace opentk.System21
{
	public partial class System21
	{
		//
		private ArrayObject m_ParticleRenderingState;
		//
		private UniformState m_UniformState;
		//
		private MatrixStack m_TransformationStack;
		//
		private MatrixStack m_Projection;
		//
		private BufferObject<Vector4> PositionBuffer;
		//
		private BufferObject<Vector4> DimensionBuffer;
		//
		private BufferObject<Vector4> ColorBuffer;

		private TextureBase Texture;
		private TextureBase UV_ColorIndex_None_Texture;
		private TextureBase AOC_Texture;
		private TextureBase AOC_Texture_Blurred_H;
		private TextureBase AOC_Texture_Blurred_HV;
		private TextureBase NormalDepth_Texture;
		private TextureBase Depth_Texture;

		//
		private Grid m_Grid;

		private OrbitManipulator m_Manip;

		private int m_PublishCounter;

		private int m_PublishSize;

		private RenderPass[] m_SolidModePasses;

		private RenderPass[] m_EmitModePasses;

		private RenderPass[] m_Passes;

		private Vector2 m_Viewport;

		private int m_SolidModeTextureSize = 2048;

		private AocParameters m_AocParameters;

		unsafe void PrepareState ()
		{
			if (m_Passes != null)
			{
				if(AocParameters.AocTextureSize != AOC_Texture.Width)
				{
					((DataTexture<float>) AOC_Texture).Data2D = new float[ AocParameters.AocTextureSize, AocParameters.AocTextureSize];
					((DataTexture<float>) AOC_Texture_Blurred_H).Data2D = new float[ AocParameters.AocTextureSize, AocParameters.AocTextureSize];
					((DataTexture<float>) AOC_Texture_Blurred_HV).Data2D = new float[ AocParameters.AocTextureSize, AocParameters.AocTextureSize];
				}

				if (PARTICLES_COUNT != PositionBuffer.Data.Length)
				{
					PositionBuffer.Data = new Vector4[PARTICLES_COUNT];
					DimensionBuffer.Data = new Vector4[PARTICLES_COUNT];
					ColorBuffer.Data = new Vector4[PARTICLES_COUNT];

					InitializeSystem ();
				}

				Simulate (DateTime.Now);

				//make changes visible to the graphics card
				PositionBuffer.Publish ();
				DimensionBuffer.Publish ();

				//determine type of shading
				switch (ParticleShape)
				{
					case ParticleShapeType.TextureSmoothDot:
					case ParticleShapeType.SmoothDot:
					m_Passes = m_EmitModePasses;
					break;
				default:
					m_Passes = m_SolidModePasses;
					break;
				}
				
				return;
			}

			m_PublishSize = 100000;

			AocParameters = new AocParameters
			{
				AocTextureSize = 512,
				OccConstantArea = false,
				OccMaxDist = 40,
				OccMinSampleRatio = 0.5f,
				OccPixmax = 100,
				OccPixmin = 2,
				SamplesCount = 32,
				AocStrength = 2
			};


			unsafe
			{
				PositionBuffer = new BufferObject<Vector4> (sizeof(Vector4), 0) { Name = "position_buffer", Usage = BufferUsageHint.DynamicDraw };
				DimensionBuffer = new BufferObject<Vector4> (sizeof(Vector4), 0) { Name = "dimension_buffer", Usage = BufferUsageHint.DynamicDraw };
				ColorBuffer = new BufferObject<Vector4> (sizeof(Vector4), 0) { Name = "color_buffer", Usage = BufferUsageHint.DynamicDraw };

				Texture = new DataTexture<Vector3> {
					Name = "custom_texture",
					InternalFormat = PixelInternalFormat.Rgb,
					Data2D = TestTexture(30, 30),
					Params = new TextureBase.Parameters
					{
						GenerateMipmap = true,
						MinFilter = TextureMinFilter.LinearMipmapLinear,
						MagFilter = TextureMagFilter.Linear,
					}};

				UV_ColorIndex_None_Texture =
				new DataTexture<Vector3> {
					Name = "UV_ColorIndex_None_Texture",
					InternalFormat = PixelInternalFormat.Rgba8,
					Data2D = TestTexture(m_SolidModeTextureSize, m_SolidModeTextureSize),
					Params = new TextureBase.Parameters
					{
						GenerateMipmap = false,
						MinFilter = TextureMinFilter.Nearest,
						MagFilter = TextureMagFilter.Nearest,
				}};

				AOC_Texture =
				new DataTexture<float> {
					Name = "AOC_Texture",
					InternalFormat = PixelInternalFormat.R16,
					Data2D = new float[AocParameters.AocTextureSize, AocParameters.AocTextureSize],
					Params = new TextureBase.Parameters
					{
						GenerateMipmap = true,
						MinFilter = TextureMinFilter.LinearMipmapLinear,
						MagFilter = TextureMagFilter.Nearest,
				}};

				AOC_Texture_Blurred_H =
				new DataTexture<float> {
					Name = "AOC_Texture_H",
					InternalFormat = PixelInternalFormat.R16,
					Data2D = new float[AocParameters.AocTextureSize, AocParameters.AocTextureSize],
					Params = new TextureBase.Parameters
					{
						GenerateMipmap = true,
						MinFilter = TextureMinFilter.LinearMipmapLinear,
						MagFilter = TextureMagFilter.Nearest,
				}};

				AOC_Texture_Blurred_HV =
				new DataTexture<float> {
					Name = "AOC_Texture_HV",
					InternalFormat = PixelInternalFormat.R16,
					Data2D = new float[AocParameters.AocTextureSize, AocParameters.AocTextureSize],
					Params = new TextureBase.Parameters
					{
						GenerateMipmap = true,
						MinFilter = TextureMinFilter.LinearMipmapLinear,
						MagFilter = TextureMagFilter.Linear,
				}};

				NormalDepth_Texture =
				new DataTexture<Vector4> {
					Name = "NormalDepth_Texture",
					InternalFormat = PixelInternalFormat.Rgba32f,
					//Format = PixelFormat.DepthComponent,
					Data2D = new Vector4[m_SolidModeTextureSize, m_SolidModeTextureSize],
					Params = new TextureBase.Parameters
					{
						GenerateMipmap = false,
						MinFilter = TextureMinFilter.Nearest,
						MagFilter = TextureMagFilter.Nearest,
				}};

				Depth_Texture =
				new DataTexture<float> {
					Name = "Depth_Texture",
					InternalFormat = PixelInternalFormat.DepthComponent32f,
					Format = PixelFormat.DepthComponent,
					Data2D = new float[m_SolidModeTextureSize, m_SolidModeTextureSize],
					Params = new TextureBase.Parameters
					{
						GenerateMipmap = false,
						MinFilter = TextureMinFilter.Nearest,
						MagFilter = TextureMagFilter.Nearest,
				}};
			}
			
			var ortho = Matrix4.CreateOrthographic (1, 1, (float)NEAR, (float)FAR);
			
			m_Projection = new MatrixStack ().Push (ortho);
			m_TransformationStack = new MatrixStack ().Push (m_Projection);
			
			m_UniformState = new UniformState ();
			m_UniformState.Set ("color", new Vector4 (0, 0, 1, 1));
			m_UniformState.Set ("particle_scale_factor", ValueProvider.Create (() => this.ParticleScaleFactor));
			m_UniformState.Set ("particle_shape", ValueProvider.Create (() => (int)this.ParticleShape));
			
			m_ParticleRenderingState =
				new ArrayObject (
					new VertexAttribute { AttributeName = "sprite_pos", Buffer = PositionBuffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float },
					new VertexAttribute { AttributeName = "sprite_color", Buffer = ColorBuffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float },
					new VertexAttribute { AttributeName = "sprite_dimensions", Buffer = DimensionBuffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float }
				);

			//
			var firstPassSolid = new SeparateProgramPass<System21>
			(
				 "solid1",

				 //pass code
				 (window) =>
				 {
					GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
				  GL.Enable (EnableCap.DepthTest);
					GL.DepthMask(true);
					GL.DepthFunc (DepthFunction.Less);
					GL.Disable (EnableCap.Blend);

					//TODO: viewport size actually doesn't propagate to shader, because uniform state has been already activated
					SetCamera (window);
					SetViewport(0, 0, m_SolidModeTextureSize, m_SolidModeTextureSize);
					GL.DrawArrays (BeginMode.Points, 0, PARTICLES_COUNT);
				 },

				 //pass state
				 m_ParticleRenderingState,
				 m_UniformState,
				 new FramebufferBindingSet(
				   new DrawFramebufferBinding { Attachment = FramebufferAttachment.DepthAttachment, Texture = Depth_Texture },
				   new DrawFramebufferBinding { VariableName = "Fragdata.uv_colorindex_none", Texture = UV_ColorIndex_None_Texture },
				   new DrawFramebufferBinding { VariableName = "Fragdata.normal_depth", Texture = NormalDepth_Texture }
				 ),
				 new TextureBindingSet(
				   new TextureBinding { VariableName = "custom_texture", Texture = Texture }
				 )
			);

			var aocPassSolid = RenderPassFactory.CreateAoc
			(
				 NormalDepth_Texture,
				 AOC_Texture,
				 m_TransformationStack,
				 new MatrixInversion(m_TransformationStack),
				 m_Projection,
				 new MatrixInversion(m_Projection),
				 ValueProvider.Create (() => AocParameters.SamplesCount),
				 ValueProvider.Create (() => AocParameters.OccMaxDist),
				 ValueProvider.Create (() => AocParameters.OccPixmax),
				 ValueProvider.Create (() => AocParameters.OccPixmin),
				 ValueProvider.Create (() => AocParameters.OccMinSampleRatio),
				 ValueProvider.Create (() => AocParameters.OccConstantArea),
				 ValueProvider.Create (() => AocParameters.AocStrength)
			);

			var aocBlur = RenderPassFactory.CreateBilateralFilter
			(
				 AOC_Texture, AOC_Texture_Blurred_H, AOC_Texture_Blurred_HV
			);


			//
			var thirdPassSolid = RenderPassFactory.CreateFullscreenQuad
			(
				 "solid3", "System21",
				 ValueProvider.Create(() => m_Viewport),
				 (window) =>
				 {
					SetCamera (window);
				 },
				 (window) =>
				 {
					GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
					GL.Enable (EnableCap.DepthTest);
					GL.DepthMask(true);
					GL.DepthFunc (DepthFunction.Less);
					GL.Disable (EnableCap.Blend);
				 },
				//pass state
				 FramebufferBindingSet.Default,
				 m_ParticleRenderingState,
				 m_UniformState,
				 new TextureBindingSet(
				   new TextureBinding { VariableName = "custom_texture", Texture = Texture },
				   new TextureBinding { VariableName = "normaldepth_texture", Texture = NormalDepth_Texture },
				   new TextureBinding { VariableName = "uv_colorindex_texture", Texture = UV_ColorIndex_None_Texture },
				   new TextureBinding { VariableName = "aoc_texture", Texture = AOC_Texture_Blurred_HV }
				 )
			);

			//
			var firstPassEmit = new SeparateProgramPass<System21>
			(
				 "light",

				 //pass code
				 (window) =>
				 {
					GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
					GL.Disable (EnableCap.DepthTest);
					GL.Enable (EnableCap.Blend);
					GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
					GL.BlendEquation (BlendEquationMode.FuncAdd);

					//TODO: viewport size actually doesn't propagate to shader, because uniform state has been already activated
					SetCamera (window);
					GL.DrawArrays (BeginMode.Points, 0, PARTICLES_COUNT);
				 },

				 //pass state
				 FramebufferBindingSet.Default,
				 m_ParticleRenderingState,
				 m_UniformState,
				 new TextureBindingSet(
				   new TextureBinding { VariableName = "custom_texture", Texture = Texture }
				 )
			);

			m_Passes = m_SolidModePasses = new RenderPass[]{ firstPassSolid, aocPassSolid, aocBlur, thirdPassSolid };
			m_EmitModePasses = new RenderPass[]{ firstPassEmit };

			m_Manip = new OrbitManipulator (m_Projection);
			m_Grid = new Grid (m_TransformationStack);

			m_TransformationStack.Push (m_Manip.RT);
			m_UniformState.Set ("modelview_transform", m_Manip.RT);
			m_UniformState.Set ("modelviewprojection_transform", m_TransformationStack);
			m_UniformState.Set ("projection_transform", m_Projection);
			m_UniformState.Set ("projection_inv_transform", new MatrixInversion(m_Projection));
			m_UniformState.Set ("modelview_inv_transform", new MatrixInversion(m_Manip.RT));
			m_UniformState.Set ("modelviewprojection_inv_transform", new MatrixInversion(m_TransformationStack));

			m_DebugView = new opentk.QnodeDebug.QuadTreeDebug(10000, m_TransformationStack);
			PrepareState ();
		}

		private Vector3[,] TestTexture (int w, int h)
		{
			var result = new Vector3[h, w];
			var center = new Vector2(w/2, h/2);

			for (int i = 0; i < h; i++)
			{
				for (int j = 0; j < w; j++)
				{
					var position = new Vector2(j, i) - center;
					position = Vector2.Divide(position, center);
					var len = position.Length;

					if(len > 1)
						result[i,j] = new Vector3(0, 0, 0);
					else result[i, j] = new Vector3(len, 0, 0);
				}
			}

			return result;
		}

		private void SetViewport(int x, int y, int width, int height)
		{
			m_Viewport = new Vector2(width, height);
			GL.Viewport (x, y, width, height);
		}

		private void SetCamera (GameWindow window)
		{
			float aspect = window.Height / (float)window.Width;
			float projw = VIEWPORT_WIDTH;

			SetViewport(0, 0, window.Width, window.Height);
			
			if (m_Projection != null)
			{
				switch (Projection)
				{
				case ProjectionType.Frustum:
					m_Projection.ValueStack[0] = Matrix4.CreatePerspectiveFieldOfView ((float)Fov, 1 / aspect, (float)NEAR, (float)FAR);
					break;
				case ProjectionType.Ortho:
					m_Projection.ValueStack[0] = Matrix4.CreateOrthographic (projw, projw * aspect, (float)NEAR, (float)FAR);
					;
					break;
				default:
					break;
				}
			}
		}
	}
}

