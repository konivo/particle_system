using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.ShadingSetup;
using OpenTK.Extensions;

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
			string scode =
@"
{0}define {1}
{0}define T_LAYOUT {2}

{0}ifdef PixelInternalFormatKind_Float
	{0}define T_IMAGE image2D
	{0}define T_PIXEL vec4
{0}endif

{0}ifdef PixelInternalFormatKind_Integer
	{0}define T_IMAGE iimage2D
	{0}define T_PIXEL ivec4
{0}endif

{0}ifdef PixelInternalFormatKind_UnsignedInteger
	{0}define T_IMAGE uiimage2D
	{0}define T_PIXEL uvec4
{0}endif
";
			scode = string.Format (scode, "#", "PixelInternalFormatKind_" + (filterparam.Source.InternalFormatKind & PixelInternalFormatKind.BaseCathegory), filterparam.Source.InternalFormat);
			
			return new SeparateProgramPass(
				"blur",
				window => { GLExtensions.DispatchCompute ((int)Math.Ceiling(filterparam.Source.Width/8.0), (int)Math.Ceiling(filterparam.Source.Height/8.0), 1); },
				new Program ("blur")
				{
				  RenderPass.GetShaders ("blur", "filters", "compute").PrependText(scode),
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

