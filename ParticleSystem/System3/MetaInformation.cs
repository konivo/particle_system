using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace opentk.System3
{
	/// <summary>
	/// Meta information.
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public struct MetaInformation
	{	
		[FieldOffset(0x0)]
		public int LifeLen;
		[FieldOffset(0x4)]
		public int Leader;
		[FieldOffset(0x8)]
		public float Size;
		[FieldOffset(0x10)]
		public Vector3 Velocity;
		[FieldOffset(0x1C)]
		private int m_Padding;
	}
}
