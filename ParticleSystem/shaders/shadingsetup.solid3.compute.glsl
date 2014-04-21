/*
 *  Expected declarations in the prepended code
 */
//#version 440
//layout(local_size_x= ... , local_size_y= ...) in;
//#define T_LAYOUT_OUT_DEPTH {0}
//#define T_LAYOUT_OUT_COLORLUMA {1}
//#line 1

////////////////////////////////////////////////////////////////////////////////
//types//
//
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

subroutine float GetShadow(vec4 pos);

////////////////////////////////////////////////////////////////////////////////
//constants//
const int c_WorkGroupSize = int(gl_WorkGroupSize.x * gl_WorkGroupSize.y);
const int c_OccludersRimSize = 8;
const int c_OccludersGroupSizeX = int(gl_WorkGroupSize.x + c_OccludersRimSize * 2);
const int c_OccludersGroupSizeY = int(gl_WorkGroupSize.y + c_OccludersRimSize * 2);
const int c_OccludersGroupSize = c_OccludersGroupSizeX * c_OccludersGroupSizeY;
const int c_MaxLocalOccluders = 2;
const int c_MaxOccludersCount = c_MaxLocalOccluders * c_OccludersGroupSize;
const int c_MaxSamplesCount = min(256, c_MaxOccludersCount);
const float c_PI = 3.141592654f;
const float c_TWO_PI = 2 * 3.141592654f;
const float c_EXP_SCALE_FACTOR = 50;
const Light c_DefaultLight = Light( vec3(0, 0, 0), vec3(-1, -1, -1));

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
uniform mat4 projection_transform;
uniform mat4 projection_inv_transform;

/*
 * other
 */
//uniform float particle_brightness;
//uniform float smooth_shape_sharpness;

/*
 * image geometry and material information
 */
uniform sampler2D u_CustomTexture;
uniform sampler2D u_NormalDepthTexture;
uniform sampler2D u_SsaoTexture;
uniform sampler2D u_ShadowTexture;
uniform sampler2D u_ColorRampTexture;

/*
 * particle attribute textures
 */
uniform sampler2D u_ParticleAttribute1Texture;

/* 
 * material settings
 * 0 - normal
 * 1 - color-ramp
 * 2 - attribute1
 */
uniform int u_MaterialColorSource;

/*
 * light settings and mattrices
 */
uniform mat4 light_modelviewprojection_transform;
uniform mat4 light_modelview_transform;
uniform mat4 light_projection_transform;
uniform mat4 light_projection_inv_transform;
uniform mat4 light_relativeillumination_transform;

/*
 * for spotlight real dimension
 * for directional light an spherical angles
 */
uniform Light u_Light = c_DefaultLight;
uniform vec3 u_LightAmbientColor = vec3(.1, .1, .1);
uniform float u_LightSize;

/*
 * soft shadow settings
 */
uniform int u_ShadowSampleCount;
subroutine uniform GetShadow u_GetShadow;


////////////////////////////////////////////////////////////////////////////////
//shared storage//
shared vec4[c_MaxOccludersCount] s_Occluders;
shared Range[c_WorkGroupSize] s_OccRanges;

////////////////////////////////////////////////////////////////////////////////
//local variables//

struct { vec2 Size; vec2 Param; vec2 GroupParam; } 
	local_Target;

struct { 	vec2 RANDOMIZATION_VECTOR; 	int RANDOMIZATION_OFFSET; }
	local_Sampling;

////////////////////////////////////////////////////////////////////////////////
//utility and library functions//
//
//
void InitSampling()
{
	int indexA = int(gl_GlobalInvocationID.x) *  173547 + int(gl_GlobalInvocationID.y) * 364525 + 1013904223;
	indexA = (indexA >> 4) & 0xFFF;

	int indexB = int(gl_GlobalInvocationID.x) *  472541 + int(gl_GlobalInvocationID.y) * 198791 + 2103477191;
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
	return (i * 97 + local_Sampling.RANDOMIZATION_OFFSET) % limit;
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
	vec4 result = texture(u_NormalDepthTexture, param);
	result = result * 2 - 1;

	return result;
}

