using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using OpenTK;
namespace opentk.ShadingSetup
{
	/// <summary>
	///
	/// </summary>
	public abstract class RenderPass
	{	
		private static System.IO.FileSystemWatcher m_Watcher;
			
		private static Action<string> m_FileManipulated = x => {};
	
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
		
		static RenderPass()
		{
			m_Watcher = new System.IO.FileSystemWatcher 
			{ 
				Path = "../../shaders/"/*, Filter = "*.glsl"*/,
				NotifyFilter = System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.LastAccess | System.IO.NotifyFilters.FileName 
			};
			
			m_Watcher.EnableRaisingEvents = true;
			System.IO.FileSystemEventHandler hnd = (sender, e) => m_FileManipulated(e.Name);
			m_Watcher.Renamed += (sender, e) => m_FileManipulated(e.Name);
			m_Watcher.Changed += hnd;
			m_Watcher.Created += hnd;
			m_Watcher.Deleted += hnd;
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
		public static IEnumerable<Shader> GetResourceShaders (string name1, string name2)
		{
			var shaders = from res in System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceNames ()
				where res.Contains ("glsl") && res.Contains (name1) && res.Contains (name2)
				select Shader.GetShader(res, Shader.GetShaderTypeFromName(res), ResourcesHelper.GetText (res, System.Text.Encoding.UTF8));

			return shaders;
		}
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
		public static IEnumerable<Shader> GetShaders (params string[] name)
		{
			var dir = new System.IO.DirectoryInfo("shaders");
			
			var files = 
				dir.Exists?
					from file in dir.GetFiles("*.glsl", System.IO.SearchOption.AllDirectories)
					where name.All(n => file.Name.Contains(n))
					select file:
					new System.IO.FileInfo[0];
			
			var fileProviders = 
				files
					.Select(f => new { value = GetFileTextProvider(f), type = Shader.GetShaderTypeFromName(f.Name) } );
			
			var resProviders = from res in System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceNames ()
				where res.Contains ("glsl") && name.All(n => res.Contains(n))
			select new { value = ValueProvider.Create(ResourcesHelper.GetText (res, System.Text.Encoding.UTF8)), type = Shader.GetShaderTypeFromName(res) };
			
			var shaders = fileProviders.Concat(resProviders)
				.GroupBy(p => p.type, p => p.value)
					.Select(p => Shader.GetShader(string.Format("{0}.{1}", string.Join(".", name), p.Key.ToString()[0]), p.Key, p.ToArray()))
					.ToArray();			
			
			return shaders;
		}
//		public static IValueProvider<string>[] GetShaderCodeProvider (params string[] name)
//		{
//			var dir = new System.IO.DirectoryInfo("shaders");
//			
//			var files = 
//				dir.Exists?
//					from file in dir.GetFiles("*.glsl", System.IO.SearchOption.AllDirectories)
//					where name.All(n => file.Name.Contains(n))
//					select file:
//					new System.IO.FileInfo[0];
//			
//			var fileProviders = 
//				files
//					.Select(f => new { value = GetFileTextProvider(f), type = Shader.GetShaderTypeFromName(f.Name) } );
//			
//			var resProviders = from res in System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceNames ()
//				where res.Contains ("glsl") && name.All(n => res.Contains(n))
//			select new { value = ValueProvider.Create(ResourcesHelper.GetText (res, System.Text.Encoding.UTF8)), type = Shader.GetShaderTypeFromName(res) };
//			
//			var shaders = fileProviders.Concat(resProviders)
//				.Select(p => p.value)
//				.ToArray ();
//			
//			return shaders;
//		}
		
		private static IValueProvider<string> GetFileTextProvider(System.IO.FileInfo f)
		{
			Action<Action> invalidatorFactory = 
				invalidator =>
				{
								
				m_FileManipulated +=
				(filename) => 
					{
					
					 if(filename != f.Name)
						return;
						
						invalidator();
					
					};
			};
		
			return ValueProvider.Create(() => System.IO.File.ReadAllText(f.FullName), invalidatorFactory);
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
		[Obsolete("remove this constructor")]
		public SeparateProgramPass (string passName, string passNamespace, Action<GameWindow> beforeStateAction, Action<GameWindow> beforeRender, Action<GameWindow> render, params StatePart[] stateParts)
		:this(passName, passNamespace, beforeStateAction, beforeRender, render)
		{
			//create program from resources filtered by namespace and name
			var program = new Program (PassName, GetShaders(PassName, PassNamespace));
			m_State = new State (null, stateParts.Concat (new[] { program }).ToArray ());
		}

		public SeparateProgramPass (string passName, Action<GameWindow> beforeStateAction, Action<GameWindow> beforeRender, Action<GameWindow> render, IEnumerable<Shader> shaders, params StatePart[] stateParts)
		:this(passName, string.Empty, beforeStateAction, beforeRender, render)
		{
			var program = new Program (PassName, shaders.ToArray ());
			m_State = new State (null, stateParts.Concat (new[] { program }).ToArray ());
		}
		
		public SeparateProgramPass (string passName, Action<GameWindow> beforeStateAction, Action<GameWindow> beforeRender, Action<GameWindow> render, Program program, params StatePart[] stateParts)
			:this(passName, string.Empty, beforeStateAction, beforeRender, render)
		{
			m_State = new State (null, stateParts.Concat (new[] { program }).ToArray ());
		}
		
		public SeparateProgramPass (string passName, Action<GameWindow> beforeStateAction, Action<GameWindow> beforeRender, Action<GameWindow> render, State state)
			:this(passName, string.Empty, beforeStateAction, beforeRender, render)
		{
			m_State = state;
		}

		public SeparateProgramPass (string passName, Action<GameWindow> render, IEnumerable<Shader> shaders, params StatePart[] stateParts)
		: this(passName, new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().DeclaringType.Namespace.Split('.').Last(), null, null, render)
		{	
			var program = new Program (PassName, shaders.ToArray ());
			m_State = new State (null, stateParts.Concat (new[] { program }).ToArray ());
		}
		
		public SeparateProgramPass (string passName, Action<GameWindow> render, Program program, params StatePart[] stateParts)
			: this(passName, new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().DeclaringType.Namespace.Split('.').Last(), null, null, render)
		{	
			m_State = new State (null, stateParts.Concat (new[] { program }).ToArray ());
		}
		
		public SeparateProgramPass (string passName, Action<GameWindow> render, State state)
			: this(passName, new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().DeclaringType.Namespace.Split('.').Last(), null, null, render)
		{	
			m_State = state;
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

