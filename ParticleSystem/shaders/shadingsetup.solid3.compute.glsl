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

subroutine float GetShadow(vec4 pos);

////////////////////////////////////////////////////////////////////////////////
//constants//
const int c_WorkGroupSize = int(gl_WorkGroupSize.x * gl_WorkGroupSize.y);
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

/*
float GetShadow(
	sampler2D u_ShadowTexture,	//
	mat4 light_transform,			//model-view-projection transform
	vec4 pos									//position in world space
	)
{
	vec3 r_pos = (Reproject(light_transform, pos).xyz + 1) * 0.5;
	vec4 comp = vec4(r_pos.z);	
	vec4 acc = vec4(0);
	
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(-1, -1)) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(1, -1)) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(-1, 1)) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(1, 1)) ));
	
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(-0, -1)) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(2, -1)) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(-0, 1)) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(2, 1)) ));
	
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(-1, 0)) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(1, 2)) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(-1, 0)) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(1, 2)) ));
	
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(0, 0)) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(2, 0)) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(0, 2)) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(u_ShadowTexture, r_pos.xy, ivec2(2, 2)) ));

	return dot(acc, vec4(1/16.0));
}
*/
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
subroutine(GetShadow)
float GetShadowSoft2(vec4 pos)
{
	vec3 r_pos = (Reproject(light_modelviewprojection_transform, pos).xyz + 1) * 0.5;
	vec4 l_pos = light_modelview_transform * pos;

	float phi = u_LightSize;
	// kernel's size to be estimated in texture coordinates <0, 1>
	float c2 = 0.05;
	// initial estimation
	const float o_CorrectionLimit = 0.001;
	const int o_CorrectionsCount = 5;
	// 
	const int o_SampleCount = 8;
	// depths of occluders found during a kernel size estimation
	vec4 o_DepthAvg = vec4(0);
	vec4 o_DepthAvgCount = vec4(0);
	// depth of an occluder for estimation of kernel size
	float o_Depth = 1;

	// Loop for finding a good size of kernel for the depthmap filtering
	for(int citer = 0; citer < o_CorrectionsCount; citer++)
	{
		/*
		// Variant of occluder depth estimation with a computation of average depth of occluders
		for(int i = 0; i < o_SampleCount; i ++)
		{
			vec2 smp_pos = r_pos.xy + GetSamplingPoint(i) * i * c2 /avg_depth_gcount;
			vec4 smp = 
				clamp(
					log(
						textureGather(u_ShadowTexture, smp_pos)) / c_EXP_SCALE_FACTOR + 1 + .0001,
					0, 1);
			
			avg_depth += (smp) * vec4(lessThan(smp, vec4(r_pos.z)));
			avg_depth_count += vec4(dot(vec4(lessThan(smp, vec4(r_pos.z))), vec4(1)));
		}		
		o_depth = dot(avg_depth, 1./avg_depth_count);*/
		
		// Variant of occluder depth estimation with a computation of minimal depth of occluders
		for(int i = 0; i < o_SampleCount; i ++)
		{
			vec2 smp_pos = r_pos.xy + GetSamplingPoint(i + citer * o_SampleCount) * i * c2 /o_SampleCount;
			vec4 smp = 
				clamp(
					log(
						textureGather(u_ShadowTexture, smp_pos)) / c_EXP_SCALE_FACTOR + 1 + .0001,
					0, 1);
			
			o_Depth = min(o_Depth, min(smp.x, min(smp.y, min(smp.z, min(smp.w, r_pos.z)))));
		}
	
		// Find an ocludder's distance in light coordinate frame
		vec3 o_pos = 
			Reproject(
				inverse(light_projection_transform), 
				GetClipCoord(r_pos.xy, 2 * o_Depth - 1)
			).xyz;
		// Compute a distance (for now just by means of z difference)
		float o_dist = clamp(o_pos.z - l_pos.z, 0, 1000);
		//
		vec3 rr_pos = 
			Reproject(
				light_projection_transform, 
				vec4(o_pos.xyz + vec3(0, tan(phi) * o_dist, 0), 1)).xyz * 0.5 + 0.5;
				
		// Get a c2's correction		
		float c2corr = length(rr_pos.xy - r_pos.xy);
		if(abs(c2 - c2corr) < o_CorrectionLimit)
			break;
		
		c2 = c2corr;
	}
	// last correction before processing: it widens the area to be processed
	c2 *= 1.0;
	
	float result = 0.0;
	for(int i = 0; i < u_ShadowSampleCount; i++)
	{
		// We need randomly distributed but evenly distributed points thus we do not use
		// the sampling point directly
		vec2 rtest_pos = r_pos.xy + GetSamplingPoint(i) * i * c2/u_ShadowSampleCount;
		// Extract a value from depth map
		float smp = 
			clamp(
				log(
					textureLod(
						u_ShadowTexture, rtest_pos, 0)) / c_EXP_SCALE_FACTOR + 1 + .00041,
				0, 1);
		
		if(smp < r_pos.z)
		{
			result += 1;
		}
	}
	
	float result2 = 0.0;
	vec3 ldir = normalize(u_Light.dir);
	vec3 ldiro1 = normalize(cross(ldir, vec3(1, 0, 1)));
	vec3 ldiro2 = normalize(cross(ldir, ldiro1));
	float est = 1;
	float step = .9491;
	
	bool estFound = false;
	
	for(int i = 0; i < 1*u_ShadowSampleCount; i++)
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
	
	//return result/ u_ShadowSampleCount;
	return clamp(max(result2, result)/u_ShadowSampleCount, 0, 1);
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
