using System;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel;
using System.ComponentModel.Composition;
using OpenTK.Graphics.OpenGL;
using opentk.PropertyGridCustom;
using opentk.Scene;
using opentk.Scene.ParticleSystem;
using opentk.System3;

namespace opentk.ShadingSetup
{
	/// <summary>
	///
	/// </summary>
	public class SolidSphereSetup: SolidSetupBase
	{
		private TextureBase NormalDepth_Texture_H;
		private TextureBase NormalDepth_Texture_Unfiltered;
		public float NormalBlurAvoidance
		{
			get; set;
		}

		protected override void TextureSetup()
		{
			base.TextureSetup();

			NormalDepth_Texture_Unfiltered =
				new DataTexture<Vector4> {
					Name = "NormalDepth_Texture_Unfiltered",
					InternalFormat = PixelInternalFormat.Rgba32f,
					//Format = PixelFormat.DepthComponent,
					Data2D = new Vector4[SolidModeTextureSize, SolidModeTextureSize],
					Params = new TextureBase.Parameters
					{
						GenerateMipmap = false,
						MinFilter = TextureMinFilter.Nearest,
						MagFilter = TextureMagFilter.Nearest,
				}};

			NormalDepth_Texture_H =
				new DataTexture<Vector4> {
					Name = "NormalDepth_Texture_H",
					InternalFormat = PixelInternalFormat.Rgba32f,
					//Format = PixelFormat.DepthComponent,
					Data2D = new Vector4[SolidModeTextureSize, SolidModeTextureSize],
					Params = new TextureBase.Parameters
					{
						GenerateMipmap = false,
						MinFilter = TextureMinFilter.Nearest,
						MagFilter = TextureMagFilter.Nearest,
				}};
		}

		protected override void UpdateTextureResolutions()
		{
			if(AA_Texture != null &&
			   AA_Texture.Width != SolidModeTextureSize)
			{
				((DataTexture<Vector4>)NormalDepth_Texture_Unfiltered).Data2D = new Vector4[SolidModeTextureSize, SolidModeTextureSize];
			}
			base.UpdateTextureResolutions();
		}

		protected override void ParameterSetup()
		{
			base.ParameterSetup();
			NormalBlurAvoidance = 0.2f;
		}

		protected override void PassSetup (ParticleSystemBase p)
		{
			base.PassSetup(p);

			var mode = ValueProvider.Create
			(() =>
			{
				switch (SunLightImpl.ImplementationType) {
				case LightImplementationType.ExponentialShadowMap:
					return 2;
				case LightImplementationType.ShadowMap:
					return 1;
				default:
				break;
				}
				return 0;
			});

			//
			var particle_scale_factor = ValueProvider.Create (() => p.ParticleScaleFactor);
			var particle_count = ValueProvider.Create (() => p.PARTICLES_COUNT);

			var firstPassSolid =  RenderPassFactory.CreateSolidSphere
			(
				 NormalDepth_Texture,
				 m_ParticleAttribute1_Texture,
				 Depth_Texture,
				 p.PositionBuffer,
				 p.ColorBuffer,
				 p.DimensionBuffer,
				 particle_count,
				 particle_scale_factor,
				 p.CameraMvp
			);

			var firstPassShadow =  RenderPassFactory.CreateSolidSphere
			(
				 Shadow_Texture,
				 p.PositionBuffer,
				 p.ColorBuffer,
				 p.DimensionBuffer,
				 particle_count,
				 particle_scale_factor,
				 mode,
				 SunLightImpl.LightMvp
			);

			var mvpUniforms = new UniformState();
			p.CameraMvp.SetUniforms("", mvpUniforms);
			mvpUniforms.Set ("viewport_size", ValueProvider.Create(() => new Vector2(SolidModeTextureSize, SolidModeTextureSize)));
			mvpUniforms.Set ("K", ValueProvider.Create(() => new Vector4(0.0f, 0.0f, 0.0f, 20 * NormalBlurAvoidance)));

			var normalDepthBlur =  RenderPassFactory.CreateFullscreenQuad
			(
				 "stringfilter", "SolidModel",
				 ValueProvider.Create(() => new Vector2(SolidModeTextureSize, SolidModeTextureSize)),
				 (window) => { },
				 (window) =>
				 {

				 },
				 //pass state
				 new FramebufferBindingSet{
				   { "Fragdata.result", NormalDepth_Texture}
				 },
				 m_Uniforms,
				 mvpUniforms,
				 new TextureBindingSet{
				   { "normaldepth_texture", NormalDepth_Texture_Unfiltered },
				   { "tangent_texture", m_ParticleAttribute1_Texture }
				 }
			);

			var aocPassSolid = RenderPassFactory.CreateAoc
			(
				 NormalDepth_Texture,
				 AOC_Texture,
				 p.CameraMvp.ModelViewProjection,
				 p.CameraMvp.ModelViewProjectionInv,
				 p.CameraMvp.Projection,
				 p.CameraMvp.ProjectionInv,
				 AocParameters
			);

			var aocBlur = RenderPassFactory.CreateBilateralFilter
			(
				 AOC_Texture, AOC_Texture_Blurred_H, AOC_Texture_Blurred_HV,
				 ValueProvider.Create(() => 20 * new Vector4(AocParameters.BlurEdgeAvoidance, AocParameters.BlurEdgeAvoidance, AocParameters.BlurEdgeAvoidance, 0))
			);

			//
			var thirdPassSolid = RenderPassFactory.CreateFullscreenQuad
			(
				 "solid3", "SolidModel",
				 ValueProvider.Create(() => new Vector2(SolidModeTextureSize, SolidModeTextureSize)),
				 (window) => { },
				 (window) =>
				 {
					GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
					GL.Disable (EnableCap.DepthTest);
					GL.Disable (EnableCap.Blend);
				 },
				 //pass state
				 new FramebufferBindingSet{
				   { FramebufferAttachment.DepthAttachment, Depth_Texture },
				   { "color_luma", BeforeAA_Texture}
				 },
				 p.ParticleStateArrayObject,
				 m_Uniforms,
				 new TextureBindingSet{
					 { "colorramp_texture", ValueProvider.Create(() => (ColorRamp ?? ColorRamps.RedBlue).Texture)},
				   { "normaldepth_texture", NormalDepth_Texture },
				   { "particle_attribute1_texture", m_ParticleAttribute1_Texture },
				   { "shadow_texture", Shadow_Texture },
				   { "aoc_texture", AOC_Texture_Blurred_HV }
				 }
			);

			var antialiasPass = RenderPassFactory.CreateFxaa3Filter
			(
				 BeforeAA_Texture, AA_Texture
			);

			var finalRender = RenderPassFactory.CreateRenderTextureToBuffer
			(
				 AA_Texture,
				 Depth_Texture,
				 ValueProvider.Create(() => p.Viewport),
				 (window) =>
				 {
					p.SetViewport (window);
				 },
				 (window) =>
				 {
					GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
					GL.Disable (EnableCap.DepthTest);
					GL.Disable (EnableCap.Blend);
				 },
				//TODO: BUG m_ParticleRenderingState is necessary, but it shouldn't be
				FramebufferBindingSet.Default,
				p.ParticleStateArrayObject
			);

			m_Pass = new CompoundRenderPass
			(
			 firstPassSolid, firstPassShadow, /*normalDepthBlur,*/ aocPassSolid, aocBlur, thirdPassSolid, antialiasPass, finalRender
			);

		}

		public override string Name
		{
			get
			{
				return "SmoothSetup";
			}
		}
	}
}

