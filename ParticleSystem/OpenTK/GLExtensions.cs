using System;
using System.Runtime.InteropServices;

namespace OpenTK.Graphics.OpenGL
{
	public enum ShaderTypeExt: int{
		FragmentShader = ShaderType.FragmentShader,		
		VertexShader = ShaderType.VertexShader,
		GeometryShader = ShaderType.GeometryShader,
		ComputeShader = 0x91B9,
		TesselationShader = 0x8E87,
		TesselationControlShader = 0x8E88
	}

	public enum ProgramInterface: int{
		//#define GL_ATOMIC_COUNTER_BUFFER 0x92C0
		AtomicCounterBuffer = 0x92C0,
		//#define GL_UNIFORM 0x92E1
		Uniform = 0x92E1,
		//#define GL_UNIFORM_BLOCK 0x92E2
		UniformBlock = 0x92E2,
		//#define GL_PROGRAM_INPUT 0x92E3
		ProgramInput = 0x92E3,
		//#define GL_PROGRAM_OUTPUT 0x92E4
		ProgramOutput = 0x92E4,
		//#define GL_BUFFER_VARIABLE 0x92E5
		BufferVariable = 0x92E5,
		//#define GL_SHADER_STORAGE_BLOCK 0x92E6
		ShaderStorageBlock = 0x92E6
	}
	
	public enum InterfaceProperty: int
	{
		ActiveResources = 0x92F5,
//		#define GL_MAX_NAME_LENGTH 0x92F6
		MaxNameLength = 0x92F6,
//		#define GL_MAX_NUM_ACTIVE_VARIABLES 0x92F7
		MaxNumActiveVariables = 0x92F7,
//		#define GL_MAX_NUM_COMPATIBLE_SUBROUTINES 0x92F8
	}
	
	public enum ResourceProperty: int
	{
//		#define GL_NAME_LENGTH 0x92F9
		NameLength = 0x92F9,
//		#define GL_TYPE 0x92FA
		Type = 0x92FA,
//		#define GL_ARRAY_SIZE 0x92FB
		ArraySize = 0x92FB,
//		#define GL_OFFSET 0x92FC
		Offset = 0x92FC,
//		#define GL_BLOCK_INDEX 0x92FD
		BlockIndex = 0x92FD,
//		#define GL_ARRAY_STRIDE 0x92FE
		ArrayStride = 0x92FE,
//		#define GL_MATRIX_STRIDE 0x92FF
		MatrixStride = 0x92FF,
//		#define GL_IS_ROW_MAJOR 0x9300
		IsRowMajor = 0x9300,
//		#define GL_ATOMIC_COUNTER_BUFFER_INDEX 0x9301
		AtomicCounterBufferIndex = 0x9301,
//		#define GL_BUFFER_BINDING 0x9302
		BufferBinding = 0x9302,
//		#define GL_BUFFER_DATA_SIZE 0x9303
		BufferDataSize = 0x9303,
//		#define GL_NUM_ACTIVE_VARIABLES 0x9304
		NumActiveVariables = 0x9304,
//		#define GL_ACTIVE_VARIABLES 0x9305
		ActiveVariables = 0x9305,
//		#define GL_REFERENCED_BY_VERTEX_SHADER 0x9306
//		#define GL_REFERENCED_BY_TESS_CONTROL_SHADER 0x9307
//		#define GL_REFERENCED_BY_TESS_EVALUATION_SHADER 0x9308
//		#define GL_REFERENCED_BY_GEOMETRY_SHADER 0x9309
//		#define GL_REFERENCED_BY_FRAGMENT_SHADER 0x930A
//		#define GL_REFERENCED_BY_COMPUTE_SHADER 0x930B
		TopLevelArraySize = 0x930C,
//		#define GL_TOP_LEVEL_ARRAY_SIZE 0x930C
		TopLevelArrayStride = 0x930D,
//		#define GL_TOP_LEVEL_ARRAY_STRIDE 0x930D
		Location = 0x930E,
//		#define GL_LOCATION 0x930E
		LocationIndex = 0x930F
//		#define GL_LOCATION_INDEX 0x930F
	}
	
