using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using OpenTK;
namespace opentk
{
	/// <summary>
	///
	/// </summary>
	public abstract class RenderPass
	{
		protected virtual State State
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public virtual void Render (GameWindow window)
		{
			BeforeState(window);
			State.Activate();
			AfterState(window);
		}

		protected virtual void BeforeState(GameWindow window)
		{		}

		protected virtual void AfterState(GameWindow window)
		{		}

		public virtual IEnumerable<Shader> GetShaders ()
		{
			var parentNamespace = GetType ().Namespace.Split ('.').Last ();
			return GetShaders(parentNamespace, "");
		}

		public static IEnumerable<Shader> GetShaders (string name1, string name2)
		{
			var shaders = from res in System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceNames ()
				where res.Contains ("glsl") && res.Contains (name1) && res.Contains (name2)
				select new Shader (res, ResourcesHelper.GetText (res, System.Text.Encoding.UTF8));

			return shaders;
		}
	}

	/// <summary>
	///
	/// </summary>
	public class SeparateProgramPass<TOwner> : RenderPass
	{
		private State m_State;

		protected override State State
		{
			get
			{
				return m_State;
			}
		}

		protected Action<GameWindow> BeforStateAction
		{
			get;
			private set;
		}

		protected Action<GameWindow> RenderAction
		{
			get;
			private set;
		}

		protected Action<GameWindow> BeforeRender
		{
			get;
			private set;
		}

		public string PassName
		{
			get;
			protected set;
		}

		public string PassNamespace
		{
			get;
			protected set;
		}

		protected override void BeforeState (GameWindow window)
		{
			BeforStateAction(window);
		}

		protected override void AfterState (GameWindow window)
		{
			BeforeRender(window);
			RenderAction(window);
		}

		private SeparateProgramPass (string passName, string passNamespace, Action<GameWindow> beforeStateAction, Action<GameWindow> beforeRender, Action<GameWindow> render)
		{
			BeforStateAction = beforeStateAction ?? new Action<GameWindow> (window => {});
			BeforeRender = beforeRender ?? new Action<GameWindow> (window => {});;
			RenderAction = render;
			PassName = passName;
			PassNamespace = passNamespace ?? string.Empty;
		}

		public SeparateProgramPass (string passName, string passNamespace, Action<GameWindow> beforeStateAction, Action<GameWindow> beforeRender, Action<GameWindow> render, params StatePart[] stateParts)
		:this(passName, passNamespace ?? typeof(TOwner).Namespace.Split ('.').Last (), beforeStateAction, beforeRender, render)
		{
			//create program from resources filtered by namespace and name
			var program = new Program (PassName, GetShaders ().ToArray ());
			m_State = new State (null, stateParts.Concat (new[] { program }).ToArray ());
		}

		public SeparateProgramPass (string passName, Action<GameWindow> beforeStateAction, Action<GameWindow> beforeRender, Action<GameWindow> render, IEnumerable<Shader> shaders, params StatePart[] stateParts)
		:this(passName, null, beforeStateAction, beforeRender, render)
		{
			//create program from resources filtered by namespace and name
			var program = new Program (PassName, shaders.ToArray ());
			m_State = new State (null, stateParts.Concat (new[] { program }).ToArray ());
		}

		public SeparateProgramPass (string passName, Action<GameWindow> render, params StatePart[] stateParts)
		: this(passName, null, null, null, render, stateParts)
		{
		}

		public override IEnumerable<Shader> GetShaders ()
		{
			return GetShaders(PassName, PassNamespace);
		}
	}
}

