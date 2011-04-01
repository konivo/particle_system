using System;
using System.Runtime.InteropServices;

namespace OpenTK.Graphics.OpenGL
{
	public static class GLExtensions
	{
		[DllImport("GL", EntryPoint = "glVertexAttribDivisor", ExactSpelling = true)]
		public extern static void VertexAttribDivisor(int index, int divisor);
	}
}

