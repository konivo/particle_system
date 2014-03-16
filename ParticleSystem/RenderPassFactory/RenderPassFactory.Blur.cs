using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.ShadingSetup;
namespace opentk
{
	public class BlurFilter
	{
		public TextureBase Source 
		{
			get; 
			set;
		}
		public TextureBase Target
		{
			get; 
			set;
		}
		public int Width
		{
			get; 
			set;
		}
	}

	public static partial class RenderPassFactory
	{
		public static RenderPass CreatePass(BlurFilter filterparam)
		{
			return new SeparateProgramPass(
				"blur",
				window => { GLExtensions.DispatchCompute ((int)Math.Ceiling(filterparam.Source.Width/8.0), (int)Math.Ceiling(filterparam.Source.Height/8.0), 1); },
				new Program ("blur")
				{
				  RenderPass.GetShaders ("blur", "filters")
				},
				new ImageBindingSet
				{
				  {"u_Source", () => filterparam.Source },
				  {"u_Target", () => filterparam.Target },
				},
				new UniformState
				{
				  {"u_FilterWidth", () => Math.Max (filterparam.Width, 1)},
				});
		}
	}
}

