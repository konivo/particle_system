using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.ShadingSetup;
using OpenTK.Extensions;
using opentk.Scene;

namespace opentk
{
	/// <summary>
	///
	/// </summary>
	public class SsaoEffect
	{
		public int SamplesCount
		{
			get; set;
		}
		
		public float OccMaxDist
		{
			get;
			set;
		}
		
		public float OccPixmax
		{
			get;
			set;
		}
		
		public float OccPixmin
		{
			get;
			set;
		}
		
		public float OccMinSampleRatio
		{
			get;
			set;
		}
		
		public bool OccConstantArea
		{
			get;
			set;
		}
		
		public float Strength
		{
			get;
			set;
		}
		
		public float Bias
		{
			get;
			set;
		}
		
		public float BlurEdgeAvoidance
		{
			get;
			set;
		}
		
		public TextureBase SourceNormalDepth 
		{
			get; 
			set;
		}
		
		public TextureBase Target
		{
			get; 
			set;
		}
		
		public ModelViewProjectionParameters SourceMvp
		{
			get; set;
		}
		
		[Obsolete]
		public int TextureSize
		{
			get;
			set;
		}
	}
	
	public static partial class RenderPassFactory
	{
		public static RenderPass CreatePass (SsaoEffect param)
		{
			var workgroupSize = 16;
			var current_pattern = MathHelper2.RandomVectorSet (256, new Vector2 (1, 1));
			string scode =
				@"
{0}version 440
{0}define T_LAYOUT_IN {1}
{0}define T_LAYOUT_OUT {2}

layout(local_size_x = {3}, local_size_y = {4}) in;
{0}line 1
";
			scode = string.Format (scode, "#", param.SourceNormalDepth.InternalFormat, param.Target.InternalFormat, workgroupSize, workgroupSize);
			var namemodifier = string.Format ("wgsize:{0}x{0},fi:{1},fo:{2}", workgroupSize, param.SourceNormalDepth.InternalFormat, param.Target.InternalFormat);
			
			return new SeparateProgramPass(
				"blur",
				window => { GLExtensions.DispatchCompute ((int)Math.Ceiling((float)param.Target.Width/workgroupSize), (int)Math.Ceiling((float)param.Target.Height/workgroupSize), 1); },
				new Program ("ssao")
				{
				RenderPass.GetShaders ("ssao", "effects", "compute").PrependText(namemodifier, scode),
				},
				new ImageBindingSet
				{
				{"u_NormalDepth", () => param.SourceNormalDepth },
				{"u_Target", () => param.Target },
				},
				new UniformState
				{
				  {"u_SamplingPattern", () => current_pattern},
				  {"u_SamplesCount", () => param.SamplesCount},
				  {"OCCLUDER_MAX_DISTANCE", () => param.OccMaxDist},
				  {"PROJECTED_OCCLUDER_DISTANCE_MAX_SIZE", () => param.OccPixmax},
				  {"PROJECTED_OCCLUDER_DISTANCE_MIN_SIZE", () => param.OccPixmin},
				  {"MINIMAL_SAMPLES_COUNT_RATIO", () => param.OccMinSampleRatio},
				  {"USE_CONSTANT_OCCLUDER_PROJECTION", () => param.OccConstantArea},
				  {"STRENGTH", () => param.Strength},
				  {"BIAS", () => param.Bias},
				  param.SourceMvp,				
			});			
		}
	}
}

