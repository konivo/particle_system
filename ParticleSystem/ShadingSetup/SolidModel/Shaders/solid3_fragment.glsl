#version 400
#pragma include <RenderPassFactory.Shaders.common.include>

///////////////////////
//model-view matrices//
uniform mat4 projection_transform;
uniform mat4 modelviewprojection_inv_transform;
uniform float particle_brightness;
uniform float smooth_shape_sharpness;

////////////
//textures//
uniform sampler2D custom_texture;
uniform sampler2D normaldepth_texture;
uniform sampler2D aoc_texture;
uniform sampler2D shadow_texture;
uniform sampler2D colorramp_texture;

///////////////////////////////
//particle attribute textures//
uniform sampler2D particle_attribute1_texture;

/////////////////////
//material settings//
/*
0 - normal
1 - color-ramp
2 - attribute1
*/
uniform int material_color_source;

////////////////////////////////
//light settings and mattrices//
//
uniform vec3 ambient = vec3(.1, .1, .1);
uniform mat4 light_modelviewprojection_transform;
uniform mat4 light_modelview_transform;
uniform mat4 light_projection_transform;
uniform mat4 light_projection_inv_transform;
uniform mat4 light_relativeillumination_transform;
//
//for spotlight real dimension
//for directional light an spherical angles
uniform float light_size;
uniform float light_expmap_level;
uniform float light_expmap_range;
uniform float light_expmap_range_k;
uniform int light_expmap_nsamples;

////////////////////////
//soft shadow settings//
//
uniform bool enable_soft_shadow = true;
/*
0 - no filtering
1 - pcf 4x4
2 - exp shadow map
3 - soft shadow with pcf
4 - soft shadow with exp
*/
uniform int shadow_implementation;

////////////////////
//common constants//
const float EXP_SCALE_FACTOR = 50;

//
struct Light
{
	vec3 pos;
	vec3 dir;
};

const Light light = Light( vec3(0, 0, 0), vec3(-1, -1, -1));

in VertexData
{
	vec2 param;
};

out vec4 color_luma;

//
vec4 get_normal_depth (vec2 param)
{
	vec4 result = texture(normaldepth_texture, param);
	result = result * 2 - 1;

	return result;
}

float get_shadow(vec4 pos)
{
	vec3 r_pos = (reproject(light_modelviewprojection_transform, pos).xyz + 1) * 0.5;
	return r_pos.z > texture(shadow_texture, r_pos.xy).x ? 1: 0;
}

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
	gl_FragDepth = texture(normaldepth_texture, param).w;
}