	public enum ImageAccess: int
	{
//		#define GL_READ_ONLY 0x88B8
		ReadOnly = 0x88B8,
//		#define GL_WRITE_ONLY 0x88B9
		WriteOnly = 0x88B9,
//		#define GL_READ_WRITE 0x88BA
		ReadWrite = 0x88BA,
	}
	
	public enum ImageFormat: int
	{
		RGBA32F = 0x8814,
		RGB32F = 0x8815,
		RGBA16F = 0x881A,
		RGB16F = 0x881B,
		RGBA32UI = 0x8D70,
		RGB32UI = 0x8D71,
		RGBA16UI = 0x8D76,
		RGB16UI = 0x8D77,
		RGBA8UI = 0x8D7C,
		RGB8UI = 0x8D7D,
		RGBA32I = 0x8D82,
		RGB32I = 0x8D83,
		RGBA16I = 0x8D88,
		RGB16I = 0x8D89,
		RGBA8I = 0x8D8E,
		RGB8I = 0x8D8F,
	}
	
	public enum BarierOperation: int
	{
		VERTEX_ATTRIB_ARRAY_BARRIER_BIT = 0x00000001,
		ELEMENT_ARRAY_BARRIER_BIT = 0x00000002,
		UNIFORM_BARRIER_BIT = 0x00000004,
		TEXTURE_FETCH_BARRIER_BIT = 0x00000008,
		SHADER_IMAGE_ACCESS_BARRIER_BIT = 0x00000020,
		COMMAND_BARRIER_BIT = 0x00000040,
		PIXEL_BUFFER_BARRIER_BIT = 0x00000080,
		TEXTURE_UPDATE_BARRIER_BIT = 0x00000100,
		BUFFER_UPDATE_BARRIER_BIT = 0x00000200,
		FRAMEBUFFER_BARRIER_BIT = 0x00000400,
		TRANSFORM_FEEDBACK_BARRIER_BIT = 0x00000800,
		ATOMIC_COUNTER_BARRIER_BIT = 0x00001000,
	}
	
