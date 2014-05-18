using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

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
			var pptr = System.Runtime.InteropServices.GCHandle.Alloc(m_Data, GCHandleType.Pinned);
			try
			{
				GL.BindBuffer (BufferTarget.CopyReadBuffer, Handle);
				GL.BufferSubData (BufferTarget.CopyReadBuffer, (IntPtr)(TypeSize * start), (IntPtr)(TypeSize * count), pptr.AddrOfPinnedObject());
			}
			finally
			{
				pptr.Free ();
			}
		}
		/// <summary>
		/// 
		/// </summary>
		public void Readout ()
		{
			GL.BindBuffer (BufferTarget.CopyReadBuffer, Handle);
			GL.GetBufferSubData (BufferTarget.CopyReadBuffer, IntPtr.Zero, (IntPtr)(TypeSize * Data.Length), Data);
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
	public sealed class BufferObjectSegmented<T> : BufferObjectBase where T : struct
	{
		[Flags]
		private enum SegmentState
		{
			ReadOut = 0x1, 
			Dirty = 0x2,
		}
		
		private class Segment
		{
			public readonly int Version;
			public readonly T[] Data;
			public readonly int Offset;
			public SegmentState State;
			
			public Segment (int offset, int version, T[] data)
			{
				Offset = offset;
				Data = data;
				Version = version;
			}
		}
				
		public const int SEGMENT_LENGTH = 500;
		
		private int m_Length;
		private readonly List<int> m_SegmentsOffsets;
		private readonly List<Segment> m_Segments;
		private volatile int m_Version;
		private ThreadLocal<Segment> m_LastSegment;
		
		public readonly int TypeSize;		
		
		public int Length
		{
			get { return m_Length; }
			set 
			{
				if( m_Length == value)
					return;
				
				lock(m_SegmentsOffsets)
				{
					m_Length = value;
					m_Segments.Clear();
					m_SegmentsOffsets.Clear ();
					m_Version++;
					if (m_Length != 0 && Initialized)
					{
						Initialize (Handle);
					}
				}
			}
		}
		
		public BufferObjectSegmented<T> Data
		{
			get{ return this;}
		}
		
		public T this[int i]
		{
			get
			{
				return MapRead(ref i)[i];			
			}
			set
			{
				MapReadWrite (ref i)[i] = value;
			}
		}
		
		public BufferObjectSegmented (int typesize, int length) : this(typesize)
		{
			m_Length = length;
		}
		
		public BufferObjectSegmented (int typesize)
		{
			TypeSize = typesize;
			m_Segments = new List<Segment>(1000);
			m_SegmentsOffsets = new List<int>(1000);
			m_LastSegment = new ThreadLocal<Segment>();
		}
		
		public T[] MapReadWrite(ref int i)
		{
			var segment = GetCreateSegment (ref i);				
			if((segment.State & SegmentState.ReadOut) == 0)
			{
				lock (segment) {
					
					if((segment.State & SegmentState.ReadOut) == 0)
						Readout(segment);
				}
			}
			
			segment.State |= SegmentState.Dirty;			
			return segment.Data;
		}
		
		public T[] MapRead(ref int i)
		{
			var segment = GetCreateSegment(ref i);				
			if((segment.State & SegmentState.ReadOut) == 0)
			{
				lock (segment) {
					
					if((segment.State & SegmentState.ReadOut) == 0)
						Readout(segment);
				}
			}
			return segment.Data;
		}
		
		private Segment GetCreateSegment(ref int i)
		{
			var ls = m_LastSegment.Value;
			if(ls != null && ls.Version == m_Version && ls.Offset <= i && ls.Offset + SEGMENT_LENGTH > i)
			{
				i -= ls.Offset;
				return ls;
			}
		
			lock (m_SegmentsOffsets) 
			{
				if (i >= m_Length)
					throw new IndexOutOfRangeException ();
				
				ls = m_LastSegment.Value;	
				if(ls != null && ls.Version == m_Version && ls.Offset <= i && ls.Offset + SEGMENT_LENGTH > i)
				{
					i -= ls.Offset;
					return ls;
				}
				
				var segIndex = m_SegmentsOffsets.BinarySearch (i);
				var segOffset = 0;
				if(segIndex >= 0)
				{
					segOffset = m_SegmentsOffsets[ segIndex];
				}
				else
				{
					segIndex = ~segIndex;
					if (segIndex == 0) {
						segOffset = i - i % SEGMENT_LENGTH;
						m_SegmentsOffsets.Add (segOffset);
						m_Segments.Add (new Segment (segOffset, m_Version, new T[Math.Min (SEGMENT_LENGTH, Length - segOffset)]){ State = SegmentState.ReadOut});
					} 
					else{
					  if (m_SegmentsOffsets[segIndex - 1] + SEGMENT_LENGTH <= i) {
							segOffset = i - i % SEGMENT_LENGTH;
							m_SegmentsOffsets.Insert (segIndex, segOffset);
							m_Segments.Insert (segIndex, new Segment (segOffset, m_Version, new T[Math.Min (SEGMENT_LENGTH, Length - segOffset)]){ State = SegmentState.ReadOut});
						} 
						else {
							segOffset = m_SegmentsOffsets [--segIndex];
						}
					}
				}
				
				m_LastSegment.Value = ls = m_Segments [segIndex];				
				i -= ls.Offset;
				return ls;
			}
		}
		
		protected override void Initialize (int handle)
		{
			if (Length <= 0)
			{
				throw new InvalidOperationException ();
			}
			
			GL.BindBuffer (BufferTarget.CopyReadBuffer, handle);
			GL.BufferData (BufferTarget.CopyReadBuffer, (IntPtr)(TypeSize * Length), IntPtr.Zero, Usage);
			Console.WriteLine ("buffer (segmented) {3}: {0}, {1}, {2}", typeof(T), TypeSize, Length, Name);
		}
		
		public void Publish ()
		{
			GL.BindBuffer (BufferTarget.CopyReadBuffer, Handle);
			
			int pcount = 0;
			for (int i = 0; i < m_Segments.Count; i++) 
			{
				var seg = m_Segments[i];
				if((seg.State & SegmentState.Dirty) > 0)
				{
					seg.State &= ~(SegmentState.Dirty | SegmentState.ReadOut);
					GL.BufferSubData (BufferTarget.CopyReadBuffer, (IntPtr)(TypeSize * seg.Offset), (IntPtr)(TypeSize * seg.Data.Length), seg.Data);
					
					pcount++;
				}
			}
		}
		
		public void PublishPart (int start, int count)
		{
			GL.BindBuffer (BufferTarget.CopyReadBuffer, Handle);
			
			for (int i = 0; i < m_Segments.Count; i++) 
			{
				var seg = m_Segments[i];
				if((seg.State & SegmentState.Dirty) > 0 && (seg.Offset < start + count) && (seg.Offset + seg.Data.Length > start))
				{
					seg.State &= ~(SegmentState.Dirty | SegmentState.ReadOut);
					GL.BufferSubData (BufferTarget.CopyReadBuffer, (IntPtr)(TypeSize * seg.Offset), (IntPtr)(TypeSize * seg.Data.Length), seg.Data);
				}
			}
		}
		/// <summary>
		/// Publishs the part.
		/// </summary>
		/// <param name="start">Start.</param>
		/// <param name="count">Count.</param>
		public void Readout()
		{
			GL.BindBuffer (BufferTarget.CopyReadBuffer, Handle);
			
			for (int i = 0; i < m_Segments.Count; i++) 
			{
				var seg = m_Segments[i];
				if((seg.State & SegmentState.ReadOut) == 0)
				{
					seg.State |= SegmentState.ReadOut;
					seg.State &= ~(SegmentState.Dirty);
					GL.GetBufferSubData (BufferTarget.CopyReadBuffer, (IntPtr)(TypeSize * seg.Offset), (IntPtr)(TypeSize * seg.Data.Length), seg.Data);
				}
			}
		}
		/// <summary>
		/// 
		/// </summary>
		private void Readout (Segment seg)
		{
			GL.BindBuffer (BufferTarget.CopyReadBuffer, Handle);
			GL.GetBufferSubData (BufferTarget.CopyReadBuffer, (IntPtr)(TypeSize * seg.Offset), (IntPtr)(TypeSize * seg.Data.Length), seg.Data);
			seg.State |= SegmentState.ReadOut;
			seg.State &= ~(SegmentState.Dirty);
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
			get;
			set;
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
		
		public override BufferTarget Target 
		{
			get 
			{
				return BufferTarget.ArrayBuffer;
			}
			set {	}
		}
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
					var program = state.GetActivateSingle<Program> ();					
					if (program == null)
						return;
					
					//
					GL.GenVertexArrays (1, out Handle);
					GL.BindVertexArray (Handle);
					foreach (var item in AttribArrays)
					{
						int location = GL.GetAttribLocation (program.Handle, item.AttributeName);
						
						if (location == -1)
							continue;

						var declCount = item.Size;
						var offset = 0;
						var slot = 0;
						GL.BindBuffer (item.Target, item.Buffer.Handle);

						while(declCount > 0)
						{
							GL.VertexAttribPointer (location + slot, Math.Min(declCount, 4), item.Type, item.Normalize, item.Stride, item.Pointer + offset);
							GLExtensions.VertexAttribDivisor (location + slot, item.Divisor);
							GL.EnableVertexAttribArray (location + slot);
							
							declCount -= 4;
							offset += 16;
							slot += 1;
						}
						
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

