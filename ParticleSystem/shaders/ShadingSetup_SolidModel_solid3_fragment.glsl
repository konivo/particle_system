#version 440
#pragma include <RenderPassFactory.Shaders.common.include>

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
//uniforms//
/*
 * model-view matrices 
 */
uniform mat4 projection_transform;
uniform mat4 modelviewprojection_transform;
uniform mat4 modelviewprojection_inv_transform;
uniform float particle_brightness;
uniform float smooth_shape_sharpness;

/*
image geometry and material information
*/
uniform sampler2D custom_texture;
uniform sampler2D normaldepth_texture;
uniform sampler2D aoc_texture;
uniform sampler2D shadow_texture;
uniform sampler2D colorramp_texture;
/*
particle attribute textures
*/
uniform sampler2D particle_attribute1_texture;
/*
material settings

0 - normal
1 - color-ramp
2 - attribute1
*/
uniform int material_color_source;

//light settings and mattrices
uniform vec3 ambient = vec3(.1, .1, .1);
uniform mat4 light_modelviewprojection_transform;
uniform mat4 light_modelview_transform;
uniform mat4 light_projection_transform;
uniform mat4 light_projection_inv_transform;
uniform mat4 light_relativeillumination_transform;

/*
for spotlight real dimension
for directional light an spherical angles*/
uniform float light_size;
uniform float light_expmap_level;
uniform float light_expmap_range;
uniform float light_expmap_range_k;
uniform int light_expmap_nsamples;

//soft shadow settings
uniform bool enable_soft_shadow = true;
subroutine uniform GetShadow u_GetShadow;

////////////////////////////////////////////////////////////////////////////////
//common constants//
const float EXP_SCALE_FACTOR = 50;
const Light light = Light( vec3(0, 0, 0), vec3(-1, -1, -1));

////////////////////////////////////////////////////////////////////////////////
//inputs and outputs//

in VertexData
{
	vec2 param;
};

out vec4 color_luma;


////////////////////////////////////////////////////////////////////////////////
//shadow implementations//
//
vec4 get_normal_depth (vec2 param)
{
	vec4 result = texture(normaldepth_texture, param);
	result = result * 2 - 1;

	return result;
}

/////////////////////
subroutine(GetShadow)
float GetShadowNoFilter(vec4 pos)
{
	vec3 r_pos = (reproject(light_modelviewprojection_transform, pos).xyz + 1) * 0.5;
	return r_pos.z - 0.0001 > texture(shadow_texture, r_pos.xy).x ? 1: 0;
}

/////////////////////
subroutine(GetShadow)
float GetShadowFilter4x4(vec4 pos)
{
	vec3 r_pos = (reproject(light_modelviewprojection_transform, pos).xyz + 1) * 0.5;
	vec4 comp = r_pos.zzzz - 0.001;
	vec4 acc = vec4(0);
	acc += vec4(greaterThan(comp, textureGatherOffset(shadow_texture, r_pos.xy, ivec2(-1, -1), 0) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(shadow_texture, r_pos.xy, ivec2(1, -1), 0) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(shadow_texture, r_pos.xy, ivec2(-1, 1), 0) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(shadow_texture, r_pos.xy, ivec2(1, 1), 0) ));

	return dot(acc, vec4(1/16.0));
}

