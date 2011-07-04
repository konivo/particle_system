using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
namespace opentk
{
	public static class RenderPassFactory
	{
		public static RenderPass CreateFullscreenQuad (string passName, string passNamespace, IValueProvider<Vector2> viewportSize, Action<GameWindow> beforeState, Action<GameWindow> beforeRender, params StatePart[] stateParts)
		{
			var shaders = RenderPass.GetShaders ("fullquad", "RenderPassFactory").Concat (RenderPass.GetShaders (passName, passNamespace));
			
			Action<GameWindow> render = x =>
			{
				GL.Viewport (0, 0, (int)viewportSize.Value.X, (int)viewportSize.Value.Y);
				//draw one point
				GL.DrawArrays (BeginMode.Points, 0, 1);
			};
			
			return new SeparateProgramPass<object> (passName, beforeState, beforeRender, render, shaders, stateParts);
		}


		//computed world-space normal and clipping space depth (depth in range 0, 1)
		//where the result will be stored, results will be stored to the first component of the texture
		//how many samples will be used for occlusion estimation
		public static RenderPass CreateAoc (
		                                    TextureBase normalDepth,
		                                    TextureBase aoc,
		                                    IValueProvider<Matrix4> modelviewprojection,
		                                    IValueProvider<Matrix4> modelviewprojection_inv,
		                                    IValueProvider<Matrix4> projection,
		                                    IValueProvider<Matrix4> projection_inv,
		                                    IValueProvider<int> samplesCount,
		                                    IValueProvider<float> occ_max_dist = null,
		                                    IValueProvider<float> occ_pixmax = null,
		                                    IValueProvider<float> occ_pixmin = null,
		                                    IValueProvider<float> occ_min_sample_ratio = null,
		                                    IValueProvider<bool> occ_constant_area = null,
		                                    IValueProvider<float> strength = null)
		{
			var viewport = ValueProvider.Create (() => new Vector2 (aoc.Width, aoc.Height));
			var current_pattern = MathHelper2.RandomVectorSet (samplesCount.Value, new Vector2 (1, 1));

			var sampling_pattern = ValueProvider.Create
			(
				 () =>
				 current_pattern.Length == samplesCount.Value?
				 current_pattern:
				 (current_pattern = MathHelper2.RandomVectorSet (samplesCount.Value, new Vector2 (1, 1)))
			);

			var uniformState = new UniformState ();
			uniformState.Set ("viewport_size", viewport);
			uniformState.Set ("sampling_pattern", sampling_pattern);
			uniformState.Set ("sampling_pattern_len", samplesCount);

			uniformState.Set ("modelviewprojection_transform", modelviewprojection);
			uniformState.Set ("modelviewprojection_inv_transform", modelviewprojection_inv);
			uniformState.Set ("projection_transform", projection);
			uniformState.Set ("projection_inv_transform", projection_inv);

			uniformState.Set ("OCCLUDER_MAX_DISTANCE", occ_max_dist ?? ValueProvider.Create(35.0f));
			uniformState.Set ("PROJECTED_OCCLUDER_DISTANCE_MAX_SIZE", occ_pixmax ?? ValueProvider.Create(35.0f));
			uniformState.Set ("PROJECTED_OCCLUDER_DISTANCE_MIN_SIZE", occ_pixmin ?? ValueProvider.Create(2.0f));
			uniformState.Set ("MINIMAL_SAMPLES_COUNT_RATIO", occ_min_sample_ratio ?? ValueProvider.Create(0.1f));
			uniformState.Set ("USE_CONSTANT_OCCLUDER_PROJECTION", occ_constant_area ?? ValueProvider.Create(false));
			uniformState.Set ("AOC_STRENGTH", strength ?? ValueProvider.Create(0.1f));

			return CreateFullscreenQuad ("aoc", "RenderPassFactory", viewport, null, window =>
			{
				GL.Clear (ClearBufferMask.ColorBufferBit);
				GL.Disable (EnableCap.DepthTest);
				GL.Disable (EnableCap.Blend);
			//pass state
			}, new FramebufferBindingSet (new DrawFramebufferBinding { VariableName = "Fragdata.aoc", Texture = aoc }), uniformState, new TextureBindingSet (new TextureBinding { VariableName = "normaldepth_texture", Texture = normalDepth }));
		}

		//
		public static RenderPass CreateBlurFilter
		(
			 TextureBase source,
			 TextureBase interm,
			 TextureBase result)
		{
			return CreateSeparableFilter("blur", "RenderPassFactory", source, interm, result);
		}

		//
		public static RenderPass CreateBilateralFilter
		(
			 TextureBase source,
			 TextureBase interm,
			 TextureBase result)
		{
			return CreateSeparableFilter("bilateralfilter", "RenderPassFactory", source, interm, result);
		}

		//
		public static RenderPass CreateFilter
		(
			 string filterName,
			 string filterNamespace,
			 TextureBase source,
			 TextureBase result)
		{
			var viewport = ValueProvider.Create (() => new Vector2 (result.Width, result.Height));
			var uniformState = new UniformState ();
			uniformState.Set ("viewport_size", viewport);

			var _1stPass = CreateFullscreenQuad (filterName, filterNamespace, viewport,
			null
			,
			window =>
			{
				GL.Clear (ClearBufferMask.ColorBufferBit);
				GL.Disable (EnableCap.DepthTest);
				GL.Disable (EnableCap.Blend);
			//pass state
			},
			new FramebufferBindingSet (
			new DrawFramebufferBinding { VariableName = "Fragdata.result", Texture = result }),
			uniformState,
			new TextureBindingSet (new TextureBinding { VariableName = "source_texture", Texture = source }));
			return _1stPass;
		}

		//
		public static RenderPass CreateSeparableFilter
		(
			 string filterName,
			 string filterNamespace,
			 TextureBase source,
			 TextureBase interm,
			 TextureBase result)
		{
			var viewport = ValueProvider.Create (() => new Vector2 (result.Width, result.Height));
			var horizontal = false;
			var uniformState = new UniformState ();
			uniformState.Set ("viewport_size", viewport);
			uniformState.Set ("horizontal", ValueProvider.Create (() => horizontal));

			var _1stPass = CreateFullscreenQuad (filterName, filterNamespace, viewport,
			window =>
			{
				horizontal = false;
			}
			,
			window =>
			{
				GL.Clear (ClearBufferMask.ColorBufferBit);
				GL.Disable (EnableCap.DepthTest);
				GL.Disable (EnableCap.Blend);
			//pass state
			},
			new FramebufferBindingSet (
			new DrawFramebufferBinding { VariableName = "Fragdata.result", Texture = interm }),
			uniformState,
			new TextureBindingSet (new TextureBinding { VariableName = "source_texture", Texture = source }));

			var _2ndPass = CreateFullscreenQuad (filterName, filterNamespace, viewport,
			window =>
			{
				horizontal = true;
			}
			,
			window =>
			{
				GL.Clear (ClearBufferMask.ColorBufferBit);
				GL.Disable (EnableCap.DepthTest);
				GL.Disable (EnableCap.Blend);
			//pass state
			},
			new FramebufferBindingSet (
			new DrawFramebufferBinding { VariableName = "Fragdata.result", Texture = result }),
			uniformState,
			new TextureBindingSet (new TextureBinding { VariableName = "source_texture", Texture = interm }));

			return new CompoundRenderPass(_1stPass, _2ndPass);
		}
	}
}

