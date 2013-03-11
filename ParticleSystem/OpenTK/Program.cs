using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace OpenTK
{
	/// <summary>
	///
	/// </summary>
	public class Program : StatePart, IHandle
	{
		public readonly IEnumerable<Shader> Shaders;
		public readonly string Name;
		public int Handle
		{
			get;
			private set;
		}

		public bool? Linked
		{
			get;
			private set;
		}

		public IEnumerable<string> ShaderLogs
		{
			get { return Shaders.Select (x => x.Log); }
		}

		public string Log
		{
			get { return GL.GetProgramInfoLog (Handle); }
		}

		private Program (string name)
		{
			Handle = GL.CreateProgram ();
			Name = name;
		}

		public Program (string name, params Shader[] shaders) : this(name)
		{
			Shaders = Array.AsReadOnly (shaders);
			
			Console.WriteLine ("Program <{0}> declared, shaders: {1}", Name, String.Join (", ", shaders.Select (x => x.Name)));
		}
		
		public Program (string name, IEnumerable<Shader> shaders) : this(name)
		{
			Shaders = Array.AsReadOnly (shaders.ToArray());
			
			Console.WriteLine ("Program <{0}> declared, shaders: {1}", Name, String.Join (", ", shaders.Select (x => x.Name)));
		}

		private void Link ()
		{
			foreach (var item in Shaders)
			{
				if (!item.Compiled.HasValue)
					item.Compile ();
				
				GL.AttachShader (Handle, item.Handle);
			}
			
			GL.LinkProgram (Handle);
			
			int result;
			GL.GetProgram (Handle, ProgramParameter.LinkStatus, out result);
			Linked = result == 1;
			
			if (Linked.Value)
				Console.WriteLine ("Program <{0}> linked:\n{1}", Name, Log);
			else
				Console.WriteLine ("Program <{0}> error:\n{1}\n----------\n{2}", Name, Log, string.Join(Environment.NewLine, ShaderLogs));
		}

		internal void EnsureLinked ()
		{
			if (!Linked.HasValue ||
			   Shaders.Any (s => !s.Compiled.HasValue))
				Link ();
		}

		protected override Tuple<Action, Action> GetActivatorCore (State state)
		{
			return new Tuple<Action, Action> (() =>
			{
				EnsureLinked ();
				GL.UseProgram (Handle);
			}, null);
		}

		#region IDisposable implementation
		protected override void DisposeCore ()
		{
			GL.DeleteProgram (Handle);
		}
		#endregion
	}

	/// <summary>
	///
	/// </summary>
	public class Shader : IDisposable, IHandle
	{
		//
		private static readonly Regex m_IncludeRegex = new Regex(
			@"\#pragma include \<(?<includename>.+)\>",
			RegexOptions.Compiled);
		//
		private static readonly Dictionary<string, Shader> m_ShaderPool = new Dictionary<string, Shader>();
		/// <summary>
		///
		/// </summary>
		public readonly ShaderType Type;
		/// <summary>
		///
		/// </summary>
		public readonly string Name;
		/// <summary>
		///
		/// </summary>
		public int Handle
		{
			get;
			private set;
		}
		/// <summary>
		/// The dynamic code.
		/// </summary>
		public readonly IValueProvider<string>[] DynamicCode;		
		/// <summary>
		///
		/// </summary>
		public string Code 
		{
			get; 
			private set;
		}
		/// <summary>
		///
		/// </summary>
		public bool? Compiled
		{
			get;
			private set;
		}
		/// <summary>
		///
		/// </summary>
		public string Log
		{
			get;
			private set;
		}
		/// <summary>
		///
		/// </summary>
		public string ExpandedCode
		{
			get; private set;
		}
		/// <summary>
		///
		/// </summary>
		private Shader (string name, ShaderType type, IValueProvider<string>[] dynamiccode)
		{
			Name = name;
			DynamicCode = dynamiccode;
			Type = type;
			
			foreach (var item in dynamiccode) 
			{
				item.PropertyChanged += (sender, args) => Compiled = null;
			}
			
			Handle = GL.CreateShader (Type);			
			Console.WriteLine ("Shader {0}:{1} declared", Name, Type);
		}
		/// <summary>
		/// Gets the shader.
		/// </summary>
		/// <returns>
		/// The shader.
		/// </returns>
		/// <param name='name'>
		/// Name.
		/// </param>
		/// <param name='code'>
		/// Code.
		/// </param>
		/// <param name='dynamiccode'>
		/// Dynamiccode.
		/// </param>
		public static Shader GetShader(string name, ShaderType type, string code)
		{
			Shader result = null;

			if(!m_ShaderPool.TryGetValue(name, out result))
				m_ShaderPool.Add(name, result =  new Shader(name, type, new []{ValueProvider.Create (code)}));

			return result;
		}
		/// <summary>
		/// Gets the shader.
		/// </summary>
		/// <returns>
		/// The shader.
		/// </returns>
		/// <param name='name'>
		/// Name.
		/// </param>
		/// <param name='type'>
		/// Type.
		/// </param>
		/// <param name='code'>
		/// Code.
		/// </param>
		/// <param name='dynamiccode'>
		/// Dynamiccode.
		/// </param>
		public static Shader GetShader(string name, ShaderType type, params IValueProvider<string>[] dynamiccode)
		{
			Shader result = null;

			if(!m_ShaderPool.TryGetValue(name, out result))
				m_ShaderPool.Add(name, result =  new Shader(name, type, dynamiccode));

			return result;
		}
		/// <summary>
		/// Compile this instance.
		/// </summary>
		public void Compile ()
		{
			foreach (var item in DynamicCode) 
			{
				Code = item.Value;
				ExpandedCode =
					m_IncludeRegex.Replace(Code,
					m =>
					{
						try
						{
							return opentk.ResourcesHelper.GetTexts(m.Groups["includename"].Value, "", System.Text.Encoding.UTF8).Single();
						}
						catch (Exception ex)
						{
							throw new ApplicationException(string.Format("cannot find resource for inclusion: {0}", m.Groups["includename"].Value), ex);
						}
					});
	
				GL.ShaderSource (Handle, ExpandedCode);
				GL.CompileShader (Handle);
				
				int result;
				GL.GetShader (Handle, ShaderParameter.CompileStatus, out result);
				
				Log = GL.GetShaderInfoLog (Handle);
				Compiled = result == 1 && !Log.Contains ("error");
				
				if(Compiled.Value)
				break;
			}
		}

		public static ShaderType GetShaderTypeFromName (string name)
		{
			if (name.Contains ("fragment") || name.Contains ("frag"))
				return ShaderType.FragmentShader;
			else if (name.Contains ("vertex") || name.Contains ("vert"))
				return ShaderType.VertexShader;
			else if (name.Contains ("geom") || name.Contains ("geometry"))
				return ShaderType.GeometryShader;
			
			throw new ArgumentOutOfRangeException ();
		}


		#region IDisposable implementation
		public void Dispose ()
		{
			m_ShaderPool.Remove(this.Name);
			Compiled = false;
			GL.DeleteShader (Handle);
		}
		#endregion
	}

	/// <summary>
	///
	/// </summary>
	public class Pipeline : StatePart, IHandle
	{
		public int Handle
		{
			get;
			private set;
		}

		public Pipeline (params Program[] innerState)
		{
			
		}

		protected override void DisposeCore ()
		{
			
		}
	}
}