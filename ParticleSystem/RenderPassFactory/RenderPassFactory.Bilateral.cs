using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.ShadingSetup;
using OpenTK.Extensions;

namespace opentk
{
	public class BilateralFilter
	{
		public TextureBase Source 
		{
			get; 
			set;
		}
		public TextureBase SourceK
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
		public static RenderPass CreatePass(BilateralFilter filterparam)
		{		
			string scode =
@"
{0}version 440
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
layout(local_size_x = 1, local_size_y = 64) in;
{0}line 1
";
			int wgsizex = 1;
			int wgsizey = 64;
			scode = string.Format (scode, "#", "PixelInternalFormatKind_" + (filterparam.Source.InternalFormatKind & PixelInternalFormatKind.BaseCathegory), filterparam.Source.InternalFormat);
			var namemodifier = string.Format ("wgsize:{0}x{1},fi:{2}", wgsizex, wgsizey, filterparam.Source.InternalFormat);
			
			return new SeparateProgramPass(
				"bilateralblur",
				window => { GLExtensions.DispatchCompute ((int)Math.Ceiling((float)filterparam.Source.Width/wgsizex), (int)Math.Ceiling((float)filterparam.Source.Height/wgsizey), 1); },
			  new Program ("bilateralblur")
				{
				  RenderPass.GetShaders ("bilateral", "filters", "compute").PrependText(namemodifier, scode),
				},
				new ImageBindingSet
				{
				  {"u_Source", () => filterparam.Source },
				  {"u_Target", () => filterparam.Target },
				},
				new TextureBindingSet
				{
				  {"u_SourceK", () => filterparam.SourceK },
				},
				new UniformState
				{
				  {"u_FilterWidth", () => Math.Max (filterparam.Width, 1)},
				});
		}
	}
}

