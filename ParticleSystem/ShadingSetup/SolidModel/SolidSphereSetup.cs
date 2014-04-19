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
using OpenTK.Extensions;

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
				 ValueProvider.Create ( () => "FragDepth" + SunLightImpl.ShadowmapType.ToString ()),
				 SunLightImpl.LightMvp
			);
			/*
			var mvpUniforms = new UniformState
			{
				{"viewport_size", ValueProvider.Create(() => new Vector2(SolidModeTextureSize, SolidModeTextureSize))},
				{"K", ValueProvider.Create(() => new Vector4(0.0f, 0.0f, 0.0f, 20 * NormalBlurAvoidance))},
				{p.CameraMvp}
			};

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
*/
			/*var aocPassSolid = RenderPassFactory.CreateAoc
			(
				 NormalDepth_Texture,
				 AOC_Texture,
				 p.CameraMvp.ModelViewProjection,
				 p.CameraMvp.ModelViewProjectionInv,
				 p.CameraMvp.Projection,
				 p.CameraMvp.ProjectionInv,
				 AocParameters
			);*/
			SsaoEffect.SourceNormalDepth = NormalDepth_Texture;
			SsaoEffect.Target = AOC_Texture;
			SsaoEffect.SourceMvp = p.CameraMvp;
			var aocPassSolid = RenderPassFactory.CreatePass(SsaoEffect);

			/*var aocBlur = RenderPassFactory.CreateBilateralFilter
			(
				 AOC_Texture, AOC_Texture_Blurred_H, AOC_Texture_Blurred_HV,
				 ValueProvider.Create(() => 20 * new Vector4(AocParameters.BlurEdgeAvoidance, AocParameters.BlurEdgeAvoidance, AocParameters.BlurEdgeAvoidance, 0))
			);*/
			var aocBlur = RenderPassFactory.CreatePass(new BlurFilter { Source = AOC_Texture, Target = AOC_Texture_Blurred_HV, Width = 0 });
			
			string scode =
				@"
{0}version 440
{0}define T_LAYOUT_OUT_DEPTH {1}
{0}define T_LAYOUT_OUT_COLORLUMA {2}

layout(local_size_x = {3}, local_size_y = {4}) in;
{0}line 1
";
			int workgroupSize = 8;
			scode = string.Format (scode, "#", ImageFormat.R32f, BeforeAA_Texture.InternalFormat, workgroupSize, workgroupSize);
			var namemodifier = string.Format ("wgsize:{0}x{0},fi:{1},fi:{2}", workgroupSize, ImageFormat.R32f, BeforeAA_Texture.InternalFormat);
			var deferredLigthing = new SeparateProgramPass
			(
				"shadingsetup.solidsphere.deferredligthing",
				window => { GLExtensions.DispatchCompute ((int)Math.Ceiling((float)Depth_Texture.Width/workgroupSize), (int)Math.Ceiling((float)Depth_Texture.Height/workgroupSize), 1); },
				new Program ("shadingsetup.solidsphere.deferredligthing")
				{
				  RenderPass.GetShaders ("shadingsetup", "solid3", "compute").PrependText(namemodifier, scode),
			  },
				new ImageBindingSet
				{
					{"u_TargetDepth", () => Depth_Texture },
					{"u_TargetColorLuma", () => BeforeAA_Texture },
				},
				
				p.ParticleStateArrayObject,
				m_Uniforms,
				new TextureBindingSet
				{
					{ "u_ColorRampTexture", ValueProvider.Create(() => (ColorRamp ?? ColorRamps.RedBlue).Texture)},
					{ "u_NormalDepthTexture", NormalDepth_Texture },
					{ "u_ParticleAttribute1Texture", m_ParticleAttribute1_Texture },
					{ "u_ShadowTexture", Shadow_Texture },
					{ "u_SsaoTexture", AOC_Texture_Blurred_HV }
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
			 firstPassSolid, firstPassShadow, /*normalDepthBlur,*/ aocPassSolid, aocBlur, deferredLigthing, antialiasPass, finalRender
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

