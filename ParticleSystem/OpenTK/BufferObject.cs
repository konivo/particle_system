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
	public interface IHandle
	{
		int Handle
		{
			get;
		}
	}

	/// <summary>
	///
	/// </summary>
	public abstract class BufferObjectBase : IHandle, IDisposable
	{
		private Lazy<int> m_Handle;

		public string Name;
		public BufferUsageHint Usage;

		public bool Initialized
		{
			get;
			private set;
		}

		public int Handle
		{
			get { return m_Handle.Value; }
		}

		public BufferObjectBase ()
		{
			m_Handle = new Lazy<int> (() =>
			{
				int result;
				GL.GenBuffers (1, out result);
				Initialize (result);
				
				Initialized = true;
				return result;
			});
		}

		protected virtual void Initialize (int handle)
		{
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			if (m_Handle.IsValueCreated)
				GL.DeleteBuffers (1, new[] { Handle });
		}
		#endregion
	}

	/// <summary>
	///
	/// </summary>
	public sealed class BufferObject<T> : BufferObjectBase where T : struct
	{
		public readonly int TypeSize;
		private T[] m_Data;

		public T[] Data
		{
			get { return m_Data; }
			set
			{
				if (m_Data == value)
					return;
				
				m_Data = value;
				
				if (m_Data != null && Initialized)
				{
					Initialize (Handle);
				}
			}
		}

		public BufferObject (int typesize, int length) : this(typesize)
		{
			Data = new T[length];
		}

		public BufferObject (int typesize)
		{
			TypeSize = typesize;
		}

		protected override void Initialize (int handle)
		{
			if (m_Data == null)
			{
				throw new InvalidOperationException ();
			}
			
			GL.BindBuffer (BufferTarget.CopyReadBuffer, handle);
			GL.BufferData (BufferTarget.CopyReadBuffer, (IntPtr)(TypeSize * Data.Length), Data, Usage);
			Console.WriteLine ("buffer {3}: {0}, {1}, {2}", typeof(T), TypeSize, m_Data.Length, Name);
		}

		public void Publish ()
		{
			GL.BindBuffer (BufferTarget.CopyReadBuffer, Handle);
			GL.BufferSubData (BufferTarget.CopyReadBuffer, IntPtr.Zero, (IntPtr)(TypeSize * Data.Length), Data);
		}

		public void PublishPart (int start, int count)
		{
			unsafe
			{
				fixed (T* ptr = &Data[start])
				{
					GL.BindBuffer (BufferTarget.CopyReadBuffer, Handle);
					GL.BufferSubData (BufferTarget.CopyReadBuffer, (IntPtr)(TypeSize * start), (IntPtr)(TypeSize * count), (IntPtr)ptr);
				}
			}
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
	public class ObjectBinding
	{	}

	/// <summary>
	///
	/// </summary>
	public class BufferObjectBinding : ObjectBinding
	{
		public BufferObjectBase Buffer;

		public virtual BufferTarget Target
		{
			get { return BufferTarget.ArrayBuffer; }
			set { }
		}
	}

	/// <summary>
	///
	/// </summary>
	public sealed class VertexAttribute : BufferObjectBinding
	{
		public string AttributeName;
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
		public readonly List<VertexAttribute> AttribArrays = new List<VertexAttribute> ();

		public ArrayObject (params VertexAttribute[] states)
		{
			AttribArrays.AddRange (states);
		}

		[System.Diagnostics.Conditional("DEBUG")]
		private void PrintError (object bufferTarget)
		{
			var err = GL.GetError ();
			if (err != ErrorCode.NoError)
			{
				StackTrace str = new StackTrace ();
				Console.WriteLine ("error {0} at method {1}, line {2} when binding to target {3}", err, str.GetFrame (0).GetMethod ().Name, str.GetFrame (0).GetFileLineNumber (), bufferTarget);
			}
		}

		protected override Tuple<Action, Action> GetActivatorCore (State state)
		{
			//for vertex arrays object
			int Handle = -1;

			return new Tuple<Action, Action> (() =>
			{
				//print unobserved errors so far
				GLHelper.PrintError ();

				if (Handle != -1)
				{
					//vertex array binding
					GL.BindVertexArray (Handle);
					GLHelper.PrintError ();
				}
				else
				{
					//one time initialization
					var program = state.GetSingleState<Program> ();
					
					if (program == null)
						return;
					
					program.EnsureLinked ();
					
					//
					GL.GenVertexArrays (1, out Handle);
					GL.BindVertexArray (Handle);
					foreach (var item in AttribArrays)
					{
						int location = GL.GetAttribLocation (program.Handle, item.AttributeName);
						
						if (location == -1)
							continue;
						
						GL.BindBuffer (item.Target, item.Buffer.Handle);
						GL.VertexAttribPointer (location, item.Size, item.Type, item.Normalize, item.Stride, item.Pointer);
						GLExtensions.VertexAttribDivisor (location, item.Divisor);
						GL.EnableVertexAttribArray (location);
						
						Console.WriteLine ("binding {0} to target {1}: {2}: {3}", item.Buffer.Name, item.Target, item.AttributeName, location);
						PrintError (item.Target);
					}
				}
			}, () =>
			{
				var hnd = Handle;
				if (hnd != -1)
					GL.DeleteVertexArrays (1, ref hnd);
			});
		}

		#region IDisposable implementation
		protected override void DisposeCore ()
		{
		}
		#endregion
	}
}

