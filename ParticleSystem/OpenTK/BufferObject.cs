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
	public static class StateEnvironment
	{
		private static readonly Dictionary<Type, ISet<StateBase>> m_StateSet = new Dictionary<Type, ISet<StateBase>> ();
	
		public static IEnumerable<StateBase> CurrentState
		{
			get{
				return m_StateSet.Values.SelectMany(x => x);
			}
		}
	
		internal static void PutState(StateBase state)
		{
			ISet<StateBase> states;
			
			if(!m_StateSet.TryGetValue(state.GetType(), out states))
			{
				m_StateSet[state.GetType()] = states = new HashSet<StateBase>();
			}
			
			GetSet(state.GetType()).Add(state);
		}
		
		public static T GetSingleState<T>() where T: StateBase
		{
			return GetSet(typeof(T)).OfType<T>().FirstOrDefault();
		}
		
		public static IEnumerable<T> GetStates<T>() where T: StateBase
		{
			return GetSet(typeof(T)).OfType<T>();
		}
		
		private static ISet<StateBase> GetSet(Type t)
		{
			ISet<StateBase> states;
			
			if(!m_StateSet.TryGetValue(t, out states))
			{
				m_StateSet[t] = states = new HashSet<StateBase>();
			}
			
			return states;
		}
	}

	public abstract class StateBase : IDisposable
	{
		public void Activate ()
		{
			StateEnvironment.PutState(this);
			ActivateCore();
		}
		
		protected virtual void ActivateCore ()
		{
		}

		protected virtual void DisposeCore ()
		{
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			//for each member which implements IDisposable and is of the type StateBase do dispose() and then do InternalDispose
		}
		#endregion
	}

	public abstract class BufferObjectBase: IDisposable
	{
		private Lazy<uint> m_Handle;

		public string Name;
		public BufferUsageHint Usage;
		public uint Handle {
			get { return m_Handle.Value; }
		}

		public BufferObjectBase ()
		{
			m_Handle = new Lazy<uint> (() =>
			{
				uint result;
				GL.GenBuffers (1, out result);
				Initialize (result);
				return result;
			});
		}
		
		protected virtual void Initialize(uint handle)
		{}	

		#region IDisposable implementation
		public void Dispose ()
		{
			if (m_Handle.IsValueCreated)
				GL.DeleteBuffers (1, new uint[] { Handle });
		}
		#endregion
}

	public sealed class BufferObject<T> : BufferObjectBase where T : struct
	{
		public readonly int TypeSize;
		private int m_Length;
		private T[] m_Data;

		public T[] Data {
			get {			
				return m_Data; 
			}
			set {
				if(m_Data != null)
					throw new InvalidOperationException();
			
				m_Data = value;
			}
		}

		public BufferObject (int typesize, int length)
		{
			TypeSize = typesize;
			m_Length = length;
		}

		public BufferObject (int typesize) : this(typesize, -1)
		{}
		
		protected override void Initialize (uint handle)
		{
			if (m_Data == null) {
				m_Data = new T[m_Length];
				GL.BindBuffer (BufferTarget.CopyReadBuffer, handle);
				GL.BufferData (BufferTarget.CopyReadBuffer, (IntPtr)(TypeSize * m_Length), IntPtr.Zero, Usage);
			}
			else{
				GL.BindBuffer (BufferTarget.CopyReadBuffer, handle);
				GL.BufferData (BufferTarget.CopyReadBuffer, (IntPtr)(TypeSize * Data.Length), Data, Usage);			
			}
			
			Console.WriteLine ("buffer {3}: {0}, {1}, {2}", typeof(T), TypeSize, m_Data.Length, Name);
		}

		public void Publish()
		{
			GL.BindBuffer (BufferTarget.CopyReadBuffer, Handle);
			GL.BufferSubData(BufferTarget.CopyReadBuffer, IntPtr.Zero, (IntPtr)(TypeSize * Data.Length), Data);
		}

		//todo:
		// GetManagedSize() returns the size of a structure whose type
		// is 'type', as stored in managed memory. For any referenec type
		// this will simply return the size of a pointer (4 or 8).
		private static int GetManagedSize (Type type)
		{
			// all this just to invoke one opcode with no arguments!
			//typeof(BufferObjectBase),
			var method = new DynamicMethod ("GetManagedSizeImpl", typeof(int), new Type[0], true);
			
			ILGenerator gen = method.GetILGenerator ();
			
			gen.Emit (OpCodes.Sizeof, type);
			gen.Emit (OpCodes.Ret);
			
			return (int)method.Invoke (null, new object[0]);
		}
	}

	public class BufferObjectBinding
	{
		private readonly List<int> m_TargetIndex = new List<int> ();

		public string BufferName;
		public BufferTarget Target;
		public List<int> TargetIndex {
			get { return m_TargetIndex; }
		}
	}

	public struct AttribArray
	{
		public string AttributeName;
		public int Index;
		public int Size;
		public VertexAttribPointerType Type;
		public bool Normalize;
		public int Stride;
		public int Divisor;
		public IntPtr Pointer;
	}

	public class ArrayObject : StateBase
	{
		public readonly uint Handle;

		public readonly IList<AttribArray> AttribArrays;

		public ArrayObject (params object[] states)
		{
			PrintError ();
			
			var atts = states.OfType<AttribArray> ().ToDictionary (x => x.Index);
			
			var buffs = from i in states.OfType<BufferObjectBinding> ()
				join b in states.OfType<BufferObjectBase> () on i.BufferName equals b.Name
				from k in i.TargetIndex
				select new { buffer = b, index = k, target = i.Target } into result
				group result by result.target;
			
			//
			GL.GenVertexArrays (1, out Handle);
			GL.BindVertexArray (Handle);
			
			foreach (var item in buffs) {
				switch (item.Key) {
				case BufferTarget.ArrayBuffer:
					foreach (var buff in item) {
						var arr = atts[buff.index];
						GL.BindBuffer (item.Key, (uint)buff.buffer.Handle);
						GL.VertexAttribPointer ((uint)buff.index, arr.Size, arr.Type, arr.Normalize, arr.Stride, arr.Pointer);
						GLExtensions.VertexAttribDivisor (buff.index, arr.Divisor);
						GL.EnableVertexAttribArray ((uint)buff.index);
						
						Console.WriteLine ("binding {0} to target {1}: {2}: {3}", buff.buffer.Name, item.Key, arr.AttributeName, buff.index);
						
						PrintError (item.Key, buff.index);
					}

					break;
				default:
					foreach (var buff in item) {
						GL.BindBuffer (item.Key, buff.buffer.Handle);
						
						PrintError (item.Key, buff.index);
					}

					break;
				}
			}
			
			AttribArrays = atts.Values.ToList ().AsReadOnly ();
		}
		
		protected override void ActivateCore ()
		{
			GL.BindVertexArray (Handle);
		}

		[System.Diagnostics.Conditional("DEBUG")]
		private void PrintError (BufferTarget target, int binding)
		{
			var err = GL.GetError ();
			if (err != ErrorCode.NoError) {
				StackTrace str = new StackTrace ();
				Console.WriteLine ("error {0} at method {1}, line {2} when binding to target {3}", err, str.GetFrame (0).GetMethod ().Name, str.GetFrame (0).GetFileLineNumber (), target);
			}
		}

		[System.Diagnostics.Conditional("DEBUG")]
		private void PrintError ()
		{
			
			var err = GL.GetError ();
			if (err != ErrorCode.NoError) {
				StackTrace str = new StackTrace ();
				Console.WriteLine ("untreated error {0} at method {1}", err, str.GetFrame (0).GetMethod ().Name);
			}
		}

		#region IDisposable implementation
		protected override void DisposeCore ()
		{
			var hnd = Handle;
			GL.DeleteVertexArrays (1, ref hnd);
		}
		#endregion
	}



	public class Program : StateBase
	{
		public readonly IEnumerable<Shader> Shaders;
		public readonly int Handle;
		public readonly string Name;

		public bool? Linked { get; private set; }

		public IEnumerable<string> ShaderLogs {
			get { return Shaders.Select (x => x.Log); }
		}

		public string Log {
			get { return GL.GetProgramInfoLog (Handle); }
		}

		public IEnumerable<AttribArray> Attributes {
			set { SetAttributes (value); }
		}

		private Program (string name)
		{
			Handle = GL.CreateProgram ();
		}

		public Program (string name, params Shader[] shaders) : this(name)
		{
			Shaders = Array.AsReadOnly (shaders);
			
			Console.WriteLine ("Program {0} declared, shaders: {1}", Name, String.Join (", ", shaders.Select (x => x.Name)));
		}

		public Program (string name, params Tuple<ShaderType, string[]>[] shaders) : this(name, (from s in shaders
			from shader in s.Item2
			select new Shader (s.Item1, shader)).ToArray ())
		{
		}

		public void SetAttributes (IEnumerable<AttribArray> atts)
		{
			if (atts == null)
				throw new ArgumentNullException ();
			
			Linked = null;
			
			foreach (var item in atts) {
				if (!string.IsNullOrEmpty (item.AttributeName))
					GL.BindAttribLocation (Handle, item.Index, item.AttributeName);
			}
		}

		public void Link ()
		{
			foreach (var item in Shaders) {
				if (!item.Compiled.HasValue)
					item.Compile ();
				
				GL.AttachShader (Handle, item.Handle);
			}
			
			GL.LinkProgram (Handle);
			
			int result;
			GL.GetProgram (Handle, ProgramParameter.LinkStatus, out result);
			Linked = result == 1;
			
			if (Linked.Value)
				Console.WriteLine ("Program {0} Linked", Name);
			else
				Console.WriteLine ("Program {0} error:\n{1}", Name, Log);
		}

		protected override void ActivateCore ()
		{
			if (!Linked.HasValue)
				Link ();
			
			GL.UseProgram (Handle);
		}

		#region IDisposable implementation
		protected override void DisposeCore ()
		{
			GL.DeleteProgram (Handle);
		}
		#endregion
	}

	public class Shader : IDisposable
	{
		public readonly string Code;
		public readonly ShaderType Type;
		public readonly int Handle;
		public readonly string Name;

		public bool? Compiled { get; private set; }
		public string Log { get; private set; }

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
				return ShaderType.FragmentShader; else if (name.Contains ("vertex") || name.Contains ("vert"))
				return ShaderType.VertexShader;
			
			throw new ArgumentOutOfRangeException ();
		}


		#region IDisposable implementation
		public void Dispose ()
		{
			GL.DeleteShader (Handle);
		}
		#endregion
	}

	public class Pipeline : StateBase
	{
		public readonly uint Handle;

		public Pipeline (params Program[] innerState)
		{
			
		}

		protected override void ActivateCore ()
		{
			base.Activate ();
		}

		protected override void DisposeCore ()
		{
			
		}
	}
}

