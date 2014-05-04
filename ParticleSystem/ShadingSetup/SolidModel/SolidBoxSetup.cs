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
using OpenTK.Extensions;

namespace opentk.ShadingSetup
{
	/// <summary>
	///
	/// </summary>
	public class SolidBoxSetup: SolidSetupBase
	{
		#region IRenderSetup implementation
		protected override void PassSetup(ParticleSystemBase p)
		{
			base.PassSetup(p);

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
				 ValueProvider.Create ( () => "FragDepth" + SunLightImpl.ShadowmapType.ToString ()),
				 SunLightImpl.LightMvp
			);

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
			var aocBlur = RenderPassFactory.CreatePass(new BilateralFilter { Source = AOC_Texture, SourceK = NormalDepth_Texture, Target = AOC_Texture_Blurred_HV, Width = 4 });

			string scode =
				@"
{0}version 440
{0}define T_LAYOUT_OUT_DEPTH {1}
{0}define T_LAYOUT_OUT_COLORLUMA {2}

layout(local_size_x = {3}, local_size_y = {4}) in;
{0}line 1
";
			int workgroupSize = 16;
			scode = string.Format (scode, "#", ImageFormat.R32f, BeforeAA_Texture.InternalFormat, workgroupSize, workgroupSize);
			var namemodifier = string.Format ("wgsize:{0}x{0},fi:{1},fi:{2}", workgroupSize, ImageFormat.R32f, BeforeAA_Texture.InternalFormat);
			var deferredLigthing = new SeparateProgramPass
			(
				"shadingsetup.solidbox.deferredligthing",
				window => { GLExtensions.DispatchCompute ((int)Math.Ceiling((float)Depth_Texture.Width/workgroupSize), (int)Math.Ceiling((float)Depth_Texture.Height/workgroupSize), 1); },
				new Program ("shadingsetup.solidbox.deferredligthing")
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

			var antialiasPass = RenderPassFactory.CreatePass( new Fxaa3Filter{ Source = BeforeAA_Texture, Target = AA_Texture});

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
					firstPassSolid, firstPassShadow, aocPassSolid, aocBlur, deferredLigthing, antialiasPass, finalRender
			);
		}

		#endregion

		#region implemented abstract members of opentk.ShadingSetup.SolidSetupBase
		public override string Name
		{
			get
			{
				return "SolidBoxSetup";
			}
		}
		#endregion
	}
}

