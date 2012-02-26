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
using System.Collections.Generic;

namespace opentk.ShadingSetup
{
	/// <summary>
	///
	/// </summary>
	public class SolidBoxSetup: IShadingSetup
	{
		private RenderPass m_Pass;
		private UniformState m_Uniforms;

		private TextureBase m_ParticleAttribute1_Texture;
		private TextureBase AOC_Texture;
		private TextureBase AOC_Texture_Blurred_H;
		private TextureBase AOC_Texture_Blurred_HV;
		private TextureBase NormalDepth_Texture;
		private TextureBase Depth_Texture;
		private TextureBase BeforeAA_Texture;
		private TextureBase AA_Texture;
		private TextureBase Shadow_Texture;
		private ColorRamp m_ColorRamp;

		private AocParameters m_AocParameters;
		private Light m_SunLight;
		private LightImplementationParameters m_SunLightImpl;

		[Category("Aoc properties")]
		[TypeConverter(typeof(AocParametersConverter))]
		[DescriptionAttribute("Expand to see the parameters of the ssao.")]
		public AocParameters AocParameters
		{
			get { return m_AocParameters; }
			set { m_AocParameters = value; }
		}

		public bool EnableSoftShadow
		{
			get; set;
		}

		public int ShadowTextureSize
		{
			get; set;
		}

		public int SolidModeTextureSize
		{
			get; set;
		}

		public float LightSize
		{
			get; set;
		}

		public float ExpMapLevel
		{
			get; set;
		}

		public float ExpMapRange
		{
			get; set;
		}

		public float ExpMapRangeK
		{
			get; set;
		}

		public int ExpMapNsamples
		{
			get; set;
		}

		[Category("ColorRamp")]
		[TypeConverter(typeof(ColorRampConverter))]
		[DescriptionAttribute("Expand to see the parameters of the map.")]
		public ColorRamp ColorRamp
		{
			get;
			set;
		}

		public MaterialType MaterialType
		{
			get;
			set;
		}

		private void TextureSetup()
		{
			//TEextures setup
			m_ParticleAttribute1_Texture =
				new DataTexture<Vector3> {
					Name = "UV_ColorIndex_None_Texture",
					InternalFormat = PixelInternalFormat.Rgba8,
					Data2D = new Vector3[SolidModeTextureSize, SolidModeTextureSize],
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
					Data2D = new float[AocParameters.TextureSize, AocParameters.TextureSize],
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
					Data2D = new float[AocParameters.TextureSize, AocParameters.TextureSize],
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
					Data2D = new float[AocParameters.TextureSize, AocParameters.TextureSize],
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
					Data2D = new Vector4[SolidModeTextureSize, SolidModeTextureSize],
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
					Data2D = new float[SolidModeTextureSize, SolidModeTextureSize],
					Params = new TextureBase.Parameters
					{
						GenerateMipmap = false,
						MinFilter = TextureMinFilter.Nearest,
						MagFilter = TextureMagFilter.Nearest,
				}};

			BeforeAA_Texture =
				new DataTexture<Vector4> {
					Name = "BeforeAA_Texture",
					InternalFormat = PixelInternalFormat.Rgba8,
					Data2D = new Vector4[SolidModeTextureSize, SolidModeTextureSize],
					Params = new TextureBase.Parameters
					{
						GenerateMipmap = false,
						MinFilter = TextureMinFilter.Linear,
						MagFilter = TextureMagFilter.Linear,
				}};

			AA_Texture =
				new DataTexture<Vector4> {
					Name = "AA_Texture",
					InternalFormat = PixelInternalFormat.Rgba8,
					Data2D = new Vector4[SolidModeTextureSize, SolidModeTextureSize],
					Params = new TextureBase.Parameters
					{
						GenerateMipmap = false,
						MinFilter = TextureMinFilter.Linear,
						MagFilter = TextureMagFilter.Linear,
				}};

			Shadow_Texture =
				new DataTexture<float> {
					Name = "Shadow_Texture",
					InternalFormat = PixelInternalFormat.DepthComponent32f,
					Format = PixelFormat.DepthComponent,
					Data2D = new float[ShadowTextureSize, ShadowTextureSize],
					Params = new TextureBase.Parameters
					{
						GenerateMipmap = true,
						MinFilter = TextureMinFilter.LinearMipmapLinear,
						MagFilter = TextureMagFilter.Linear,
				}};
		}

		private void ParameterSetup()
		{
			//
			AocParameters = new AocParameters
			{
				TextureSize = 512,
				OccConstantArea = false,
				OccMaxDist = 40,
				OccMinSampleRatio = 0.5f,
				OccPixmax = 100,
				OccPixmin = 2,
				SamplesCount = 32,
				Strength = 2,
				Bias = 0.2f,
				BlurEdgeAvoidance = 0.2f
			};

			//
			m_SunLight = new Light
			{
				Direction = new Vector3(1, 1, 1),
				Type = LightType.Directional
			};

			m_SunLightImpl = new LightImplementationParameters(m_SunLight);
			m_SunLightImpl.ImplementationType = LightImplementationType.ExponentialShadowMap;

			//
			ShadowTextureSize = 2048;
			SolidModeTextureSize = 2048;

			LightSize = 0.1f;
			ExpMapLevel = 1;
			ExpMapNsamples = 15;
			ExpMapRange = 0.055f;
			ExpMapRangeK = 0.85f;
		}