void InitGlobals()
{
	InitSampling();
	
	local_Target.Param = vec2(gl_GlobalInvocationID)/imageSize(u_TargetColorLuma);
	local_Target.Size = vec2(imageSize(u_TargetColorLuma));
}

////////////////////////////////////////////////////////////////////////////////
//shadow implementations//

/////////////////////
subroutine(GetShadow)
float GetShadowNoFilter(vec4 pos)
{
	vec3 r_pos = (Reproject(light_modelviewprojection_transform, pos).xyz + 1) * 0.5;
	return r_pos.z - 0.0001 > texture(u_ShadowTexture, r_pos.xy).x ? 1: 0;
}

/////////////////////
subroutine(GetShadow)
float GetShadowFilter4x4(vec4 pos)
{
	vec3 r_pos = (Reproject(light_modelviewprojection_transform, pos).xyz + 1) * 0.5;
	vec4 comp = r_pos.zzzz - 0.001;
	vec4 acc = vec4(0);
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(-1, -1), 0) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(1, -1), 0) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(-1, 1), 0) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(1, 1), 0) ));

	return dot(acc, vec4(1/16.0));
}

/////////////////////
subroutine(GetShadow)
float GetShadowSoft1(vec4 pos)
{
	vec3 r_pos = (Reproject(light_modelviewprojection_transform, pos).xyz + 1) * 0.5;
	vec3 l_pos = (light_modelview_transform * pos).xyz;
	vec4 avg_depth = vec4(0);
	const int avg_depth_gcount = 2;
	vec4 avg_depth_count = vec4(0);
	float phi = u_LightSize;
	float c2 = 1;
	float c2corr = 0.1;
	int stepcount = 0;

	// Loop for finding a good size of kernel for the depthmap filtering
	while(abs(c2 - c2corr) > 0.00001 && stepcount < 5)
	{
		stepcount++;
		c2 = c2corr;
		for(int i = 0; i < avg_depth_gcount; i ++)
		{
			vec4 smp = 
				clamp(
					log(
						textureGather(
							u_ShadowTexture, r_pos.xy + GetSamplingPoint(i) * c2)) / c_EXP_SCALE_FACTOR + 1 + .0001,
					0, 1);
			
			avg_depth += (smp) * vec4(lessThan(smp, vec4(r_pos.z)));
			avg_depth_count += vec4(dot(vec4(lessThan(smp, vec4(r_pos.z))), vec4(1)));
		}
	
		// Find an ocludder's distance in light coordinate frame
		vec3 o_pos = 
			Reproject(
				inverse(light_projection_transform), 
				GetClipCoord(r_pos.xy, 2 * dot(avg_depth, 1./avg_depth_count) - 1)
			).xyz;
		// Compute a distance (for now just by means of z difference)
		float o_dist = clamp(o_pos.z - l_pos.z, 0, 1000);
		//
		vec3 rr_pos = 
			Reproject(
				light_projection_transform, 
				vec4(l_pos.xyz + vec3(0, tan(phi) * o_dist, 0), 1)).xyz * 0.5 + 0.5;
		// Get a c2's constant correction
		c2corr = pow(length(rr_pos.xy - r_pos.xy), 1);
	}
	
	float v1 = 
		textureLod(
			u_ShadowTexture, r_pos.xy, 0).x * exp(c_EXP_SCALE_FACTOR * (1 - r_pos.z));	
	for(int i = 0; i < u_ShadowSampleCount; i++)
	{
		float f =  			
			textureLod(
				u_ShadowTexture, 
				r_pos.xy + GetSamplingPoint(i) * clamp(c2corr, 0., 0.01), 0).x * exp(c_EXP_SCALE_FACTOR * (1 - r_pos.z));
				
		//v1 += pow(f, 1);
		v1 += clamp(f, 0, 1) > 0.99621 ? 1: 0.10421;
		//v1 *= clamp(f, 0, 1) > 0.621 ? 1: 0.8921;
	}
	v1 /= u_ShadowSampleCount;
	return clamp(1 - v1, 0, 1);
}

/////////////////////
float est = .2;
float step = 1;
float stepmult = 1;
float estbias = 0;
bool estFound = false;

