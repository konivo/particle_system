#version 400
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
uniform mat4 light_modelview_transform;
uniform mat4 light_projection_transform;
uniform mat4 light_projection_inv_transform;
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
	vec3 r_pos = (reproject(light_modelviewprojection_transform, pos).xyz + 1) * 0.5;
	vec3 l_pos = (light_modelview_transform * pos).xyz;
	vec4 avg_depth;
	float count = 36;
	float phi = 0.1;

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
	c2 = clamp(c2, 4, 16);
	int b = int(c2 - 1)/2;

	count = 0;
	vec4 acc;
	for(int i = -b; i < b + 1; i += 2)
		for(int j = -b; j < b + 1; j += 2)
		{
			vec4 depths = textureGatherOffset(shadow_texture, r_pos.xy, ivec2(i, j));
			count += 4;
			acc += vec4(greaterThan(r_pos.zzzz - 0.0001, depths));
		}

	return dot(acc, vec4(1/count));
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

	float shadow = 0;//get_shadow(p_pos);

	vec3 diffuse = material * max(dot(light.dir, p_nd.xyz) * (1 - shadow), 0);
	vec3 color = (diffuse  + ambient * ambientmat) * (1 - aoc);

	color_luma = vec4(color, sqrt(dot(color.rgb, vec3(0.299, 0.587, 0.114))));
	gl_FragDepth = texture(normaldepth_texture, param).w;
}