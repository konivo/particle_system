using System;
using System.Runtime.InteropServices;

namespace OpenTK.Graphics.OpenGL
{
	public static class GLExtensions
	{
		[DllImport("GL", EntryPoint = "glVertexAttribDivisor", ExactSpelling = true)]
		public extern static void VertexAttribDivisor(int index, int divisor);

		[DllImport("GL", EntryPoint = "glGetSubroutineUniformLocation", ExactSpelling = true)]
		unsafe private extern static int glGetSubroutineUniformLocation(int program, ShaderType shadertype, string name);

		[DllImport("GL", EntryPoint = "glGetSubroutineIndex", ExactSpelling = true)]
		unsafe private extern static int glGetSubroutineIndex( int program, ShaderType shadertype, string name);

		[DllImport("GL", EntryPoint = "glUniformSubroutinesuiv", ExactSpelling = true)]
		unsafe private extern static void glUniformSubroutinesuiv(ShaderType shadertype, int count, int* indices);

		public static int GetSubroutineUniformLocation(int program, ShaderType shadertype, string name)
		{
			unsafe
			{
				fixed(char* fname = name)
				{
					return glGetSubroutineUniformLocation(program, shadertype, name);
				}
			}
		}

		public static int GetSubroutineIndex( int program, ShaderType shadertype, string name)
		{
			unsafe
			{
				fixed(char* fname = name)
				{
					return glGetSubroutineIndex(program, shadertype, name);
				}
			}
		}

		public static void UniformSubroutinesuiv(ShaderType shadertype, int[] indices)
		{
			unsafe
			{
				fixed(int* findices = indices)
				{
					glUniformSubroutinesuiv(shadertype, indices.Length, findices);
				}
			}
		}
	}
}

