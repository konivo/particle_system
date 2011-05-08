using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

		public Program (string name, params Tuple<ShaderType, string[]>[] shaders) : this(name, (from s in shaders
			from shader in s.Item2
			select new Shader (s.Item1, shader)).ToArray ())
		{
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
				Console.WriteLine ("Program <{0}> error:\n{1}", Name, Log);
		}

		internal void EnsureLinked ()
		{
			if (!Linked.HasValue)
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
		public readonly string Code;
		public readonly ShaderType Type;
		public readonly string Name;

		public int Handle
		{
			get;
			private set;
		}

		public bool? Compiled
		{
			get;
			private set;
		}
		public string Log
		{
			get;
			private set;
		}

		public Shader (string name, ShaderType type, string code)
		{
			Name = name;
			Code = code;
			Type = type;
			
			Handle = GL.CreateShader (Type);
			
			Console.WriteLine ("Shader {0}:{1} declared", Name, Type);
		}

		public Shader (ShaderType type, string code) : this(Guid.NewGuid ().ToString (), type, code)
		{
		}

		public Shader (string name, string code) : this(name, GetShaderTypeFromName (name), code)
		{
		}

		public void Compile ()
		{
			GL.ShaderSource (Handle, Code);
			GL.CompileShader (Handle);
			
			int result;
			GL.GetShader (Handle, ShaderParameter.CompileStatus, out result);
			
			Compiled = result == 0;
			Log = GL.GetShaderInfoLog (Handle);
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