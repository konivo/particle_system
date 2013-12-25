#version 400
#pragma include <RenderPassFactory.Shaders.common.include>

////////////////////////////////////////////////////////////////////////////////
//types//
//
struct Light
{
	vec3 pos;
	vec3 dir;
};

////////////////////////////////////////////////////////////////////////////////
//uniforms//
/*
 * model-view matrices 
 */
uniform mat4 projection_transform;
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
/*
0 - no filtering
1 - pcf 4x4
2 - exp shadow map
3 - soft shadow with pcf
4 - soft shadow with exp
*/
uniform int shadow_implementation;

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

float get_shadow_no_filter(vec4 pos)
{
	vec3 r_pos = (reproject(light_modelviewprojection_transform, pos).xyz + 1) * 0.5;
	return r_pos.z - 0.01 > texture(shadow_texture, r_pos.xy).x ? 1: 0;
}

float get_shadow_pcf4x4(vec4 pos)
{
	vec3 r_pos = (reproject(light_modelviewprojection_transform, pos).xyz + 1) * 0.5;
	vec4 comp = r_pos.zzzz - 0.01;
	vec4 acc = vec4(0);
	acc += vec4(greaterThan(comp, textureGatherOffset(shadow_texture, r_pos.xy, ivec2(-1, -1), 0) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(shadow_texture, r_pos.xy, ivec2(1, -1), 0) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(shadow_texture, r_pos.xy, ivec2(-1, 1), 0) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(shadow_texture, r_pos.xy, ivec2(1, 1), 0) ));

	return dot(acc, vec4(1/16.0));
}

float get_shadow_soft_pcf4x4(vec4 pos)
{
	vec3 r_pos = (reproject(light_modelviewprojection_transform, pos).xyz + 1) * 0.5;
	vec3 l_pos = (light_modelview_transform * pos).xyz;
	vec4 avg_depth;
	float count = 36;
	float phi = 0.5;

	avg_depth += textureGatherOffset(shadow_texture, r_pos.xy, ivec2(-16, 16));
	avg_depth += textureGatherOffset(shadow_texture, r_pos.xy, ivec2(-12, -12));
	avg_depth += textureGatherOffset(shadow_texture, r_pos.xy, ivec2(8, -8));
	avg_depth += textureGatherOffset(shadow_texture, r_pos.xy, ivec2(-4, 4));
	avg_depth += textureGatherOffset(shadow_texture, r_pos.xy, ivec2(0, 0));
	avg_depth += textureGatherOffset(shadow_texture, r_pos.xy, ivec2(16, -16));
	avg_depth += textureGatherOffset(shadow_texture, r_pos.xy, ivec2(-12, 12));
	avg_depth += textureGatherOffset(shadow_texture, r_pos.xy, ivec2(-8, 8));
	avg_depth += textureGatherOffset(shadow_texture, r_pos.xy, ivec2(4, -4));

	float occ_depth = dot(avg_depth, vec4(1/count)) * 2 - 1;
	float occ_surf_dist = length(reproject(light_projection_inv_transform, get_clip_coordinates(r_pos.xy, occ_depth)).xyz - l_pos);

	float c2 = tan(phi) * occ_surf_dist;
	vec3 rr_pos = reproject(light_projection_transform, vec4(l_pos + vec3(0, c2, 0), 1)).xyz * 0.5 + 0.5;
	c2 = length(rr_pos - r_pos) * 1024;
	c2 = clamp(c2, 1, 64);
	int b = int(c2 - 1)/2;

	vec4 comp = r_pos.zzzz - 0.01;
	vec4 acc = vec4(0);
	acc += vec4(greaterThan(comp, textureGatherOffset(shadow_texture, r_pos.xy, ivec2(-1, -1)) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(shadow_texture, r_pos.xy, ivec2(1, -1)) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(shadow_texture, r_pos.xy, ivec2(-1, 1)) ));
	acc += vec4(greaterThan(comp, textureGatherOffset(shadow_texture, r_pos.xy, ivec2(1, 1)) ));
	count = 4;

	return dot(acc, vec4(1/16.0));
}

