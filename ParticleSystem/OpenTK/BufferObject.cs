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
		private int m_Length;

		public string Name;
		public BufferUsageHint Usage;
		
		public readonly int TypeSize;
		
		public virtual int Length
		{
			get { return m_Length; }
			set 
			{
				if( m_Length == value)
					return;
					
				m_Length = value;
				if (m_Length != 0 && Initialized)
				{
					Initialize (Handle);
				}
			}
		}

		public bool Initialized
		{
			get;
			private set;
		}

		public int Handle
		{
			get { return m_Handle.Value; }
		}

		public BufferObjectBase (int typesize)
		{
			TypeSize = typesize;
		
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
		{	}

		#region IDisposable implementation
		public void Dispose ()
		{
			if (m_Handle.IsValueCreated)
				GL.DeleteBuffers (1, new[] { Handle });
		}
		#endregion
	}
	
	public abstract class BufferObject<T> : BufferObjectBase where T : struct
	{
		/// <summary>
		/// Gets or sets the <see cref="OpenTK.BufferObject`1"/> with the specified i.
		/// </summary>
		/// <param name="i">The index.</param>
		public abstract T this[int i]
		{
			get;
			set;
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="OpenTK.BufferObject`1"/> class.
		/// </summary>
		/// <param name="typesize">Typesize.</param>
		public BufferObject (int typesize):base(typesize)
		{		}
		/// <summary>
		/// Maps the read write.
		/// </summary>
		/// <returns>The read write.</returns>
		/// <param name="i">The index.</param>
		public abstract T[] MapReadWrite(ref int i);
		/// <summary>
		/// Maps the read.
		/// </summary>
		/// <returns>The read.</returns>
		/// <param name="i">The index.</param>
		public abstract T[] MapRead(ref int i);
		/// <summary>
		/// Publish this instance.
		/// </summary>
		public abstract void Publish ();
		/// <summary>
		/// Publishs the part.
		/// </summary>
		/// <param name="start">Start.</param>
		/// <param name="count">Count.</param>
		public abstract void PublishPart (int start, int count);
		/// <summary>
		/// 
		/// </summary>
		public abstract void Readout ();
		/// <summary>
		/// Copies to.
		/// </summary>
		/// <param name="target">Target.</param>
		/// <param name="index">Index.</param>
		public void CopyTo(T[] target, int index)
		{
			
		}
		/// <summary>
		/// Copies from.
		/// </summary>
		/// <param name="source">Source.</param>
		/// <param name="index">Index.</param>
		public void CopyFrom(T[] source, int index)
		{
			if(source.Length + index > Length)
				throw new ArgumentOutOfRangeException();
		
			var l = source.Length;
			while(l > 0)
			{
				var _index = index;
				var m = MapReadWrite(ref _index);
				var _l = Math.Min (l, m.Length - _index);
				Array.Copy(source, 0, m, _index, _l);
				
				l -= _l;
			}
		}
		/// <summary>
		/// Tos the array.
		/// </summary>
		/// <returns>The array.</returns>
		public T[] ToArray()
		{
			var result = new T[Length];
			
			var index = 0;
			while(index < Length)
			{
				var _index = index;
				var m = MapReadWrite(ref _index);
				Array.Copy(m, 0, result, index, m.Length);
				index += m.Length;
			}
			
			return result;
		}
		//todo:
		// GetManagedSize() returns the size of a structure whose type
		// is 'type', as stored in managed memory. For any referenec type
		// this will simply return the size of a pointer (4 or 8).
		protected static int GetManagedSize (Type type)
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
	public sealed class BufferObjectCompact<T> : BufferObject<T> where T: struct
	{
		private T[] m_Data;

		public override int Length 
		{
			get {
				return base.Length;
			}
			set {
				if (Length == value)
					return;
				
				m_Data = new T[value];				
				base.Length = value;
			}
		}
		
		public override T this[int i]
		{
			get
			{
				return m_Data[i];
			}
			set
			{
				m_Data[i] = value;
			}
		}

		public BufferObjectCompact (int typesize, int length) : this(typesize)
		{
			Length = length;
		}

		public BufferObjectCompact (int typesize) : base(typesize)
		{	}
		
		public override T[] MapReadWrite(ref int i)
		{
			return m_Data;
		}
		
		public override T[] MapRead(ref int i)
		{
			return m_Data;
		}

		protected override void Initialize (int handle)
		{
			if (m_Data == null)
			{
				throw new InvalidOperationException ();
			}
			
			GL.BindBuffer (BufferTarget.CopyReadBuffer, handle);
			GL.BufferData (BufferTarget.CopyReadBuffer, (IntPtr)(TypeSize * m_Data.Length), m_Data, Usage);
			Console.WriteLine ("buffer {3}: {0}, {1}, {2}", typeof(T), TypeSize, m_Data.Length, Name);
		}

		public override void Publish ()
		{
			GL.BindBuffer (BufferTarget.CopyReadBuffer, Handle);
			GL.BufferSubData (BufferTarget.CopyReadBuffer, IntPtr.Zero, (IntPtr)(TypeSize * m_Data.Length), m_Data);
		}

		public override void PublishPart (int start, int count)
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
		public override void Readout ()
		{
			GL.BindBuffer (BufferTarget.CopyReadBuffer, Handle);
			GL.GetBufferSubData (BufferTarget.CopyReadBuffer, IntPtr.Zero, (IntPtr)(TypeSize * m_Data.Length), m_Data);
		}
	}

	/// <summary>
	///
	/// </summary>
	// TODO: ReadOUt might be called from multiple threads at once, add some delegation to thread with Opengl context
	public sealed class BufferObjectSegmented<T> : BufferObject<T> where T : struct
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
		
		private readonly List<int> m_SegmentsOffsets;
		private readonly List<Segment> m_Segments;
		private volatile int m_Version;
		private ThreadLocal<Segment> m_LastSegment;
		
		public override int Length
		{			
			set 
			{
				if(Length == value)
					return;
				
				lock(m_SegmentsOffsets)
				{
					m_Segments.Clear();
					m_SegmentsOffsets.Clear ();
					m_Version++;					
					base.Length = value;
				}
			}
		}
		
		public override T this[int i]
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
			Length = length;
		}
		
		public BufferObjectSegmented (int typesize): base(typesize)
		{
			m_Segments = new List<Segment>(1000);
			m_SegmentsOffsets = new List<int>(1000);
			m_LastSegment = new ThreadLocal<Segment>();
		}
		
		public override T[] MapReadWrite(ref int i)
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
		
		public override T[] MapRead(ref int i)
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
				if (i >= Length)
					throw new IndexOutOfRangeException ();
				
//				ls = m_LastSegment.Value;	
//				if(ls != null && ls.Version == m_Version && ls.Offset <= i && ls.Offset + SEGMENT_LENGTH > i)
//				{
//					i -= ls.Offset;
//					return ls;
//				}
				
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
		
		public override void Publish ()
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
		
		public override void PublishPart (int start, int count)
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
		public override void Readout()
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