vec4 FindOccluder(vec4 pos, vec4 defaultpos, vec3 nldir, vec3 ldir)
{
	vec4 result = defaultpos;
	
	for(float j = 0; j < 20; j++)
	{			
		vec4 ppp = vec4(nldir * (j * step * stepmult  + est + estbias) + pos.xyz, 1);
		vec2 p_param = Reproject(modelviewprojection_transform, ppp).xy * 0.5 + 0.5;
		vec4 p_nd = GetNormalDepth(clamp(p_param, 0, 1));
		vec4 p_clip = GetClipCoord(p_param, p_nd.w);
		vec4 p_pos = Reproject(modelviewprojection_inv_transform, p_clip);
		
		if(length(ppp - p_pos) < 0.06361 * length(ppp - pos))
		{
			est = (j * step * stepmult + est + estbias) ;
			step = 0.05 * est;
			est *= 0.9;
			estbias = 0;
			stepmult = 1;
			
			if(estFound)
			{
				result = p_pos;
				break;
			}
			else
			{
				j = 1;
				estFound = true;
			}
		}
		else if(estFound)
		{
			vec4 ppp = vec4(nldir * (-j * step * stepmult  + est + estbias) + pos.xyz, 1);
			vec2 p_param = Reproject(modelviewprojection_transform, ppp).xy * 0.5 + 0.5;
			vec4 p_nd = GetNormalDepth(clamp(p_param, 0, 1));
			vec4 p_clip = GetClipCoord(p_param, p_nd.w);
			vec4 p_pos = Reproject(modelviewprojection_inv_transform, p_clip);
			
			if(length(ppp - p_pos) < 0.06 * length(ppp - pos))
			{
				est = (-j * step * stepmult + est + estbias) ;
				step = 0.05 * est;
				est *= 0.9;
				estbias = 0;
				stepmult = 1;
				
				if(estFound)
				{
					result = p_pos;
					break;
				}
				else
				{
					j = 1;
					estFound = true;
				}
			}
		}
	}
	
	return result;
}

