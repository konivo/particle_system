#version 440
layout(local_size_x=8, local_size_y=8) in;
#define T_LAYOUT_OUT_DEPTH r32f
#define T_LAYOUT_OUT_COLORLUMA rgba32f

////////////////////////////////////////////////////////////////////////////////
//types//
//
#define Spectrum vec4
#define c_MaxRayDepth 8

struct Light
{
	vec3 pos;
	vec3 dir;
};

struct Range
{
	float Min;
	float Max;
};

struct Camera
{
	vec4 pos;
	vec4 ray_dir;
	vec4 look_dir;
	vec4 ray_intr;
	vec4 x_delta;
	vec4 y_delta;
};

struct Ray
{
	vec4 pos;
	vec4 dir;
};

struct Intersection
{
	// throughput, f(wo, wi, p) * cos (wi)
	Spectrum t;
	// normal
	vec4 n;
	// t and n might be computed given a ray and a primitive index
	int pi;
};

struct RayPath
{
	Ray[c_MaxRayDepth] rays;
	Intersection[c_MaxRayDepth + 1] intrs;
	Spectrum measure;
};

//subroutine float GetShadow(vec4 pos);

////////////////////////////////////////////////////////////////////////////////
//constants//
const int c_WorkGroupSize = int(gl_WorkGroupSize.x * gl_WorkGroupSize.y);
const int c_OccludersRimSize = 1;
const int c_OccludersGroupSizeX = int(gl_WorkGroupSize.x + c_OccludersRimSize * 2);
const int c_OccludersGroupSizeY = int(gl_WorkGroupSize.y + c_OccludersRimSize * 2);
const int c_OccludersGroupSize = c_OccludersGroupSizeX * c_OccludersGroupSizeY;
const int c_MaxLocalOccluders = 4;
const int c_MaxOccludersCount = c_MaxLocalOccluders * c_OccludersGroupSize;
const int c_MaxSamplesCount = min(256, c_MaxOccludersCount);
const float c_PI = 3.141592654f;
const float c_TWO_PI = 2 * 3.141592654f;
const float c_EXP_SCALE_FACTOR = 50;
const Light c_DefaultLight = Light( vec3(0, 0, 0), vec3(-1, -1, -1));

const float c_Epsilon = 0.001;
const float c_NearPlaneZ = 1;

/*
 * random functions
 */
const int[] PERMUTATION_TABLE = int[](151,160,137,91,90,15,131,13);
#define PERM(i) PERMUTATION_TABLE[(i)&0x7]

vec4[] sph = vec4[](
vec4( 0.04, -0.08, 0, 0.291366834171),
vec4( -0.1, -0.72, 0.92, 0.1756756757),
vec4( -0.8, -0.88, -0.76, 0.169273743),
vec4( 0.42, -0.36, -0.08, 0.2582417582),
vec4( -0.34, -0.44, -0.4, 0.1072164948),
vec4( 0.42, -0.82, 0.18, 0.2372093023),
vec4( 0.54, -0.56, -0.26, 0.1782051282),
vec4( 0.54, 0.9, -0.06, 0.2487046632),
vec4( -0.4, 0.7, -0.92, 0.1923076923),
vec4( -0.82, -0.14, -0.18, 0.1086419753),
vec4( -0.16, 0.1, -0.38, 0.2370860927),
vec4( 0.02, 0.7, 0.98, 0.196350365),
vec4( 0.86, 0.92, -0.94, 0.2063583815),
vec4( -0.9, 0.84, 0.1, 0.2402597403),
vec4( -0.2, 0.7, 0.96, 0.2363636364),
vec4( 0.72, 0.12, 0.92, 0.1846153846),
vec4( 0.26, 0.98, -0.78, 0.1959183673),
vec4( -0.7, 0.62, -0.56, 0.2576419214));

vec4[] sph_mat = vec4[](
vec4( 0.4, 0.08, 0, 0.1366834171),
vec4( -0.8, -0.88, -0.76, 0.169273743),
vec4( 0.42, -0.36, -0.08, 0.2582417582),
vec4( -0.34, -0.44, -0.4, 0.1072164948),
vec4( 0.42, -0.82, 0.18, 0.2372093023),
vec4( 0.54, -0.56, -0.26, 0.1782051282),
vec4( 0.54, 0.9, -0.06, 0.2487046632),
vec4( -0.4, 0.7, -0.92, 0.1923076923),
vec4( -0.82, -0.14, -0.18, 0.1086419753),
vec4( -0.16, 0.1, -0.38, 0.2370860927),
vec4( 0.02, 0.7, 0.98, 0.196350365),
vec4( 0.86, 0.92, -0.94, 0.2063583815),
vec4( -0.9, 0.84, 0.1, 0.2402597403),
vec4( -0.2, 0.7, 0.96, 0.2363636364),
vec4( 0.72, 0.12, 0.92, 0.1846153846),
vec4( -0.1, -0.72, 0.92, 0.1756756757),
vec4( 0.26, 0.98, -0.78, 0.1959183673),
vec4( -0.7, 0.62, -0.56, 0.2576419214));

