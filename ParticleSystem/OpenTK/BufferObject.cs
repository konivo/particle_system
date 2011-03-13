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
	public class State
	{
		private readonly Dictionary<Type, ISet<StatePart>> m_StateSet = new Dictionary<Type, ISet<StatePart>> ();
		//private List<StateActivator> m_Activators = new List<StateActivator>();
	
		private Lazy<List<StateActivator>> m_Activators;
	
		public IEnumerable<StatePart> StateParts
		{
			get{
				return m_StateSet.Values.SelectMany(x => x);
			}
		}
	
		public T GetSingleState<T>() where T: StatePart
		{
			return GetSet(typeof(T)).OfType<T>().FirstOrDefault();
		}
		
		public IEnumerable<T> GetStates<T>() where T: StatePart
		{
			return GetSet(typeof(T)).OfType<T>();
		}
		
		private ISet<StatePart> GetSet(Type t)
		{
			ISet<StatePart> states;
			
			if(!m_StateSet.TryGetValue(t, out states))
			{
				m_StateSet[t] = states = new HashSet<StatePart>();
			}
			
			return states;
		}
		
		private void PutState(StatePart state)
		{
			GetSet(state.GetType()).Add(state);
		}
		
		private State ()
		{
			m_Activators = new Lazy<List<StateActivator>>(
			() => {
				var acts = 
					from i in m_StateSet.Values
					from j in i
					select j.GetActivator(this);
				
				return acts.ToList();			
			}, true);
		}
		
		public State (State basestate, params StatePart[] states)
		:this()
		{
			if(basestate != null)
			{
				throw new NotImplementedException();
			}
			
			foreach (StatePart item in states)
			{
				PutState(item);
			}
		}
		
		public void Activate ()
		{
			foreach (var item in m_Activators.Value)
				item.Activate();
		}
	}

	/// <summary>
	///
	/// </summary>
	internal class StateActivator: IDisposable
	{
		private readonly Action m_Activate;
		private readonly Action m_Dispose;
	
		public StateActivator (Action activate, Action dispose)
		{
			m_Activate = activate?? (() => {});
			m_Dispose = dispose?? (() => {});
		}
		
		public void Activate ()
		{
			m_Activate();
		}
	
		#region IDisposable implementation
		public void Dispose ()
		{
			m_Dispose();
		}
		#endregion	
	
	}

	/// <summary>
	///
	/// </summary>
	public abstract class StatePart : IDisposable
	{
		protected virtual void DisposeCore ()
		{
		}
		
		internal StateActivator GetActivator(State state)
		{
			var act = GetActivatorCore(state);
			return new StateActivator(act.Item1, act.Item2);
		}
		
		protected virtual Tuple<Action, Action> GetActivatorCore(State state)
		{
			return null;		
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			//for each member which implements IDisposable and is of the type StateBase do dispose() and then do InternalDispose
		}
		#endregion
	}

	/// <summary>
	///
	/// </summary>
	public abstract class BufferObjectBase: IDisposable
	{
		private Lazy<uint> m_Handle;

		public string Name;
		public BufferUsageHint Usage;
		public uint Handle 
		{
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

	/// <summary>
	///
	/// </summary>
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

	/// <summary>
	///
	/// </summary>
	public class BufferObjectBinding
	{
		private readonly List<int> m_TargetIndex = new List<int> ();

		public string BufferName;
		public virtual BufferTarget Target{ get {return BufferTarget.ArrayBuffer;} set{}}
		public List<int> TargetIndex {
			get { return m_TargetIndex; }
		}
	}

	/// <summary>
	///
	/// </summary>
	public sealed class VertexAttribute: BufferObjectBinding
	{
		public string AttributeName;
		public BufferObjectBase Buffer;
		public int Size;
		public VertexAttribPointerType Type;
		public bool Normalize;
		public int Stride;
		public int Divisor;
		public IntPtr Pointer;
	}

	/// <summary>
	///
	/// </summary>
	public class ArrayObject : StatePart
	{
		public readonly List<BufferObjectBinding> AttribArrays = new List<BufferObjectBinding>();

		public ArrayObject (params BufferObjectBinding[] states)
		{
			AttribArrays.AddRange(states);
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
		
		protected override Tuple<Action, Action> GetActivatorCore (State state)
		{
			int Handle = -1;
		
			return new Tuple<Action, Action>(
			() => 
			{
				PrintError ();
				
				if(Handle != -1){
					GL.BindVertexArray (Handle);
					PrintError ();
					return;
				}
					
				var program = state.GetSingleState<Program>();
				
				if(program == null)
					return;
				
				program.EnsureLinked();
				
				//
				GL.GenVertexArrays (1, out Handle);
				GL.BindVertexArray (Handle);
				foreach (var item in AttribArrays.OfType<VertexAttribute>()) 
				{
					int location = GL.GetAttribLocation(program.Handle, item.AttributeName);
					
					if(location == -1)
						continue;
					
					GL.BindBuffer (item.Target, item.Buffer.Handle);
					GL.VertexAttribPointer (location, item.Size, item.Type, item.Normalize, item.Stride, item.Pointer);
					GLExtensions.VertexAttribDivisor (location, item.Divisor);
					GL.EnableVertexAttribArray (location);
					
					Console.WriteLine ("binding {0} to target {1}: {2}: {3}", item.Buffer.Name, item.Target, item.AttributeName, location);
		
					PrintError (item.Target, location);
				}
			},
			() => 
			{
				var hnd = Handle;				
				if(hnd != -1)
					GL.DeleteVertexArrays (1, ref hnd);
			}
			);
		}

		#region IDisposable implementation
		protected override void DisposeCore ()
		{}
		#endregion
	}


	/// <summary>
	///
	/// </summary>
	public class Program : StatePart
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
		{}

		private void Link ()
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
				Console.WriteLine ("Program <{0}> linked:\n{1}", Name, Log);
			else
				Console.WriteLine ("Program <{0}> error:\n{1}", Name, Log);
		}
		
		internal void EnsureLinked()
		{
			if (!Linked.HasValue)
				Link ();
		}
		
		protected override Tuple<Action, Action> GetActivatorCore (State state)
		{
			return new Tuple<Action, Action>(
			() => 
			{
				EnsureLinked();
				GL.UseProgram (Handle);
			},
			null
			);			
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
	public class Pipeline : StatePart
	{
		public readonly uint Handle;

		public Pipeline (params Program[] innerState)
		{
			
		}

		protected override void DisposeCore ()
		{
			
		}
	}
}

