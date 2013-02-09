#version 330
#extension GL_ARB_gpu_shader5 : enable

#define FXAA_PC 1
#define FXAA_GLSL_130 1
#define FXAA_LINEAR 1

#pragma include <RenderPassFactory.Shaders.Fxaa3_8.include>

//
uniform sampler2D source_texture;
//
uniform vec2 viewport_size;

//param in range (0, 0) to (1, 1)
in VertexData
{
	vec2 param;
}
IN_VertexData;

//computed ambient occlusion estimate
out vec4 OUT_FragData_result;

//
void main ()
{
	// {x_} = 1.0/screenWidthInPixels
	// {_y} = 1.0/screenHeightInPixels
	float2 rcpFrame = 1.0/viewport_size;

	// This must be from a constant/uniform.
	// {x___} = 2.0/screenWidthInPixels
	// {_y__} = 2.0/screenHeightInPixels
	// {__z_} = 0.5/screenWidthInPixels
	// {___w} = 0.5/screenHeightInPixels
	float4 rcpFrameOpt = vec4( 2 * rcpFrame, 0.5 * rcpFrame);

	// {xy} = center of pixel
	float2 pos = IN_VertexData.param;

	// {xy__} = upper left of pixel
	// {__zw} = lower right of pixel
	float4 posPos = vec4( pos - rcpFrameOpt.zw, pos + rcpFrameOpt.zw);

	OUT_FragData_result = FxaaPixelShader(pos, posPos, source_texture, rcpFrame, rcpFrameOpt);
}