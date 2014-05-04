/*
 *  Expected declarations in the prepended code
 */
//layout(local_size_x= ... , local_size_y= ...) in;
//#define T_LAYOUT
//#define T_IMAGE uiimage2D
//#define T_PIXEL uvec4
//#line 1

#extension GL_ARB_gpu_shader5 : enable

#define FXAA_PC 1
#define FXAA_GLSL_130 1
#define FXAA_LINEAR 1

#pragma include <RenderPassFactory.Shaders.Fxaa3_8.include>

////////////////////////////////////////////////////////////////////////////////
//constants//

////////////////////////////////////////////////////////////////////////////////
//types//

////////////////////////////////////////////////////////////////////////////////
//uniforms//
uniform sampler2D u_Source;
layout(T_LAYOUT) /*coherent, volatile, restrict, readonly, writeonly*/ uniform T_IMAGE u_Target;

////////////////////////////////////////////////////////////////////////////////
//local storage//
//shared T_PIXEL[c_SharedArraySizeY] localResult;

////////////////////////////////////////////////////////////////////////////////
//local variables//

struct { vec2 Size; vec2 Param; }
	local_Target;
	
////////////////////////////////////////////////////////////////////////////////
//utility and library functions//

void InitGlobals()
{
	local_Target.Param = vec2(gl_GlobalInvocationID)/imageSize(u_Target);
	local_Target.Size = vec2(imageSize(u_Target));
}

////////////////////////////////////////////////////////////////////////////////
//kernel//
void main ()
{
	InitGlobals();
	
	// {x_} = 1.0/screenWidthInPixels
	// {_y} = 1.0/screenHeightInPixels
	float2 rcpFrame = 1.0/local_Target.Size;

	// This must be from a constant/uniform.
	// {x___} = 2.0/screenWidthInPixels
	// {_y__} = 2.0/screenHeightInPixels
	// {__z_} = 0.5/screenWidthInPixels
	// {___w} = 0.5/screenHeightInPixels
	float4 rcpFrameOpt = vec4( 2 * rcpFrame, 0.5 * rcpFrame);

	// {xy} = center of pixel
	float2 pos = local_Target.Param;

	// {xy__} = upper left of pixel
	// {__zw} = lower right of pixel
	float4 posPos = vec4( pos - rcpFrameOpt.zw, pos + rcpFrameOpt.zw);
	float4 result = FxaaPixelShader(pos, posPos, u_Source, rcpFrame, rcpFrameOpt);	
	imageStore(u_Target, ivec2(gl_GlobalInvocationID), result);
}
