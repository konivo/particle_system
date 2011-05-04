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

	/// <summary>
	///
	/// </summary>
	public class SeparateProgramPass: RenderPass
	{
		private State m_State;

		private Action m_Action;

		private string m_PassId;

		public override void Render()
		{
			m_State.Activate();
			m_Action();
		}

		public SeparateProgramPass (string passId, Action action, State baseState, params StatePart[] stateParts)
		{
			//create program from resources filtered by passId

			//build up state

			//
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