////////////////////////////////////////////////////////////////////////////////
//uniforms//

/*
 * randomized vec2. Shall be uniformly distributed. Look for some advice, how to 
 * make it properly. They will be randomly rotated per pixel
 */
uniform vec2[256] u_SamplingPattern;

/*
 * images
 */
// Depth
layout(T_LAYOUT_OUT_DEPTH) restrict /*coherent, volatile, restrict, readonly, writeonly*/ uniform image2D u_TargetDepth;
// Color Luma
layout(T_LAYOUT_OUT_COLORLUMA) restrict /*coherent, volatile, restrict, readonly, writeonly*/ uniform image2D u_TargetColorLuma;

/*
 * model-view matrices 
 */
uniform mat4 modelviewprojection_transform;
uniform mat4 modelviewprojection_inv_transform;
uniform mat4 modelview_transform;
uniform mat4 modelview_inv_transform;
uniform mat4 projection_transform;
uniform mat4 projection_inv_transform;

/*
 * for spotlight real dimension
 * for directional light an spherical angles
 */
uniform Light u_Light = c_DefaultLight;
uniform vec3 u_LightAmbientColor = vec3(.1, .1, .1);
uniform float u_LightSize;

/*
 * GI settings
 */



////////////////////////////////////////////////////////////////////////////////
//shared storage//
//~ shared vec4[c_MaxOccludersCount] s_Occluders;
//~ //shared vec3[c_MaxOccludersCount] s_OccludersDirs;
//~ shared Range[c_MaxOccludersCount] s_OccRanges;
//~ shared vec4[c_OccludersGroupSize] s_Occludees;
//local storage
//shared vec4[gl_WorkGroupSize.x * gl_WorkGroupSize.y * c_ChunkSize_x * c_ChunkSize_y] s_LocalResult;
//shared vec3[gl_WorkGroupSize.x * gl_WorkGroupSize.y * c_ChunkSize_x * c_ChunkSize_y] s_TracedPoints;
//shared vec3[gl_WorkGroupSize.x * gl_WorkGroupSize.y * c_ChunkSize_x * c_ChunkSize_y] s_TracedFlags;

////////////////////////////////////////////////////////////////////////////////
//local variables//

struct { vec2 Size; vec2 Param; vec2 GroupParam; } 
	local_Target;

struct { 	vec2 RANDOMIZATION_VECTOR; 	int RANDOMIZATION_OFFSET; }
	local_Sampling;
	
RayPath local_RayPath;

Camera local_Camera;
	
int local_RandomState;

////////////////////////////////////////////////////////////////////////////////
//utility and library functions//
//
void InitSampling()
{
	int indexA = int(gl_GlobalInvocationID.x) *  173547 + int(gl_GlobalInvocationID.y) * 364525 + 1013904223;
	indexA = (indexA >> 4) & 0xFFF;

	int indexB = int(gl_GlobalInvocationID.x) *  472541 + int(gl_GlobalInvocationID.y) * 198791 + 2103477191;
	indexB = (indexB >> 4) & 0xFFF;

	local_Sampling.RANDOMIZATION_VECTOR = vec2( cos(c_TWO_PI * (indexA * indexB)/360), sin(c_TWO_PI * (indexB * indexA)/360));
	local_Sampling.RANDOMIZATION_OFFSET = indexB * indexA;
	local_RandomState = local_Sampling.RANDOMIZATION_OFFSET * 1;
}

//
vec2 GetSamplingPoint(int i)
{
	return reflect(u_SamplingPattern[(i * 97 + local_Sampling.RANDOMIZATION_OFFSET) % u_SamplingPattern.length()], local_Sampling.RANDOMIZATION_VECTOR);
}

//
int GetSamplingIndex(int i, int limit)
{
	return (i * 97 + local_Sampling.RANDOMIZATION_OFFSET) % limit;
}