float get_shadow_exp(vec4 pos)
{
	vec3 r_pos = (reproject(light_modelviewprojection_transform, pos).xyz + 1) * 0.5;
	return
		clamp(
			1 - clamp(textureLod(shadow_texture, r_pos.xy, 0).x * exp(EXP_SCALE_FACTOR * (1 - r_pos.z)), 0, 1),
			0, 1);
	/*return
		clamp(
			1 - textureLod(shadow_texture, r_pos.xy, 0).x * exp(EXP_SCALE_FACTOR * (1 - r_pos.z)),
			0, 1);*/
}

//
/*vec2 get_sampling_point_cont2(int i)
{
	i *= 23;
	return reflect(
		sampling_pattern[(i * 7 + 173547) % 256], 
		sampling_pattern[(i * 19 + 472541) % 256]);
}*/

float get_shadow_soft_exp(vec4 pos)
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

float get_shadow_soft_exp2(vec4 pos)
{
	vec3 r_pos = (reproject(light_modelviewprojection_transform, pos).xyz + 1) * 0.5;
	vec3 l_pos = (light_modelview_transform * pos).xyz;
	float avg_depth = 0;
	const int avg_depth_count = 8;
	float phi = light_size;
	float c2 = 1;
	float c2corr = 0.1;
	int stepcount = 0;
	vec3 ro_pos = vec3(0, 0, 1);

	// Loop for finding a good size of kernel for the depthmap filtering
	//while(abs(c2 - c2corr) > 0.00001 && stepcount < 5)
	stepcount++;
	c2 = c2corr;
	for(int i = 0; i < avg_depth_count; i ++)
	{
		vec2 rtest_pos = i == 0 ? r_pos.xy: r_pos.xy + get_sampling_point(i) * c2;
		float smp = 
			clamp(
				log(
					texture(
						shadow_texture, test_pos)) / EXP_SCALE_FACTOR + 1 + .0001,
				0, 1);
		
		if(smp < r_pos.z && length(rtest_pos - r_pos.xy) < length(ro_pos.xy - r_pos.xy))
		{
			ro_pos = vec3(rtest_pos, smp);
		}
	}

	// Find an ocludder's distance in light coordinate frame
	vec4 o1_pos = 
		reproject(
			inverse(light_projection_transform), 2 * vec4(ro_pos, 1) - 1
		) - l_pos;
	vec4 o2_pos =
		reproject(
			inverse(light_projection_transform), 2 * vec4(r_pos.xy, ro_pos.z, 1) - 1
		) - l_pos;
	// Compute a distance (for now just by means of z difference)
	float result = length(o1_pos - o2_pos)/(tan(phi) * length(o2_pos));	
	return clamp(1 - result, 0, 1);
}

float get_shadow(vec4 pos)
{
	switch(shadow_implementation)
	{
		case 0:
			return get_shadow_no_filter(pos);
		case 1:
			return get_shadow_pcf4x4(pos);
		case 2:
			return get_shadow_soft_exp2(pos);
		default:
			return 0;
	}
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
	float shadow = get_shadow(p_pos);

	float luminance = 0.3 * material.r + 0.5 * material.g + 0.2 * material.b;
	vec3 ambientmat =  0.5 * (material + normalize(vec3(1, 1, 1)) * dot(material, normalize(vec3(1, 1, 1))));

	vec3 diffuse = material * max(dot(light.dir, p_nd.xyz) * (1 - shadow), 0);
	vec3 color = (diffuse  + ambient * ambientmat) * (1 - aoc);

	color_luma = vec4(color, sqrt(dot(color.rgb, vec3(0.299, 0.587, 0.114))));
	//color_luma = vec4(p_nd.xyz * 0.5 + 0.5, sqrt(dot(p_nd.rgb* 0.5 + 0.5, vec3(0.299, 0.587, 0.114))));
	//color_luma = vec4(shadow, 0, 0, 1);
	gl_FragDepth = texture(normaldepth_texture, param).w;
}