	public enum ImageEnums: int
	{
		VERTEX_ATTRIB_ARRAY_BARRIER_BIT = 0x00000001,
		ELEMENT_ARRAY_BARRIER_BIT = 0x00000002,
		UNIFORM_BARRIER_BIT = 0x00000004,
		TEXTURE_FETCH_BARRIER_BIT = 0x00000008,
		SHADER_IMAGE_ACCESS_BARRIER_BIT = 0x00000020,
		COMMAND_BARRIER_BIT = 0x00000040,
		PIXEL_BUFFER_BARRIER_BIT = 0x00000080,
		TEXTURE_UPDATE_BARRIER_BIT = 0x00000100,
		BUFFER_UPDATE_BARRIER_BIT = 0x00000200,
		FRAMEBUFFER_BARRIER_BIT = 0x00000400,
		TRANSFORM_FEEDBACK_BARRIER_BIT = 0x00000800,
		ATOMIC_COUNTER_BARRIER_BIT = 0x00001000,
		MAX_IMAGE_UNITS = 0x8F38,
		MAX_COMBINED_IMAGE_UNITS_AND_FRAGMENT_OUTPUTS = 0x8F39,
		IMAGE_BINDING_NAME = 0x8F3A,
		IMAGE_BINDING_LEVEL = 0x8F3B,
		IMAGE_BINDING_LAYERED = 0x8F3C,
		IMAGE_BINDING_LAYER = 0x8F3D,
		IMAGE_BINDING_ACCESS = 0x8F3E,
		IMAGE_1D = 0x904C,
		IMAGE_2D = 0x904D,
		IMAGE_3D = 0x904E,
		IMAGE_2D_RECT = 0x904F,
		IMAGE_CUBE = 0x9050,
		IMAGE_BUFFER = 0x9051,
		IMAGE_1D_ARRAY = 0x9052,
		IMAGE_2D_ARRAY = 0x9053,
		IMAGE_CUBE_MAP_ARRAY = 0x9054,
		IMAGE_2D_MULTISAMPLE = 0x9055,
		IMAGE_2D_MULTISAMPLE_ARRAY = 0x9056,
		INT_IMAGE_1D = 0x9057,
		INT_IMAGE_2D = 0x9058,
		INT_IMAGE_3D = 0x9059,
		INT_IMAGE_2D_RECT = 0x905A,
		INT_IMAGE_CUBE = 0x905B,
		INT_IMAGE_BUFFER = 0x905C,
		INT_IMAGE_1D_ARRAY = 0x905D,
		INT_IMAGE_2D_ARRAY = 0x905E,
		INT_IMAGE_CUBE_MAP_ARRAY = 0x905F,
		INT_IMAGE_2D_MULTISAMPLE = 0x9060,
		INT_IMAGE_2D_MULTISAMPLE_ARRAY = 0x9061,
		UNSIGNED_INT_IMAGE_1D = 0x9062,
		UNSIGNED_INT_IMAGE_2D = 0x9063,
		UNSIGNED_INT_IMAGE_3D = 0x9064,
		UNSIGNED_INT_IMAGE_2D_RECT = 0x9065,
		UNSIGNED_INT_IMAGE_CUBE = 0x9066,
		UNSIGNED_INT_IMAGE_BUFFER = 0x9067,
		UNSIGNED_INT_IMAGE_1D_ARRAY = 0x9068,
		UNSIGNED_INT_IMAGE_2D_ARRAY = 0x9069,
		UNSIGNED_INT_IMAGE_CUBE_MAP_ARRAY = 0x906A,
		UNSIGNED_INT_IMAGE_2D_MULTISAMPLE = 0x906B,
		UNSIGNED_INT_IMAGE_2D_MULTISAMPLE_ARRAY = 0x906C,
		MAX_IMAGE_SAMPLES = 0x906D,
		IMAGE_BINDING_FORMAT = 0x906E,
		IMAGE_FORMAT_COMPATIBILITY_TYPE = 0x90C7,
		IMAGE_FORMAT_COMPATIBILITY_BY_SIZE = 0x90C8,
		IMAGE_FORMAT_COMPATIBILITY_BY_CLASS = 0x90C9,
		MAX_VERTEX_IMAGE_UNIFORMS = 0x90CA,
		MAX_TESS_CONTROL_IMAGE_UNIFORMS = 0x90CB,
		MAX_TESS_EVALUATION_IMAGE_UNIFORMS = 0x90CC,
		MAX_GEOMETRY_IMAGE_UNIFORMS = 0x90CD,
		MAX_FRAGMENT_IMAGE_UNIFORMS = 0x90CE,
		MAX_COMBINED_IMAGE_UNIFORMS = 0x90CF,
		//ALL_BARRIER_BITS = 0xFFFFFFFF,
	}
	/*
	#ifndef GL_ARB_shader_storage_buffer_object
	#define GL_ARB_shader_storage_buffer_object 1
	
	#define GL_SHADER_STORAGE_BARRIER_BIT 0x2000
	#define GL_MAX_COMBINED_SHADER_OUTPUT_RESOURCES 0x8F39
	#define GL_SHADER_STORAGE_BUFFER 0x90D2
	#define GL_SHADER_STORAGE_BUFFER_BINDING 0x90D3
	#define GL_SHADER_STORAGE_BUFFER_START 0x90D4
	#define GL_SHADER_STORAGE_BUFFER_SIZE 0x90D5
	#define GL_MAX_VERTEX_SHADER_STORAGE_BLOCKS 0x90D6
	#define GL_MAX_GEOMETRY_SHADER_STORAGE_BLOCKS 0x90D7
	#define GL_MAX_TESS_CONTROL_SHADER_STORAGE_BLOCKS 0x90D8
	#define GL_MAX_TESS_EVALUATION_SHADER_STORAGE_BLOCKS 0x90D9
	#define GL_MAX_FRAGMENT_SHADER_STORAGE_BLOCKS 0x90DA
	#define GL_MAX_COMPUTE_SHADER_STORAGE_BLOCKS 0x90DB
	#define GL_MAX_COMBINED_SHADER_STORAGE_BLOCKS 0x90DC
	#define GL_MAX_SHADER_STORAGE_BUFFER_BINDINGS 0x90DD
	#define GL_MAX_SHADER_STORAGE_BLOCK_SIZE 0x90DE
	#define GL_SHADER_STORAGE_BUFFER_OFFSET_ALIGNMENT 0x90DF
	
	typedef void (GLAPIENTRY * PFNGLSHADERSTORAGEBLOCKBINDINGPROC) (GLuint program, GLuint storageBlockIndex, GLuint storageBlockBinding);
	
	#define glShaderStorageBlockBinding GLEW_GET_FUN(__glewShaderStorageBlockBinding)
	
	#define GLEW_ARB_shader_storage_buffer_object GLEW_GET_VAR(__GLEW_ARB_shader_storage_buffer_object)
*/
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
		
