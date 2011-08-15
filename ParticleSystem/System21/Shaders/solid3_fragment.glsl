#version 330
uniform mat4 projection_transform;
uniform mat4 modelviewprojection_inv_transform;
uniform float particle_brightness;
uniform float smooth_shape_sharpness;

uniform sampler2D custom_texture;
uniform sampler2D normaldepth_texture;
uniform sampler2D aoc_texture;
uniform sampler2D uv_colorindex_texture;
uniform sampler2D shadow_texture;

uniform vec4[3] colors2;

uniform vec3 ambient = vec3(.1, .1, .1);
uniform mat4 light_modelviewprojection_transform;
uniform mat4 light_relativeillumination_transform;

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

out Fragdata
{
	vec4 color_luma;
};

//
vec4 get_normal_depth (vec2 param)
{
	vec4 result = texture(normaldepth_texture, param);
	result = result * 2 - 1;

	return result;
}

//
vec4 get_clip_coordinates (vec2 param, float imagedepth)
{
	return vec4((param * 2) - 1, imagedepth, 1);
}

//
vec4 reproject (mat4 transform, vec4 vector)
{
	vec4 result = transform * vector;
	result /= result.w;
	return result;
}

float get_shadow(vec4 pos)
{
	vec4 r_pos = reproject(light_modelviewprojection_transform, pos);
	float depth = texture(shadow_texture, (r_pos.xy + 1) / 2).x;

	return (r_pos.z + 0.999) / 2 > depth ? 0.999 : 0;
}

void main ()
{
//TODO: cleanup
	vec4 p_nd = get_normal_depth(param);
	vec4 p_clip = get_clip_coordinates(param, p_nd.w);
	vec4 p_pos = reproject(modelviewprojection_inv_transform, p_clip);

	vec2 cparam = 2 * (texture(uv_colorindex_texture, param).xy - 0.5f);
	float dist = length(cparam);

	float aoc = texture(aoc_texture, param).x;

	vec3 material = vec3((p_nd.xyz + 1) * 0.5f);
	material = min( material / material .x, material / material .y);
	material = min( material, material / material .z);

	float luminance = 0.3 * material.r + 0.5 * material.g + 0.2 * material.b;
	vec3 ambientmat =  0.5 * (material + normalize(vec3(1, 1, 1)) * dot(material, normalize(vec3(1, 1, 1))));

	float shadow = get_shadow(p_pos);

	vec3 diffuse = material * max(dot(light.dir, p_nd.xyz) * (1 - shadow), 0);
	vec3 color = (diffuse  + ambient * ambientmat) * (1 - aoc);

	color_luma = vec4(color, sqrt(dot(color.rgb, vec3(0.299, 0.587, 0.114))));
	gl_FragDepth = texture(normaldepth_texture, param).w;
}