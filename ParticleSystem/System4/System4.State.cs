using System;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.GridRenderPass;
using opentk.Manipulators;

namespace opentk.System4
{
	public partial class System4
	{
		//
		private UniformState m_UniformState;
		//
		private MatrixStack m_TransformationStack;
		//
		private MatrixStack m_Projection;
		//
		private TextureBase AOC_Texture;
		//
		private TextureBase NormalDepth_Texture;
		//
		private TextureBase Depth_Texture;
		//
		private Grid m_Grid;
		//
		private OrbitManipulator m_Manip;
		//
		private RenderPass[] m_Passes;
		//
		private Vector2 m_Viewport;
		//
		private int m_SolidModeTextureSize = 1024;
		//
		private int m_AocTextureSize = 512;
		//
		private int m_AocSampleCount = 64;

		private void PrepareState ()
		{
			if (m_Passes != null)
			{
				return;
			}
			
			unsafe
			{
				AOC_Texture =
				new DataTexture<float> {
					Name = "AOC_Texture",
					InternalFormat = PixelInternalFormat.R8,
					Data2D = new float[m_AocTextureSize, m_AocTextureSize],
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

			//
			var firstPassSolid = RenderPassFactory.CreateFullscreenQuad
			(
				 "raymarch", "System4",
				 ValueProvider.Create(() => new Vector2(m_SolidModeTextureSize, m_SolidModeTextureSize)),
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
				 new FramebufferBindingSet(
				   new DrawFramebufferBinding { Attachment = FramebufferAttachment.DepthAttachment, Texture = Depth_Texture },
				   new DrawFramebufferBinding { VariableName = "Fragdata.normal_depth", Texture = NormalDepth_Texture }
				 ),
				 m_UniformState
			);

			var AocParameters = new AocParameters
			{
				TextureSize = 512,
				OccConstantArea = false,
				OccMaxDist = 40,
				OccMinSampleRatio = 0.5f,
				OccPixmax = 100,
				OccPixmin = 2,
				SamplesCount = 32,
				Strength = 2.3f
			};

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
				 ValueProvider.Create (() => AocParameters.Strength)
			);

			//
			var thirdPassSolid = RenderPassFactory.CreateFullscreenQuad
			(
				 "lighting", "System4",
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
				 m_UniformState,
				 new TextureBindingSet(
				   new TextureBinding { VariableName = "normaldepth_texture", Texture = NormalDepth_Texture },
				   new TextureBinding { VariableName = "aoc_texture", Texture = AOC_Texture }
				 )
			);

			m_Passes = new RenderPass[]{ firstPassSolid, aocPassSolid, thirdPassSolid };

			m_Manip = new OrbitManipulator (m_Projection);
			m_Grid = new Grid (m_TransformationStack);
			
			m_TransformationStack.Push (m_Manip.RT);
			m_UniformState.Set ("modelview_transform", m_Manip.RT);
			m_UniformState.Set ("modelviewprojection_transform", m_TransformationStack);
			m_UniformState.Set ("projection_transform", m_Projection);
			m_UniformState.Set ("projection_inv_transform", new MatrixInversion(m_Projection));
			m_UniformState.Set ("modelview_inv_transform", new MatrixInversion(m_Manip.RT));
			m_UniformState.Set ("modelviewprojection_inv_transform", new MatrixInversion(m_TransformationStack));

			PrepareState ();
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
			
			if (m_Manip != null)
			{
				m_Manip.HandleInput (window);
			}
		}

		public override void Dispose ()
		{

		}
	}
}

