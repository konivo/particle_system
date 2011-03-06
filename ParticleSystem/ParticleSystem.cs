using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using OpenTK;
namespace opentk
{
	[InheritedExport]
	public abstract class ParticleSystem
	{
		public ParticleSystem GetInstance (GameWindow win)
		{
			var result = GetInstanceInternal(win);
			win.RenderFrame += result.HandleWinRenderFrame;

			return result;
		}

		protected abstract ParticleSystem GetInstanceInternal (GameWindow win);

		protected abstract void HandleFrame (GameWindow window);

		private void HandleWinRenderFrame (object sender, OpenTK.FrameEventArgs e)
		{
			HandleFrame ((GameWindow)sender);
		}

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

