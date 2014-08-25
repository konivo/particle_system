using System;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.GridRenderPass;
using opentk.Manipulators;
using opentk.ShadingSetup;

namespace opentk.System6
{
	public partial class System6
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
		private TextureBase BeforeAATexture;
		//
		private TextureBase AccumTexture;
		//
		private TextureBase AATexture;
		//
		private TextureBase Depth_Texture;
		//
		private OrbitManipulator m_Manip;
		//
		private RenderPass[] m_Passes;
		//
		private Vector2 m_Viewport;
		//
		private int m_SolidModeTextureSize = 256;
		//
		private int m_AocTextureSize = 512;

		private void PrepareState ()
		{
			if (m_Passes != null)
			{
				return;
			}
			
			unsafe
			{
				BeforeAATexture =
				new DataTexture<Vector4> {
					Name = "BeforeAATexture",
					InternalFormat = PixelInternalFormat.Rgba32f,
					//Format = PixelFormat.DepthComponent,
					Data2D = new Vector4[m_SolidModeTextureSize, m_SolidModeTextureSize],
					Params = new TextureBase.Parameters
					{
						GenerateMipmap = false,
						MinFilter = TextureMinFilter.Nearest,
						MagFilter = TextureMagFilter.Nearest,
				}};
				
				AccumTexture =
				new DataTexture<Vector4> {
					Name = "AccumTexture",
					InternalFormat = PixelInternalFormat.Rgba32f,
					Data2D = new Vector4[m_SolidModeTextureSize, m_SolidModeTextureSize],
					Params = new TextureBase.Parameters
					{
						GenerateMipmap = false,
						MinFilter = TextureMinFilter.Nearest,
						MagFilter = TextureMagFilter.Nearest,
					}};
				
				AATexture = new Texture
				{
					Name = "AATexture",
					InternalFormat = PixelInternalFormat.Rgba8,
					Target = TextureTarget.Texture2D,
					Width = m_SolidModeTextureSize,
					Height = m_SolidModeTextureSize,
					Params = new TextureBase.Parameters
					{
						//GenerateMipmap = true,
						MinFilter = TextureMinFilter.Nearest,
						MagFilter = TextureMagFilter.Nearest,
					}
				};

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
			
			var frameCount = 1;
			m_UniformState = new UniformState
			{
				{"pRayMarchStepFactor", () => this.RayMarchStepFactor},
				{"k1", () => this.K1},
				{"k2", () => this.K2},
				{"k3", () => this.K3},
				{"k4", () => this.K4},
				{"time", () => this.Time},
				{"u_FrameCount", () => frameCount},
			};
			
			var workgroupSize = 8;
			var giPass = new SeparateProgramPass(
				"system6.globalillumination",
				window => { GLExtensions.DispatchCompute ((int)Math.Ceiling((float)Depth_Texture.Width/workgroupSize), (int)Math.Ceiling((float)Depth_Texture.Height/workgroupSize), 1); },
			  new Program ("system6.gi")
				{
					RenderPass.GetShaders ("system6", "gi", "compute")//.PrependText(namemodifier, scode),
				},
				new ImageBindingSet
				{
					{"u_TargetDepth", () => Depth_Texture },
					{"u_TargetColorLuma", () => BeforeAATexture },
				  {"u_TargetAccumLuma", () => AccumTexture },
				},
			  m_UniformState);
			
			//var antialiasPass = RenderPassFactory.CreatePass( new Fxaa3Filter{ Source = BeforeAATexture, Target = AATexture});
			
			var finalRender = RenderPassFactory.CreateRenderTextureToBuffer
			(
				BeforeAATexture,
				Depth_Texture,
				ValueProvider.Create(() => m_Viewport),
				(window) =>
				{
					SetCamera (window);
				},
				(window) =>
				{
					GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
					GL.Disable (EnableCap.DepthTest);
					GL.Disable (EnableCap.Blend);
					frameCount++;
				},
				FramebufferBindingSet.Default);

			m_Passes = new RenderPass[]{ giPass, /*antialiasPass, */finalRender };
			m_Manip = new OrbitManipulator (m_Projection);
			//m_Grid = new Grid (m_TransformationStack);
						
			m_TransformationStack.Push (m_Manip.RT);
			m_UniformState.Set ("modelview_transform", m_Manip.RT);
			m_UniformState.Set ("modelviewprojection_transform", m_TransformationStack);
			m_UniformState.Set ("projection_transform", m_Projection);
			m_UniformState.Set ("projection_inv_transform", new MatrixInversion(m_Projection));
			m_UniformState.Set ("modelview_inv_transform", new MatrixInversion(m_Manip.RT));
			m_UniformState.Set ("modelviewprojection_inv_transform", new MatrixInversion(m_TransformationStack));
			
			m_Manip.RT.PropertyChanged +=
			(s, args) => { frameCount = 0; };

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