//
vec2 GetSequenceHalton(int i)
{
	vec2 result = vec2(0);
	ivec2 base = ivec2(7, 3);
	vec2 f = 1 / vec2(base);
  ivec2 index = ivec2(i, i);
  while (index.x > 0 || index.y > 0)
  {
		result = result + f * (index % base);
		index = index / base;
		f = f / base; 
	}
  return result;
}

//
vec2 GetRandom2DHalton(vec2 min, vec2 max)
{
	local_RandomState++;
	vec2 v = GetSequenceHalton(local_RandomState * 23 % 10000 + 23);
	return mix(min, max, v);
}

//
int GetRandom1DHalton(int min, int max)
{
	vec2 v = GetRandom2DHalton(vec2(0), vec2(1));
	return int( mix(min, max + 1, (v.x + v.y) / 2));
}

//
vec4 GetClipCoord (vec2 param, float imagedepth)
{
	return vec4(2 * param - 1, imagedepth, 1);
}

//
vec4 Reproject (mat4 transform, vec4 vector)
{
	vec4 result = transform * vector;
	result /= result.w;
	return result;
}

//
vec4 Reproject (vec4 vector)
{
	vector /= vector.w;
	return vector;
}

void InitGlobals()
{
	InitSampling();
	
	local_Target.Param = vec2(gl_GlobalInvocationID)/imageSize(u_TargetColorLuma);
	local_Target.Size = vec2(imageSize(u_TargetColorLuma));
}

//returns value t, where given ray intersects sphere. Only positive return values are valid
//so it computes intersection only when ray starts outside spehere and aims toward  it
float SphereRayIntersection(vec4 sphere, vec3 raycenter, vec3 rayDirection)
{
	vec3 k = raycenter - sphere.xyz;

	vec3 koef = vec3(
			dot(rayDirection,rayDirection),
			2.0*dot(rayDirection, k),
			dot(k,k) - sphere.w*sphere.w);


	float discr = koef.y*koef.y - 4.0*koef.x*koef.z;

	if(discr < 0.0)
		return -1.0;
	else
		return (-koef.y - sqrt(discr))/(2.0*koef.x);
}

//returns true when sphere contains given point
bool SphereContains(in vec4 s, in vec3 point)
{
	return length(s.xyz - point) < s.w;
}

vec4 GetPerpendicularVector(const in vec4 n)
{
	vec4 result = {-n.y - n.z, n.x, n.x, 0};
	
	if(all(equal(result, vec4(0))))
	{
		result = vec4(n.zz, -n.x - n.y, 0);
	}
	
	return normalize(result);
}

void UniformSampleDisc(in vec2 uv, out vec2 xy)
{
	float r = sqrt(uv.x);
	float phi = c_TWO_PI * uv.y;
	
	xy = vec2(r * cos(phi), r * sin(phi));
}

void CosineSampleHemisphere(in vec2 uv, out vec4 xyz)
{
	vec2 xy;	
	UniformSampleDisc(uv, xy);
	xyz = vec4(xy, sqrt(1 - dot(xy, xy)), 0);
}

void UniformSampleHemisphere(in vec2 uv, out vec4 xyz)
{
	float r = sqrt(max(0, 1 - uv.x * uv.x));
	float phi = c_TWO_PI * uv.y;
	
	xyz = vec4(r * cos(phi), r * sin(phi), uv.x, 0);
}

void CosineSampleHemisphereAt(const in vec2 uv, const in vec4 n, out vec4 newdir)
{
	vec3 b1 = cross(n.xyz, GetPerpendicularVector(n).xyz);
	vec3 b2 = cross(b1, n.xyz);
	mat3 rotation = {	b1, b2, n.xyz };
	
	CosineSampleHemisphere(uv, newdir);		
	newdir.xyz = rotation * newdir.xyz;
}

void UniformSampleHemisphereAt(const in vec2 uv, const vec4 n, out vec4 newdir)
{
	vec3 b1 = cross(n.xyz, GetPerpendicularVector(n).xyz);
	vec3 b2 = cross(b1, n.xyz);
	mat3 rotation = {	b1, b2, n.xyz };
	
	UniformSampleHemisphere(uv, newdir);		
	newdir.xyz = rotation * newdir.xyz;
}

////////////////////////////////////////////////////////////////////////////////
//scene geometry functions//
//
void FindIntersection(in const Ray r, out float t, out int pi, out vec4 n)
{
	pi = -1;
	t = 1000000;
	n = vec4(1, 0, 0, 0);
	for(int i = 0; i < sph.length(); i++)
	{
		float newT = SphereRayIntersection(sph[i] * 50, r.pos.xyz, r.dir.xyz);
		if(newT > 0.001 && newT < t)
		{
			t = newT;
			pi = i;
		}
	}
	
	if(pi >= 0)
	{
		n = -normalize(vec4(sph[pi].xyz * 50, 1) - (r.pos + r.dir * t));
	}
}

