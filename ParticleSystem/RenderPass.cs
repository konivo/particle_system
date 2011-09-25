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
		/// <summary>
		/// provides the State which is to be activated during the Render call
		/// </summary>
		//todo: refactor and define factory method for retrieving set of StateParts
		protected virtual State State
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// template method for rendering logic. Override when the template is not sufficient
		/// </summary>
		/// <param name="window">
		/// A <see cref="GameWindow"/>
		/// </param>
		public virtual void Render (GameWindow window)
		{
			BeforeState(window);
			State.Activate();
			AfterState(window);
		}

		//
		protected virtual void BeforeState(GameWindow window)
		{		}

		//
		protected virtual void AfterState(GameWindow window)
		{		}

		/// <summary>
		/// returns set of shaders each of which contains in its resource identifier both name1 and name2
		/// </summary>
		/// <param name="name1">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="name2">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="IEnumerable<Shader>"/>
		/// </returns>
		public static IEnumerable<Shader> GetShaders (string name1, string name2)
		{
			var shaders = from res in System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceNames ()
				where res.Contains ("glsl") && res.Contains (name1) && res.Contains (name2)
				select Shader.GetShader(res, ResourcesHelper.GetText (res, System.Text.Encoding.UTF8));

			return shaders;
		}
	}

	/// <summary>
	///
	/// </summary>
	public class SeparateProgramPass : RenderPass
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
		:this(passName, passNamespace, beforeStateAction, beforeRender, render)
		{
			//create program from resources filtered by namespace and name
			var program = new Program (PassName, GetShaders ().ToArray ());
			m_State = new State (null, stateParts.Concat (new[] { program }).ToArray ());
		}

		public SeparateProgramPass (string passName, Action<GameWindow> beforeStateAction, Action<GameWindow> beforeRender, Action<GameWindow> render, IEnumerable<Shader> shaders, params StatePart[] stateParts)
		:this(passName, string.Empty, beforeStateAction, beforeRender, render)
		{
			//create program from resources filtered by namespace and name
			var program = new Program (PassName, shaders.ToArray ());
			m_State = new State (null, stateParts.Concat (new[] { program }).ToArray ());
		}

		public SeparateProgramPass (string passName, Action<GameWindow> render, params StatePart[] stateParts)
		: this(passName, new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().DeclaringType.Namespace.Split('.').Last(), null, null, render, stateParts)
		{	}

		public IEnumerable<Shader> GetShaders ()
		{
			return GetShaders(PassName, PassNamespace);
		}
	}

	/// <summary>
	///
	/// </summary>
	public class CompoundRenderPass : RenderPass
	{
		private RenderPass[] m_Passes;

		public CompoundRenderPass (params RenderPass[] passes)
		{
			m_Passes = passes;
		}

		public override void Render (GameWindow window)
		{
			foreach (var item in m_Passes)
			{
				item.Render(window);
			}
		}
	}
}