		private void UpdateTextureResolutions()
		{
			if(Shadow_Texture != null &&
			   Shadow_Texture.Width != ShadowTextureSize)
			{
				((DataTexture<float>)Shadow_Texture).Data2D = new float[ShadowTextureSize, ShadowTextureSize];
			}

			if(AA_Texture != null &&
			   AA_Texture.Width != SolidModeTextureSize)
			{
				((DataTexture<Vector3>)m_ParticleAttribute1_Texture).Data2D = new Vector3[SolidModeTextureSize, SolidModeTextureSize];
				((DataTexture<Vector4>)NormalDepth_Texture).Data2D = new Vector4[SolidModeTextureSize, SolidModeTextureSize];
				((DataTexture<float>)Depth_Texture).Data2D = new float[SolidModeTextureSize, SolidModeTextureSize];
				((DataTexture<Vector4>)AA_Texture).Data2D = new Vector4[SolidModeTextureSize, SolidModeTextureSize];
				((DataTexture<Vector4>)BeforeAA_Texture).Data2D = new Vector4[SolidModeTextureSize, SolidModeTextureSize];
			}

			if(AOC_Texture != null &&
			   AOC_Texture.Width != AocParameters.TextureSize)
			{
				((DataTexture<float>)AOC_Texture).Data2D = new float[AocParameters.TextureSize, AocParameters.TextureSize];
				((DataTexture<float>)AOC_Texture_Blurred_H).Data2D = new float[AocParameters.TextureSize, AocParameters.TextureSize];
				((DataTexture<float>)AOC_Texture_Blurred_HV).Data2D = new float[AocParameters.TextureSize, AocParameters.TextureSize];
			}
		}

		#region IRenderSetup implementation
		RenderPass IShadingSetup.GetPass (ParticleSystemBase p)
		{
			UpdateTextureResolutions();

			if(m_Pass != null)
				return m_Pass;

			ParameterSetup();
			TextureSetup();

			//
			m_Uniforms = new UniformState(p.Uniforms);
			m_Uniforms.SetMvp ("light", m_SunLightImpl.LightMvp);
			m_Uniforms.Set("shadow_implementation", ValueProvider.Create
			(() =>
			{
				switch (m_SunLightImpl.ImplementationType) {
				case LightImplementationType.ExponentialShadowMap:
					return 2;
				case LightImplementationType.ShadowMap:
					return 1;
				default:
				break;
				}
				return 0;
			}));
			m_Uniforms.Set("material_color_source", ValueProvider.Create(() => MaterialType));

			var mode = ValueProvider.Create
			(() =>
			{
				switch (m_SunLightImpl.ImplementationType) {
				case LightImplementationType.ExponentialShadowMap:
					return 2;
				case LightImplementationType.ShadowMap:
					return 1;
				default:
				break;
				}
				return 0;
			});

			m_Uniforms.Set("light_size", ValueProvider.Create(() => LightSize ));
			m_Uniforms.Set("light_expmap_level", ValueProvider.Create(() => ExpMapLevel ));
			m_Uniforms.Set("light_expmap_range", ValueProvider.Create(() => ExpMapRange ));
			m_Uniforms.Set("light_expmap_range_k", ValueProvider.Create(() => ExpMapRangeK ));
			m_Uniforms.Set("light_expmap_nsamples", ValueProvider.Create(() => ExpMapNsamples ));

			//
			m_Uniforms.Set ("sampling_pattern", MathHelper2.RandomVectorSet (256, new Vector2 (1, 1)));
			m_Uniforms.Set ("sampling_pattern_len", 256);

			//
			var particle_scale_factor = ValueProvider.Create (() => p.ParticleScaleFactor);
			var particle_count = ValueProvider.Create (() => p.PARTICLES_COUNT);

			var firstPassSolid =  RenderPassFactory.CreateSolidBox
			(
				 NormalDepth_Texture,
				 m_ParticleAttribute1_Texture,
				 Depth_Texture,
				 p.PositionBuffer,
				 p.ColorBuffer,
				 p.DimensionBuffer,
				 p.RotationLocalBuffer,
				 p.RotationBuffer,
				 particle_count,
				 particle_scale_factor,
				 p.CameraMvp
			);

			var firstPassShadow =  RenderPassFactory.CreateSolidBox
			(
				 Shadow_Texture,
				 p.PositionBuffer,
				 p.ColorBuffer,
				 p.DimensionBuffer,
				 p.RotationLocalBuffer,
				 p.RotationBuffer,
				 particle_count,
				 particle_scale_factor,
				 mode,
				 m_SunLightImpl.LightMvp
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
				 new FramebufferBindingSet(
				   new DrawFramebufferBinding { Attachment = FramebufferAttachment.DepthAttachment, Texture = Depth_Texture },
				   new DrawFramebufferBinding { VariableName = "Fragdata.color_luma", Texture = BeforeAA_Texture}
				 ),
				 p.ParticleStateArrayObject,
				 m_Uniforms,
				 new TextureBindingSet
				 {
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
			 firstPassSolid, firstPassShadow, aocPassSolid, aocBlur, thirdPassSolid, antialiasPass, finalRender
			);

			return m_Pass;
		}

		string IShadingSetup.Name
		{
			get
			{
				return "SolidBoxSetup";
			}
		}
		#endregion
	}
}

