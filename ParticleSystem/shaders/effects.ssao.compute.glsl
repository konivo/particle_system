#version 440
layout(local_size_x = 4, local_size_y = 4) in;

////////////////////////////////////////////////////////////////////////////////
//constants//
const int c_WorkGroupSize = int(gl_WorkGroupSize.x * gl_WorkGroupSize.y);
const int c_MaxLocalOccluders = 2;
const int c_MaxOccludersCount = c_MaxLocalOccluders * c_WorkGroupSize;
const int c_MaxSamplesCount = min(64, c_MaxOccludersCount);
const float c_PI = 3.141592654f;
const float c_TWO_PI = 2 * 3.141592654f;

////////////////////////////////////////////////////////////////////////////////
//types//
struct Occluder
{
	vec3 Position;
	float Radius;
};

struct Sample
{
	vec4 Position;
	vec4 CamPosition;
	vec3 N;
	float CamDist;
};

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
// texture holding depth in projection space and normal in camera space
layout(rgba32f) restrict readonly /*coherent, volatile, readonly, writeonly*/ uniform image2D u_NormalDepth;
// computed ambient occlusion estimate
layout(r32f) restrict /*coherent, volatile, restrict, readonly, writeonly*/ uniform image2D u_Target;

/*
 * model-view matrices 
 */
uniform mat4 modelviewprojection_transform;
uniform mat4 modelviewprojection_inv_transform;
uniform mat4 projection_transform;
uniform mat4 projection_inv_transform;

/*
 * SSAO settings
 */
// how many samples will be used for current pixel (max is 256)
uniform int u_SamplesCount;

// maximum distance of an occluder in the world space
uniform float OCCLUDER_MAX_DISTANCE = 35.5;

// if true, occluder projection will be equal to max size.
// TODO: when true, occluder max distance has to be recomputed
uniform bool USE_CONSTANT_OCCLUDER_PROJECTION = false;

// these two constants will limit how big area in image space will be sampled.
// farther areas will be smaller in size and thus will contain less samples,
// less far areas will be bigger in screen size and will be covered by more samples.
// Samples count should change with square of projected screen size?
uniform float PROJECTED_OCCLUDER_DISTANCE_MIN_SIZE = 2;
uniform float PROJECTED_OCCLUDER_DISTANCE_MAX_SIZE = 35;

// determines how big fraction of the samples will be used for the minimal computed 
// projection of occluder distance
uniform float MINIMAL_SAMPLES_COUNT_RATIO = 0.5;
uniform float STRENGTH = 0.1;
uniform float BIAS = 0.1;


////////////////////////////////////////////////////////////////////////////////
//shared storage//
shared Occluder[c_MaxOccludersCount] s_Occluders;
shared vec2 s_Rf;

////////////////////////////////////////////////////////////////////////////////
//local variables//
struct { vec2 Size; vec2 Param; vec2 GroupParam; } 
	local_Target;

vec2 local_Rf;
// P is the sample in target for which Ssao is computed by the current invocation
Sample local_P;

struct { 	vec2 RANDOMIZATION_VECTOR; 	int RANDOMIZATION_OFFSET; }
	local_Sampling;

////////////////////////////////////////////////////////////////////////////////
//utility and library functions//

//
void InitSampling()
{
	int indexA = int(gl_WorkGroupID.x) *  173547 + int(gl_WorkGroupID.y) * 364525 + 1013904223;
	indexA = (indexA >> 4) & 0xFFF;

	int indexB = int(gl_WorkGroupID.x) *  472541 + int(gl_WorkGroupID.y) * 198791 + 2103477191;
	indexB = (indexB >> 4) & 0xFFF;

	local_Sampling.RANDOMIZATION_VECTOR = vec2( cos(c_TWO_PI * (indexA * indexB)/360), sin(c_TWO_PI * (indexB * indexA)/360));
	local_Sampling.RANDOMIZATION_OFFSET = indexB * indexA;
}

//
vec2 GetSamplingPoint(int i)
{
	return reflect(u_SamplingPattern[(i * 97 + local_Sampling.RANDOMIZATION_OFFSET) % u_SamplingPattern.length()], local_Sampling.RANDOMIZATION_VECTOR);
}

