using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;

namespace OpenTK.Extensions
{
	public static class ShaderExtensions
	{
		public static Shader PrependText(this Shader shader, string text)
		{
			return Shader.GetShader ( string.Format("{0}+'{1}'", shader.Name, text), shader.Type, shader.DynamicCode.Select(dc => dc.Combine(dcv => text + dcv)).ToArray());
		}
		
		public static IEnumerable<Shader> PrependText(this IEnumerable<Shader> shader, string text)
		{
			return shader.Select(s => s.PrependText(text)).ToArray ();
		}
	}
}