void MaterialF(in int pi, in vec4 wi, in vec4 wo, out Spectrum f, out float pdf)
{
	if(pi < 0)
	{
		f = Spectrum(0.);
		pdf = 1;
	}
	else
	{
		f = Spectrum(1);
		pdf = 1 / c_TWO_PI;
	}
}

void MaterialE(in int pi, in vec4 wo, out Spectrum f, out float pdf)
{
	if(pi < 0)
	{
		f = Spectrum(0.141252915);
		pdf = 1;
	}
	else if(pi == 0)
	{
		f = Spectrum(00.84141252915);
		pdf = 1;
	}
	else
	{
		f = Spectrum(0);
		pdf = 1 / c_TWO_PI;
	}	
}

////////////////////////////////////////////////////////////////////////////////
// raytrace main body //
//
void main ()
{
	InitGlobals();
	
	//
	ivec2 size = imageSize(u_TargetColorLuma);
	vec2 isize = 1./imageSize(u_TargetColorLuma);
	vec2 param = vec2(gl_GlobalInvocationID.xy) * isize;
	ivec2 startPixelID = ivec2(gl_GlobalInvocationID.xy);
	int workGroupOffset = int(gl_LocalInvocationID.x * gl_WorkGroupSize.y + gl_LocalInvocationID.y);
	
	if(startPixelID.x >= size.x ||startPixelID.y >= size.y)
	{
		return;
	}
	
	//local_Camera initialization
	local_Camera.pos = modelview_inv_transform * vec4(0, 0, 0, 1);
	local_Camera.ray_intr = Reproject(modelviewprojection_inv_transform, GetClipCoord(param, -1));
	local_Camera.ray_dir = normalize(local_Camera.ray_intr - local_Camera.pos);
	local_Camera.x_delta = (Reproject(modelviewprojection_inv_transform, GetClipCoord(param + vec2(isize.x, 0), -1)) - local_Camera.ray_intr);
	local_Camera.y_delta = (Reproject(modelviewprojection_inv_transform, GetClipCoord(param + vec2(0, isize.y), -1)) - local_Camera.ray_intr);
	local_Camera.look_dir = modelview_inv_transform * vec4(0, 0, -1, 0);

	//
	Spectrum result = Spectrum(0);
	int rCount = 20;
	
	for(int j = 0; j < rCount; j++){
	RayPath rp;
	int depth = 1;
	
	// initialize the path	
	local_RayPath.rays[0].pos = local_Camera.pos;
	local_RayPath.rays[0].dir = local_Camera.ray_dir;
	
	// compute path in the scene
	do
	{
		float t = 0;
		int pi = 0;
		vec4 n;
		
		// compute ray's intersection with the scene along with index of primitive 
		// intersected and normal at the point of intersection
		FindIntersection(local_RayPath.rays[depth - 1], t, pi, n);
		
		local_RayPath.intrs[depth].pi = pi;
		local_RayPath.intrs[depth].n = n;
		
		if(pi < 0)
		{
			depth++;
			break;
		}
		local_RayPath.rays[depth].pos = local_RayPath.rays[depth - 1].pos + local_RayPath.rays[depth - 1].dir * t;
		
		// determine next path segment
		CosineSampleHemisphereAt(GetRandom2DHalton(vec2(0, 0), vec2(1, 1)), n, local_RayPath.rays[depth].dir);
	}
	while (++depth < c_MaxRayDepth);
	
	// shade it
	Spectrum throughput = Spectrum(1, 1, 1, 1);
	Spectrum L = Spectrum(0);
	for(int i = 1; i < depth; i++)
	{
		Spectrum f, e;
		float pdff, pdfe;
		vec4 wo = -local_RayPath.rays[i - 1].dir;
		vec4 wi = local_RayPath.rays[i].dir;
		
		int pi = local_RayPath.intrs[i].pi;
		float cost = abs(dot(wi, local_RayPath.intrs[i].n));
				
		//FSpecular(pi, wi, wo, f, pdf);
		MaterialF(pi, wi, wo, f, pdff);
		MaterialE(pi, wo, e, pdfe);
		
		L += e * throughput;
		throughput *= f * cost;
	}
	result += L;	
	}
	
	imageStore(u_TargetColorLuma, startPixelID, result / rCount);
	imageStore(u_TargetDepth, startPixelID, vec4(0));
}