subroutine(GetShadow)
float GetShadowSoft2(vec4 pos)
{
	int samplesCount = clamp(u_ShadowSampleCount, 1, c_MaxSamplesCount);
	int occCount = clamp(samplesCount * c_OccludersGroupSize, 1, c_MaxOccludersCount);
	int occLocal = occCount / c_OccludersGroupSize;
	
	float phi = u_LightSize;
	vec3 ldir = normalize(c_DefaultLight.dir);
	vec3 ldiro1 = normalize(cross(ldir, vec3(1, 0, 1)));
	vec3 ldiro2 = normalize(cross(ldir, ldiro1));
	
	
	// range estimate
	/*Range range = {-100000, 100000};
	Range rangeInvalid = {100000, -100000};
	s_OccRanges[gl_LocalInvocationIndex] = rangeInvalid;
	for(int i = 0; i < 5; i++)
	{
		vec2 offset = GetSamplingPoint(i * 3 + 1132) * tan(phi);
		vec3 rdir = ldir + ldiro1 * offset.x + ldiro2 * offset.y;
		vec3 nldir = normalize(rdir);	
			
		vec4 p_p1 = Reproject(modelviewprojection_transform, vec4(pos.xyz, 1));
		vec4 p_p2 = Reproject(modelviewprojection_transform, vec4(nldir * 100 + pos.xyz, 1));
		ivec2 p_size = textureSize(u_NormalDepthTexture, 0);
		vec4 p_delta = normalize(p_p2 - p_p1)/max(p_size.x, p_size.y);
		
		for(float j = 0; j < 225; j++)
		{
			vec4 p_param = p_p1 + p_delta * (j * step * stepmult + est + estbias);
			vec4 ppp = Reproject(modelviewprojection_inv_transform, p_param);		
			vec4 p_nd = GetNormalDepth(clamp(p_param.xy * 0.5 + 0.5, 0, 1));
			vec4 p_clip = GetClipCoord(p_param.xy * 0.5 + 0.5, p_nd.w);
			vec4 p_pos = Reproject(modelviewprojection_inv_transform, p_clip);
			
			if(length(ppp - p_pos) < 0.0191 * length(ppp - pos))
			{
				est = (j * step * stepmult + est + estbias);
				step = 0.01 * est;
				est *= 0.9;
				estbias = 0;
				stepmult = 1;
				
				range.Min = max(est, range.Min);
				range.Max = min(est, range.Max);
				s_OccRanges[gl_LocalInvocationIndex] = range;
				break;
			}
		}
	}
	
	
	barrier();
	for(int i = 0; i < c_WorkGroupSize; i++)
	{
		range.Min = min(s_OccRanges[i].Min, range.Min);
		range.Max = max(s_OccRanges[i].Max, range.Max);
	}
	
	est = range.Min * 0.958;
	step = 0.0151 * (range.Max * 1.2 - est);
	stepmult = 1;
	estbias = 0;
	*/
	// finding of occluders
	/*for(int i = 0; i < occLocal; i++)
	{
		s_Occluders[i + gl_LocalInvocationIndex * occLocal] = pos - 10*vec4(ldir, 0);
		vec2 offset = GetSamplingPoint(i*3 + 1132) * tan(phi);
		vec3 rdir = ldir + ldiro1 * offset.x + ldiro2 * offset.y;
		vec3 nldir = normalize(rdir);
		
		vec4 p_p1 = Reproject(modelviewprojection_transform, vec4(pos.xyz, 1));
		vec4 p_p2 = Reproject(modelviewprojection_transform, vec4(nldir * 100 + pos.xyz, 1));
		ivec2 p_size = textureSize(u_NormalDepthTexture, 0);
		vec4 p_delta = normalize(p_p2 - p_p1)/max(p_size.x, p_size.y);
		
		for(float j = 0; j < 200; j++)
		{
			vec4 p_param = p_p1 + p_delta * (j * step * stepmult + est + estbias);
			vec4 ppp = Reproject(modelviewprojection_inv_transform, p_param);
			//vec4 ppp = vec4(nldir * (j * step * stepmult  + est + estbias) + pos.xyz, 1);
			
			vec4 p_nd = GetNormalDepth(clamp(p_param.xy * 0.5 + 0.5, 0, 1));
			vec4 p_clip = GetClipCoord(p_param.xy * 0.5 + 0.5, p_nd.w);
			vec4 p_pos = Reproject(modelviewprojection_inv_transform, p_clip);
			
			if(length(ppp - p_pos) < 0.029312821 * length(ppp - pos))
			{
				est = (j * step * stepmult + est + estbias);
				step = 0.051 * est;
				est *= 0.9;
				s_Occluders[i + gl_LocalInvocationIndex * occLocal] = p_pos;
				break;
			}
		}
	}*/
	for(int start = int(gl_LocalInvocationIndex); start < c_OccludersGroupSize; start += c_WorkGroupSize)
	{
		estbias = 0;
		est = .1;
		step = .1;
		stepmult = 1;
		estFound = false;
		for(int i = 0; i < occLocal; i++)
		{
			vec2 pos_index = vec2(start % c_OccludersGroupSizeX, start / c_OccludersGroupSizeX) + gl_GlobalInvocationID.xy - gl_LocalInvocationID.xy - vec2(c_OccludersRimSize);
			vec2 pos_param = pos_index/local_Target.Size;
			
			vec4 p_nd = GetNormalDepth(pos_param);
			vec4 p_clip = GetClipCoord(pos_param, p_nd.w);
			vec4 p_pos = Reproject(modelviewprojection_inv_transform, p_clip);
		
			vec4 defpos = p_pos - 10*vec4(ldir, 0);
			vec2 offset = GetSamplingPoint(i + start + 113) * tan(phi);
			vec3 nldir = normalize(ldir + ldiro1 * offset.x + ldiro2 * offset.y);
			
			if(!estFound)
			{
				estbias = i % 12;
				est = .1;
				step = 1;
				stepmult = i + 1;
			}
			
			s_Occluders[i + start * occLocal] =
				FindOccluder(p_pos, defpos, nldir, ldir);
		}
	}
	
	barrier();
	
	int locId = int(c_OccludersGroupSizeX * (gl_LocalInvocationID.y + c_OccludersRimSize) + gl_LocalInvocationID.x + c_OccludersRimSize);
	int sf = 0;
	float result3 = 0.0;
	for(int i = 0; i < occLocal && sf < samplesCount; i++)
	{
		int index = locId * occLocal + i;
		
		vec4 occ = s_Occluders[index % occCount];
		vec4 d = occ - pos;
		float s = dot(normalize(d.xyz), ldir);
		
		if(s > cos(phi))
		{
			result3 += 1;
		}
		
		if(abs(s) > cos(phi))
		{
			sf++;
		}
	}
	
	ivec2[] cursors = {	{-1, 0}, {0, 1},	{1, 0},	{0, -1}	};
	int round = 2;
	int roundCompl = 0;
	for(int i = 0; i < 20 && sf < samplesCount; i++)
	{
		for(int j = 0; j < 4; j++)
		{
			ivec2 cursor = cursors[j];
			
			for(int k = 0; k < occLocal; k++)
			{
				int index = int(locId + cursor.x + cursor.y * c_OccludersGroupSizeX) * occLocal + k;
				
				vec4 occ = s_Occluders[index % occCount];
				vec4 d = occ - pos;
				float s = dot(normalize(d.xyz), ldir);
				
				if(s > cos(phi))
				{
					result3 += 1.5;
				}
				
				if(abs(s) > cos(phi))
				{
					sf++;
				}
			}
		}
		
		roundCompl = ++roundCompl % round;		
		if(roundCompl == 0)
		{
			cursors[0] += ivec2(-1, - round - 1);
			cursors[1] += ivec2(- round - 1, 1);
			cursors[2] += ivec2(1, round + 1);
			cursors[3] += ivec2(- round - 1, -1);
			
			round += 2;
		}
	}
	
	return clamp(result3/sf, 0, 1);
}