/////////////////////
subroutine(GetShadow)
float GetShadowSoft1(vec4 pos)
{
	vec3 r_pos = (reproject(light_modelviewprojection_transform, pos).xyz + 1) * 0.5;
	vec3 l_pos = (light_modelview_transform * pos).xyz;
	vec4 avg_depth = vec4(0);
	const int avg_depth_gcount = 2;
	vec4 avg_depth_count = vec4(0);
	float phi = light_size;
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
							shadow_texture, r_pos.xy + get_sampling_point(i) * c2)) / EXP_SCALE_FACTOR + 1 + .0001,
					0, 1);
			
			avg_depth += (smp) * vec4(lessThan(smp, vec4(r_pos.z)));
			avg_depth_count += vec4(dot(vec4(lessThan(smp, vec4(r_pos.z))), vec4(1)));
		}
	
		// Find an ocludder's distance in light coordinate frame
		vec3 o_pos = 
			reproject(
				inverse(light_projection_transform), 
				get_clip_coordinates(r_pos.xy, 2 * dot(avg_depth, 1./avg_depth_count) - 1)
			).xyz;
		// Compute a distance (for now just by means of z difference)
		float o_dist = clamp(o_pos.z - l_pos.z, 0, 1000);
		//
		vec3 rr_pos = 
			reproject(
				light_projection_transform, 
				vec4(l_pos.xyz + vec3(0, tan(phi) * o_dist, 0), 1)).xyz * 0.5 + 0.5;
		// Get a c2's constant correction
		c2corr = pow(length(rr_pos.xy - r_pos.xy), 1);
	}
	
	float v1 = 
		textureLod(
			shadow_texture, r_pos.xy, 0).x * exp(EXP_SCALE_FACTOR * (1 - r_pos.z));	
	for(int i = 0; i < light_expmap_nsamples; i++)
	{
		float f =  			
			textureLod(
				shadow_texture, 
				r_pos.xy + get_sampling_point(i) * clamp(c2corr, 0., 0.01), 0).x * exp(EXP_SCALE_FACTOR * (1 - r_pos.z));
				
		//v1 += pow(f, 1);
		v1 += clamp(f, 0, 1) > 0.99621 ? 1: 0.10421;
		//v1 *= clamp(f, 0, 1) > 0.621 ? 1: 0.8921;
	}
	v1 /= light_expmap_nsamples;
	return clamp(1 - v1, 0, 1);
}

/////////////////////
subroutine(GetShadow)
float GetShadowSoft2(vec4 pos)
{
	vec3 r_pos = (reproject(light_modelviewprojection_transform, pos).xyz + 1) * 0.5;
	vec4 l_pos = light_modelview_transform * pos;

	float phi = light_size;
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
			vec2 smp_pos = r_pos.xy + get_sampling_point(i) * i * c2 /avg_depth_gcount;
			vec4 smp = 
				clamp(
					log(
						textureGather(shadow_texture, smp_pos)) / EXP_SCALE_FACTOR + 1 + .0001,
					0, 1);
			
			avg_depth += (smp) * vec4(lessThan(smp, vec4(r_pos.z)));
			avg_depth_count += vec4(dot(vec4(lessThan(smp, vec4(r_pos.z))), vec4(1)));
		}		
		o_depth = dot(avg_depth, 1./avg_depth_count);*/
		
		// Variant of occluder depth estimation with a computation of minimal depth of occluders
		for(int i = 0; i < o_SampleCount; i ++)
		{
			vec2 smp_pos = r_pos.xy + get_sampling_point(i + citer * o_SampleCount) * i * c2 /o_SampleCount;
			vec4 smp = 
				clamp(
					log(
						textureGather(shadow_texture, smp_pos)) / EXP_SCALE_FACTOR + 1 + .0001,
					0, 1);
			
			o_Depth = min(o_Depth, min(smp.x, min(smp.y, min(smp.z, min(smp.w, r_pos.z)))));
		}
	
		// Find an ocludder's distance in light coordinate frame
		vec3 o_pos = 
			reproject(
				inverse(light_projection_transform), 
				get_clip_coordinates(r_pos.xy, 2 * o_Depth - 1)
			).xyz;
		// Compute a distance (for now just by means of z difference)
		float o_dist = clamp(o_pos.z - l_pos.z, 0, 1000);
		//
		vec3 rr_pos = 
			reproject(
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
	for(int i = 0; i < light_expmap_nsamples; i++)
	{
		// We need randomly distributed but evenly distributed points thus we do not use
		// the sampling point directly
		vec2 rtest_pos = r_pos.xy + get_sampling_point(i) * i * c2/light_expmap_nsamples;
		// Extract a value from depth map
		float smp = 
			clamp(
				log(
					textureLod(
						shadow_texture, rtest_pos, 0)) / EXP_SCALE_FACTOR + 1 + .00041,
				0, 1);
		
		if(smp < r_pos.z)
		{
			result += 1;
		}
	}
	
	float result2 = 0.0;
	vec3 ldir = normalize(light.dir);
	vec3 ldiro1 = normalize(cross(ldir, vec3(1, 0, 1)));
	vec3 ldiro2 = normalize(cross(ldir, ldiro1));
	float est = 3;
	float step = .91;
	
	bool estFound = false;
	
	for(int i = 0; i < 1*light_expmap_nsamples; i++)
	{
		vec2 offset = get_sampling_point(i*1 + 113) * tan(phi);/// 500;
		vec3 rdir = ldir + ldiro1 * offset.x + ldiro2 * offset.y;
		vec3 nldir = normalize(rdir);
		
		//est = max(est, 0.3);
		//step = max(.91, step);
		float estbias = i % 2;
		
		for(float j = 0; j < 5; j++)
		{			
			vec4 ppp = vec4(nldir * (j * step * i  + est + estbias) + pos.xyz, 1);
			vec2 p_param = reproject(modelviewprojection_transform, ppp).xy * 0.5 + 0.5;
			
			//vec2 p_param = param + offset;
			vec4 p_nd = get_normal_depth(clamp(p_param, 0, 1));
			vec4 p_clip = get_clip_coordinates(p_param, p_nd.w);
			vec4 p_pos = reproject(modelviewprojection_inv_transform, p_clip);
			
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
				vec2 p_param = reproject(modelviewprojection_transform, ppp).xy * 0.5 + 0.5;
				
				//vec2 p_param = param + offset;
				vec4 p_nd = get_normal_depth(clamp(p_param, 0, 1));
				vec4 p_clip = get_clip_coordinates(p_param, p_nd.w);
				vec4 p_pos = reproject(modelviewprojection_inv_transform, p_clip);
				
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
			
			if(estFound && j == 4)
			{
				estFound = false;
			}
		}
	}
	
	//return result/ light_expmap_nsamples;
	return clamp(max(result2, result)/light_expmap_nsamples, 0, 1);
}


