#version 440
layout(local_size_x=8, local_size_y=8) in;

////////////////////////////////////////////////////////////////////////////////
//types//

////////////////////////////////////////////////////////////////////////////////
//uniforms//
// 32 is the maximal filter width .. 
uniform int u_FilterWidth;
layout(T_LAYOUT) uniform T_IMAGE u_Source;
layout(T_LAYOUT) uniform T_IMAGE u_Target;

////////////////////////////////////////////////////////////////////////////////
//local storage//
shared T_PIXEL[gl_WorkGroupSize.y + 16][gl_WorkGroupSize.x + 16] localResult;
shared T_PIXEL[gl_WorkGroupSize.y + 16][gl_WorkGroupSize.x] sumResult;

////////////////////////////////////////////////////////////////////////////////
//constants//

////////////////////////////////////////////////////////////////////////////////
//utility and library functions//
/* 
 * blur in x (horizontal)
 * take nine samples, with the distance blurSize between them
*/ 
T_PIXEL FilterAt(int fw, ivec2 center, ivec2 step)
{
	T_PIXEL sum = T_PIXEL(0);
	
	for(int i = -fw; i <= fw; i++)
	{
		ivec2 index = center + i * step;
		sum += localResult[index.y][index.x];
	}
	return sum/(2 * fw + 1);
}

////////////////////////////////////////////////////////////////////////////////
//kernel//
void main ()
{
	int fw = clamp(u_FilterWidth, 0, 8);
	ivec2 fcenter = ivec2(gl_GlobalInvocationID.xy);
	ivec2 lcenter = ivec2(gl_LocalInvocationID.xy);
	ivec2 lsize = ivec2(gl_WorkGroupSize.xy);
	
	for(int j = lcenter.y; j < lsize.y + 2 * fw; j += lsize.y)
		for(int i = lcenter.x; i < lsize.x + 2 * fw; i += lsize.x)
			localResult[j][i] = imageLoad(u_Source, fcenter - lcenter + ivec2(i, j) - fw);
			
	barrier();
	
	for(int j = lcenter.y; j < lsize.y + 2 * fw; j += lsize.y)
		sumResult[j][lcenter.x] = FilterAt(fw, ivec2(lcenter.x + fw, j), ivec2(1, 0));
	
	barrier();
	
	for(int j = lcenter.y; j < lsize.y + 2 * fw; j += lsize.y)
		localResult[j][lcenter.x] = sumResult[j][lcenter.x];
	
	barrier();
	
	T_PIXEL result = FilterAt(fw, ivec2(lcenter.x, lcenter.y + fw), ivec2(0, 1));
	
	imageStore(u_Target, fcenter, result);
}