/////////////////////
subroutine(GetShadow)
float GetShadowSoft2_v0(vec4 pos)
{
	int samplesCount = clamp(u_ShadowSampleCount, 1, c_MaxSamplesCount);	
	float phi = u_LightSize;
	float result2 = 0.0;
	vec3 ldir = normalize(c_DefaultLight.dir);
	vec3 ldiro1 = normalize(cross(ldir, vec3(1, 0, 1)));
	vec3 ldiro2 = normalize(cross(ldir, ldiro1));
	float est = 10;	
	float step = 1;	
	bool estFound = false;
	float stepmult = 1;
	float estbias = 0;
	
	for(int i = 0; i < samplesCount; i++)
	{
		vec2 offset = GetSamplingPoint(i*1 + 113) * tan(phi);/// 500;
		vec3 rdir = ldir + ldiro1 * offset.x + ldiro2 * offset.y;
		vec3 nldir = normalize(rdir);
		
		//est = max(est, 0.3);
		//step = max(.91, step);
		float estbias = i % 2;
		
		for(float j = 0; j < 10; j++)
		{			
			vec4 ppp = vec4(nldir * (j * step * i  + est + estbias) + pos.xyz, 1);
			vec2 p_param = Reproject(modelviewprojection_transform, ppp).xy * 0.5 + 0.5;
			
			//vec2 p_param = param + offset;
			vec4 p_nd = GetNormalDepth(clamp(p_param, 0, 1));
			vec4 p_clip = GetClipCoord(p_param, p_nd.w);
			vec4 p_pos = Reproject(modelviewprojection_inv_transform, p_clip);
			
			vec4 d = p_pos - pos;
			//vec4 d = vec4(p_nd.xyz, 0);
			float s = dot(normalize(d.xyz), ldir);
			
			if(length(ppp - p_pos) < 0.1 * length(ppp - pos))
			{
				est = (j * step * i + est + estbias) ;
				step = 0.1 * est / ((i + 1) * 1);
				est *= 0.9;
				estbias = 0;
				
				if(estFound)
				{
					result2 += 1;
					break;
				}
				else
				{
					j = 0;
					estFound = true;
				}
			}
			else if(estFound)
			{
				vec4 ppp = vec4(nldir * (-j * step * i  + est + estbias) + pos.xyz, 1);
				vec2 p_param = Reproject(modelviewprojection_transform, ppp).xy * 0.5 + 0.5;
				
				//vec2 p_param = param + offset;
				vec4 p_nd = GetNormalDepth(clamp(p_param, 0, 1));
				vec4 p_clip = GetClipCoord(p_param, p_nd.w);
				vec4 p_pos = Reproject(modelviewprojection_inv_transform, p_clip);
				
				vec4 d = p_pos - pos;
				//vec4 d = vec4(p_nd.xyz, 0);
				float s = dot(normalize(d.xyz), ldir);
				
				if(length(ppp - p_pos) < 0.1 * length(ppp - pos))
				{
					est = (-j * step * i + est + estbias) ;
					step = 0.1 * est / ((i + 1) * 1);
					est *= 0.9;
					estbias = 0;
					
					if(estFound)
					{
						result2 += 1;
						break;
					}
					else
					{
						j = 0;
						estFound = true;
					}
				}
			}
			
			if(estFound && j == 49)
			{
				estFound = false;
			}
		}
	}
	
	//return result/ light_expmap_nsamples;
	return clamp(max(result2, 0)/samplesCount, 0, 1);
}

