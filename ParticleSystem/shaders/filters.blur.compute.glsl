/*
 *  Expected declarations in the prepended code
 */
//layout(local_size_x= ... , local_size_y= ...) in;
//#define T_LAYOUT
//#define T_IMAGE uiimage2D
//#define T_PIXEL uvec4
//#line 1

////////////////////////////////////////////////////////////////////////////////
//constants//
const int c_MaxFilterWidth = 8;
const int c_SharedArraySizeY = int(gl_WorkGroupSize.y) + c_MaxFilterWidth * 2;

////////////////////////////////////////////////////////////////////////////////
//types//

////////////////////////////////////////////////////////////////////////////////
//uniforms//
// 32 is the maximal filter width .. 
uniform int u_FilterWidth;
layout(T_LAYOUT) /*coherent, volatile, restrict, readonly, writeonly*/ uniform T_IMAGE u_Source;
layout(T_LAYOUT) /*coherent, volatile, restrict, readonly, writeonly*/ uniform T_IMAGE u_Target;

////////////////////////////////////////////////////////////////////////////////
//local storage//
shared T_PIXEL[c_SharedArraySizeY] localResult;

// This is an example for a rectangular workgroup
//shared T_PIXEL[gl_WorkGroupSize.y + c_MaxFilterWidth * 2][gl_WorkGroupSize.x + c_MaxFilterWidth * 2] localResult;
//shared T_PIXEL[gl_WorkGroupSize.y + c_MaxFilterWidth * 2][gl_WorkGroupSize.x] sumResult;

////////////////////////////////////////////////////////////////////////////////
//utility and library functions//
/* 
 * blur in x (horizontal)
 * take nine samples, with the distance blurSize between them
*/

// This is an example for a rectangular workgroup 
/*
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
*/

T_PIXEL FilterAt(const in T_PIXEL[c_SharedArraySizeY] img, int fw, int center, int step)
{
	T_PIXEL sum = T_PIXEL(0);
	
	for(int i = -fw; i <= fw; i++)
	{
		int index = center + i * step;
		sum += img[index];
	}
	return sum/(2 * fw + 1);
}

T_PIXEL FilterAt(/*T_IMAGE img, */int fw, ivec2 center, ivec2 step)
{
	T_PIXEL sum = T_PIXEL(0);
	
	for(int i = -fw; i <= fw; i++)
	{
		ivec2 index = center + i * step;
		sum += imageLoad(u_Source, index);
	}
	return sum/(2 * fw + 1);
}

////////////////////////////////////////////////////////////////////////////////
//kernel//
void main ()
{
	int fw = clamp(u_FilterWidth, 0, c_MaxFilterWidth);
	ivec2 fcenter = ivec2(gl_GlobalInvocationID.xy);
	ivec2 lcenter = ivec2(gl_LocalInvocationID.xy);
	ivec2 lsize = ivec2(gl_WorkGroupSize.xy);
		
	for(int j = lcenter.y; j < lsize.y + 2 * fw; j += lsize.y)
		localResult[j] = FilterAt(fw, fcenter - lcenter + ivec2(0, j), ivec2(1, 0));
	
	barrier();
	
	T_PIXEL result = FilterAt(localResult, fw, lcenter.y + fw, 1);	
	imageStore(u_Target, fcenter, result);
	
	// This is an example for a rectangular workgroup
	/*
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
	*/
}
