using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
namespace opentk
{
	public static partial class RenderPassFactory
	{

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
		                                    AocParameters parameters)
		{
			return CreateAoc(
			                 normalDepth,
			                 aoc,
			                 modelviewprojection,
			                 modelviewprojection_inv,
			                 projection,
			                 projection_inv,
			                 ValueProvider.Create (() => parameters.SamplesCount),
			                 ValueProvider.Create (() => parameters.OccMaxDist),
			                 ValueProvider.Create (() => parameters.OccPixmax),
			                 ValueProvider.Create (() => parameters.OccPixmin),
			                 ValueProvider.Create (() => parameters.OccMinSampleRatio),
			                 ValueProvider.Create (() => parameters.OccConstantArea),
			                 ValueProvider.Create (() => parameters.Strength),
			                 ValueProvider.Create (() => parameters.Bias));
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
		                                    IValueProvider<float> strength = null,
		                                    IValueProvider<float> bias = null)
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
			uniformState.Set ("AOC_STRENGTH", strength ?? ValueProvider.Create(2f));
			uniformState.Set ("AOC_BIAS", bias ?? ValueProvider.Create(-0.5f));

			return CreateFullscreenQuad ("aoc", "RenderPassFactory", viewport, null, window =>
			{
				GL.Clear (ClearBufferMask.ColorBufferBit);
				GL.Disable (EnableCap.DepthTest);
				GL.Disable (EnableCap.Blend);
			//pass state
			}, new FramebufferBindingSet (new DrawFramebufferBinding { VariableName = "Fragdata.aoc", Texture = aoc }), uniformState, new TextureBindingSet (new TextureBinding { VariableName = "normaldepth_texture", Texture = normalDepth }));
		}
	}
}