		[DllImport("GL", EntryPoint = "glGetProgramInterfaceiv", ExactSpelling = true)]
		unsafe private extern static void glGetProgramInterfaceiv(int program, ProgramInterface intr, InterfaceProperty pname, int* parameters);
		
		[DllImport("GL", EntryPoint = "glGetProgramResourceIndex", ExactSpelling = true)]
		unsafe private extern static int glGetProgramResourceIndex(int program, ProgramInterface intr, string name);
		
		[DllImport("GL", EntryPoint = "glGetProgramResourceName", ExactSpelling = true)]
		unsafe private extern static void glGetProgramResourceName(int program, ProgramInterface intr, int index, int buffsize, int* length, sbyte* name);
		
		[DllImport("GL", EntryPoint = "glGetProgramResourceiv", ExactSpelling = true)]
		unsafe private extern static void glGetProgramResourceiv(int program, ProgramInterface intr, int index, int propcount, ResourceProperty* props, int buffsize, int* length, int* parameters);
		
		[DllImport("GL", EntryPoint = "glBindImageTexture", ExactSpelling = true)]
		unsafe private extern static void glBindImageTexture(int unit, int texture, int level, bool layered, int layer, ImageAccess access, ImageFormat format);
		
		[DllImport("GL", EntryPoint = "glDispatchCompute", ExactSpelling = true)]
		unsafe private extern static void glDispatchCompute(int nx, int ny, int nz);
		
		[DllImport("GL", EntryPoint = "glShaderStorageBlockBinding", ExactSpelling = true)]
		unsafe private extern static void glShaderStorageBlockBinding(int program, int blockIndex, int blockBinding);
		
		public static void BindShaderStorage(int program, int blockIndex, int blockBinding)
		{
			unsafe
			{
				glShaderStorageBlockBinding(program, blockIndex, blockBinding);
			}
		}
		
		public static void BindImageTexture(int unit, int texture, int level, bool layered, int layer, ImageAccess access, ImageFormat format)
		{
			unsafe
			{
				glBindImageTexture(unit, texture, level, layered, layer, access, format);
			}
		}
		
		public static void DispatchCompute(int nx, int ny, int nz)
		{
			glDispatchCompute(nx, ny, nz);
		}

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
		
		public static int GetProgramInterfaceiv(int program, ProgramInterface intr, InterfaceProperty pname)
		{
			unsafe
			{
				int result = 0;
				glGetProgramInterfaceiv(program, intr, pname, &result);
				
				return result;
			}
		}
		
		public static int GetProgramResourceIndex(int program, ProgramInterface intr, string pname)
		{
			unsafe
			{
				fixed(char* fparams = pname)
				{
					return glGetProgramResourceIndex(program, intr, pname);
				}
			}
		}
		
		public static string GetProgramResourceName(int program, ProgramInterface intr, int index)
		{
			unsafe
			{
				sbyte[] buffer = new sbyte[100];
				fixed(sbyte* pbuff = buffer)
				{
					
					glGetProgramResourceName(program, intr, index, 100, (int*)0, pbuff);
					return new string(pbuff);
				}
			}
		}
		
		public static int GetProgramResourceiv(int program, ProgramInterface intr, int index, ResourceProperty pname)
		{
			unsafe
			{
				int result = 0;
				glGetProgramResourceiv(program, intr, index, 1, &pname, 1, (int*) 0, &result);
				
				return result;
			}
		}
	}
}

