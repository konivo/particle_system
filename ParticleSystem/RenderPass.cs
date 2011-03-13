using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using OpenTK;
namespace opentk
{
	public abstract class RenderPass
	{
		public abstract void Render();

		public IEnumerable<Shader> GetShaders()
		{
			var parentNamespace = GetType().Namespace.Split('.').Last();

			var shaders = from res in System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceNames ()
				where res.Contains ("glsl") && res.Contains (parentNamespace)
				select new Shader (res, ResourcesHelper.GetText (res, System.Text.Encoding.UTF8));

			return shaders;
		}
	}
}