//
int GetSamplingIndex(int i, int limit)
{
	return i * 97 + local_Sampling.RANDOMIZATION_OFFSET % limit;
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
vec4 GetNormalDepth (vec2 param)
{
	vec4 result = imageLoad(u_NormalDepth, ivec2(param * imageSize(u_NormalDepth)));
	return result * 2 - 1;
}

/*
 * screen space radius of sphere of influence (projection of its size in range -1, 1)
 */
vec2 ComputeOccludedRadiusProjection(float camera_space_dist)
{	
	//for constant projection this is important just for determining aspect ratio
	vec2 projection = Reproject(projection_transform, vec4(OCCLUDER_MAX_DISTANCE, OCCLUDER_MAX_DISTANCE, camera_space_dist, 1)).xy * 0.5;

	if(USE_CONSTANT_OCCLUDER_PROJECTION)
		return projection * PROJECTED_OCCLUDER_DISTANCE_MAX_SIZE / (local_Target.Size.x * projection.x);
	else
		return clamp(projection,
			vec2(PROJECTED_OCCLUDER_DISTANCE_MIN_SIZE / local_Target.Size.x),
	  	vec2(PROJECTED_OCCLUDER_DISTANCE_MAX_SIZE / local_Target.Size.x));
}

/*
 * compute number of samples needed (step size)
 */
int ComputeStepFromOccludedScreenSize(vec2 rf)
{
	int samplesCount = clamp(u_SamplesCount, 1, c_MaxSamplesCount);
	
	// compute projection screen size
	rf *= local_Target.Size;

	// compute number of samples needed (step size)
	float ssize = samplesCount * clamp(MINIMAL_SAMPLES_COUNT_RATIO, 0, 1);
	float msize = samplesCount * (1 - clamp(MINIMAL_SAMPLES_COUNT_RATIO, 0, 1));

	float min_dist_squared = pow(PROJECTED_OCCLUDER_DISTANCE_MIN_SIZE, 2);
	float max_dist_squared = pow(PROJECTED_OCCLUDER_DISTANCE_MAX_SIZE, 2);
	float rf_squared = pow(max(rf.x, rf.y), 2);

	// linear interpolation between (msize + ssize) and ssize, parameter is squared projection size
	float samples_cnt =
		(msize / (max_dist_squared - min_dist_squared)) *
		(rf_squared - min_dist_squared) +  ssize;

 	float step = clamp(samplesCount / samples_cnt, 1, samplesCount);
 	return int(step);
}

void InitGlobals()
{
	InitSampling();
	
	local_Target.Param = vec2(gl_GlobalInvocationID)/imageSize(u_Target);
	local_Target.Size = vec2(imageSize(u_Target));
	
	vec4 p_nd = GetNormalDepth (local_Target.Param);
	vec4 p_clip = GetClipCoord (local_Target.Param, p_nd.w);
	vec4 p_pos = Reproject (modelviewprojection_inv_transform, p_clip);
	vec4 p_campos = Reproject (projection_inv_transform, p_clip);

  local_Rf =	ComputeOccludedRadiusProjection( p_campos.z );
  local_P.CamPosition = p_campos;
  local_P.N = normalize(p_nd.xyz);
  local_P.Position = p_pos;
  
  if(gl_LocalInvocationID.xy == uvec2(0,0))
	  s_Rf = local_Rf;
}

/*
 * 
 */
void ComputeOccluders()
{
	int samplesCount = clamp(u_SamplesCount, 1, c_MaxSamplesCount);
	int occCount = clamp(samplesCount * c_WorkGroupSize, 1, c_MaxOccludersCount);
	int occLocal = occCount / c_WorkGroupSize;
	int start = int(gl_LocalInvocationIndex) * occLocal;
		
	// for each sample compute occlussion estimation and add it to result
	//for(int i = start; i < occCount; i += c_WorkGroupSize)
	for(int i = start; i < start + occLocal; i++)
	{
		vec2 oc_param = local_Target.Param + normalize(GetSamplingPoint(i)) * s_Rf * i / occLocal;
		vec4 o_nd = GetNormalDepth (oc_param);
		vec4 o_clip = GetClipCoord (oc_param, o_nd.w);
		vec4 o_pos = Reproject ( modelviewprojection_inv_transform, o_clip);
		float o_r =  Reproject ( projection_inv_transform, vec4(2.0 / local_Target.Size.x, 0, o_nd.w, 1)).x;

		//correction to prevent occlusion from itself or from neighbours which are on the same tangent plane
		o_pos -= o_r * vec4(o_nd.xyz, 0);
		s_Occluders[i] = Occluder(o_pos.xyz, o_r);
	}
}

/*
 * 
 */
void ComputeSsao()
{
	float result = 0;
	int samplesCount = clamp(u_SamplesCount, 1, c_MaxSamplesCount);
	int occCount = clamp(u_SamplesCount * c_WorkGroupSize, 1, c_MaxOccludersCount);
	int occLocal = occCount / c_WorkGroupSize;
	int step = ComputeStepFromOccludedScreenSize(local_Rf);
	int i = int(gl_LocalInvocationIndex) * occLocal;
	int si = 0;
	
	// for each sample compute occlussion estimation and add it to result
	for(; si < samplesCount; i++, si += step)
	{
		i = i >= int(gl_LocalInvocationIndex + 1) * occLocal? (i + 1 + 10 * occLocal) % occCount: i% occCount;
		
		const Occluder o = s_Occluders[i];
		const Sample p = local_P;

		vec3 opvec = o.Position.xyz - p.Position.xyz;
		float opdist = length (opvec);
		float omega = c_TWO_PI * (1 - cos( asin( clamp(o.Radius / opdist, 0, 1))));
		result +=
			opdist <= OCCLUDER_MAX_DISTANCE ?
				omega * max(dot(opvec, p.N) / opdist, 0):
			//else
				0;
	}

	imageStore(u_Target, ivec2(gl_GlobalInvocationID), vec4(pow(result, BIAS) * STRENGTH));
}

////////////////////////////////////////////////////////////////////////////////
//kernel//
void main ()
{
	InitGlobals();
	barrier();
	ComputeOccluders();
	barrier();
	ComputeSsao();
}
