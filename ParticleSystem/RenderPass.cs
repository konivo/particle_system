using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using OpenTK;
namespace opentk
{
	public abstract class RenderPass
	{
		public abstract void Render(GameWindow window);

		public IEnumerable<Shader> GetShaders()
		{
			var parentNamespace = GetType().Namespace.Split('.').Last();

			var shaders = from res in System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceNames ()
				where res.Contains ("glsl") && res.Contains (parentNamespace)
				select new Shader (res, ResourcesHelper.GetText (res, System.Text.Encoding.UTF8));

			return shaders;
		}
	}

	/// <summary>
	///
	/// </summary>
	public class SeparateProgramPass<TOwner>: RenderPass
	{
		private State m_State;

		private Action<GameWindow> m_Action;

		public string PassName
		{
			get;
			private set;
		}

		public override void Render(GameWindow window)
		{
			m_State.Activate();
			m_Action(window);
		}

		public SeparateProgramPass (string passName, Action<GameWindow> action, params StatePart[] stateParts)
		{
			//create program from resources filtered by passId
			var program = new Program(passName, GetShaders(passName).ToArray());
			m_State = new State(null, stateParts.Concat(new []{ program} ).ToArray());
			m_Action = action;
			PassName = passName;
		}

		private IEnumerable<Shader> GetShaders(string passName = "")
		{
			var parentNamespace = typeof(TOwner).Namespace.Split('.').Last();

			var shaders = from res in System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceNames ()
				where res.Contains ("glsl") && res.Contains (parentNamespace) && res.Contains(passName)
				select new Shader (res, ResourcesHelper.GetText (res, System.Text.Encoding.UTF8));

			return shaders;
		}
	}
}

