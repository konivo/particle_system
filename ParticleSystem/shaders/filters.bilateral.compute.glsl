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
const float[] c_G = float[](0, 0, .1, .3, 6, 9 );

////////////////////////////////////////////////////////////////////////////////
//types//

////////////////////////////////////////////////////////////////////////////////
//uniforms//
// 32 is the maximal filter width .. 
uniform sampler2D u_SourceK;
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
//local variables//

struct { vec2 Size; vec2 Param; vec2 ParamStep; } 
	local_Source;

struct { 	vec2 RANDOMIZATION_VECTOR; 	int RANDOMIZATION_OFFSET; }
	local_Sampling;

////////////////////////////////////////////////////////////////////////////////
//utility and library functions//

void GetNormAt(ivec2 center, out vec4 s)
{
	s = texture2D(u_SourceK, center / vec2(local_Source.Size)) * 2 - 1;
}

float GetWeightAt(ivec2 center, vec4 s)
{
	vec4 k = texture2D(u_SourceK, center / vec2(local_Source.Size)) * 2 - 1;
	float k1 = 5*(dot(s.xyz, s.xyz) - dot(k.xyz, s.xyz)) + 10000* abs(s.w - k.w) ;
	int iindex = 5 - clamp(int(k1), 0, 5);
	return c_G[iindex]; 
}

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
	vec4 val_S;
	float normFactor = 0.001;
	
	GetNormAt(center, val_S);
	
	for(int i = -fw; i <= fw; i++)
	{
		ivec2 index = center + i * step;
		float w = GetWeightAt(index, val_S);
		
		sum += T_PIXEL(imageLoad(u_Source, index) * w);
		normFactor += w;
	}
	return T_PIXEL(sum/normFactor);
}

void InitGlobals()
{
	local_Source.Param = vec2(gl_GlobalInvocationID)/imageSize(u_Source);
	local_Source.ParamStep = 1./imageSize(u_Source);
	local_Source.Size = vec2(imageSize(u_Source));
}

////////////////////////////////////////////////////////////////////////////////
//kernel//
void main ()
{
	InitGlobals();
	
	int fw = clamp(u_FilterWidth, 0, c_MaxFilterWidth);
	ivec2 fcenter = ivec2(gl_GlobalInvocationID.xy);
	ivec2 lcenter = ivec2(gl_LocalInvocationID.xy);
	ivec2 lsize = ivec2(gl_WorkGroupSize.xy);
		
	for(int j = lcenter.y; j < lsize.y + 2 * fw; j += lsize.y)
		localResult[j] = FilterAt(fw, fcenter - lcenter + ivec2(0, j), ivec2(1, 0));
	
	barrier();
	
	T_PIXEL result = FilterAt(localResult, fw, lcenter.y + fw, 1);	
	imageStore(u_Target, fcenter, result);
}