////////////////////////////////////////////////////////////////////////////////
//material implementation//
vec4 GetMaterial(vec4 pos, vec4 normaldepth)
{
	vec4 material;

	switch(u_MaterialColorSource)
	{
		case 0:
			material = (normaldepth + 1) * 0.5f;
			break;
		case 1:
			vec2 cparam = 2 * (texture(u_ParticleAttribute1Texture, local_Target.Param).xy - 0.5f);
			material = 0.5f * texture(u_ColorRampTexture, cparam) + 0.5f;
			break;
		case 2:
			material = texture(u_ParticleAttribute1Texture, local_Target.Param) * 0.5f + 0.5f;
			break;
		default:
			material = vec4(1, 1, 1, 1);
	}

	material = min( material / material .x, material / material .y);
	material = min( material, material / material .z);
	return material;
}

void main ()
{
	InitGlobals();

	vec4 p_nd = GetNormalDepth(local_Target.Param);
	vec4 p_clip = GetClipCoord(local_Target.Param, p_nd.w);
	vec4 p_pos = Reproject(modelviewprojection_inv_transform, p_clip);

	if(p_nd.w > 0.99999)
	{
		imageStore(u_TargetColorLuma, ivec2(gl_GlobalInvocationID), vec4(0));
		imageStore(u_TargetDepth, ivec2(gl_GlobalInvocationID), vec4(1));
		return;
	}
	
	float aoc = texture(u_SsaoTexture, local_Target.Param).x;
	vec3 material = GetMaterial(p_pos, p_nd).xyz;
	float shadow = u_GetShadow(p_pos);

	float luminance = 0.3 * material.r + 0.5 * material.g + 0.2 * material.b;
	vec3 ambientmat =  0.5 * (material + normalize(vec3(1, 1, 1)) * dot(material, normalize(vec3(1, 1, 1))));

	vec3 diffuse = material * max(/*dot(light.dir, p_nd.xyz) */ (1 - shadow), 0);
	vec3 color = (diffuse  + u_LightAmbientColor * ambientmat) * (1 - aoc);

	imageStore(u_TargetColorLuma, ivec2(gl_GlobalInvocationID), vec4(color, sqrt(dot(color.rgb, vec3(0.299, 0.587, 0.114)))));
	//color_luma = vec4(p_nd.xyz * 0.5 + 0.5, sqrt(dot(p_nd.rgb* 0.5 + 0.5, vec3(0.299, 0.587, 0.114))));
	//color_luma = vec4(pow(texture(u_ShadowTexture, param).x, 1), 0, 0, 1);
	//color_luma = vec4((Reproject(light_modelviewprojection_transform, p_pos).xy + 1) * 0.5, 0, 1);
	//color_luma = vec4(shadow, 0, 0, 1);
	//color_luma = vec4(aoc, 0, 0, 1);
	imageStore(u_TargetDepth, ivec2(gl_GlobalInvocationID), texture(u_NormalDepthTexture, local_Target.Param).wwww);
}
