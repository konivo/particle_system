using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.ShadingSetup;
using OpenTK.Extensions;

namespace opentk
{
	public class Fxaa3Filter
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
	}

	public static partial class RenderPassFactory
	{
		public static RenderPass CreatePass(Fxaa3Filter filterparam)
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
			scode = string.Format (scode, "#", "PixelInternalFormatKind_" + (filterparam.Target.InternalFormatKind & PixelInternalFormatKind.BaseCathegory), filterparam.Target.InternalFormat);
			var namemodifier = string.Format ("wgsize:{0}x{1},fi:{2}", wgsizex, wgsizey, filterparam.Target.InternalFormat);
			
			return new SeparateProgramPass(
				"fxaa3",
				window => { GLExtensions.DispatchCompute ((int)Math.Ceiling((float)filterparam.Target.Width/wgsizex), (int)Math.Ceiling((float)filterparam.Target.Height/wgsizey), 1); },
			  new Program ("fxaa3")
				{
				  RenderPass.GetShaders ("fxaa3", "filters", "compute").PrependText(namemodifier, scode),
				},
				new ImageBindingSet
				{
				  {"u_Target", () => filterparam.Target },
				},
				new TextureBindingSet
				{
				  {"u_Source", () => filterparam.Source },
				});
		}
	}
}