////////////////////////////////////////////////////////////////////////////////
//material implementation//
vec4 get_material(vec4 pos, vec4 normaldepth)
{
	vec4 material;

	switch(material_color_source)
	{
		case 0:
			material = (normaldepth + 1) * 0.5f;
			break;
		case 1:
			vec2 cparam = 2 * (texture(particle_attribute1_texture, param).xy - 0.5f);
			material = 0.5f * texture(colorramp_texture, cparam) + 0.5f;
			break;
		case 2:
			material = texture(particle_attribute1_texture, param) * 0.5f + 0.5f;
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
	init_sampling();

	vec4 p_nd = get_normal_depth(param);
	vec4 p_clip = get_clip_coordinates(param, p_nd.w);
	vec4 p_pos = reproject(modelviewprojection_inv_transform, p_clip);

	if(p_nd.w > 0.99999)
		discard;

	float aoc = texture(aoc_texture, param).x;
	vec3 material = get_material(p_pos, p_nd).xyz;
	float shadow = u_GetShadow(p_pos);

	float luminance = 0.3 * material.r + 0.5 * material.g + 0.2 * material.b;
	vec3 ambientmat =  0.5 * (material + normalize(vec3(1, 1, 1)) * dot(material, normalize(vec3(1, 1, 1))));

	vec3 diffuse = material * max(/*dot(light.dir, p_nd.xyz) */ (1 - shadow), 0);
	vec3 color = (diffuse  + ambient * ambientmat) * (1 - aoc);

	color_luma = vec4(color, sqrt(dot(color.rgb, vec3(0.299, 0.587, 0.114))));
	//color_luma = vec4(p_nd.xyz * 0.5 + 0.5, sqrt(dot(p_nd.rgb* 0.5 + 0.5, vec3(0.299, 0.587, 0.114))));
	//color_luma = vec4(pow(texture(shadow_texture, param).x, 1), 0, 0, 1);
	//color_luma = vec4((reproject(light_modelviewprojection_transform, p_pos).xy + 1) * 0.5, 0, 1);
	//color_luma = vec4(shadow, 0, 0, 1);
	gl_FragDepth = texture(normaldepth_texture, param).w;
}
