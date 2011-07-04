#version 330
uniform mat4 projection_transform;
uniform float particle_brightness;
uniform float smooth_shape_sharpness;

uniform sampler2D custom_texture;
uniform sampler2D normaldepth_texture;
uniform sampler2D aoc_texture;
uniform sampler2D uv_colorindex_texture;

uniform vec4[3] colors2;

uniform vec4 ambient = vec4(.1, .1, .1, 1);

struct Light
{
	vec3 pos;
	vec3 dir;
};

const Light light = Light( vec3(0, 0, 0), vec3(1, 0, 0));

in VertexData
{
	vec2 param;
};

//
vec4 get_normal_depth (vec2 param)
{
	vec4 result = texture(normaldepth_texture, param);
	result = result * 2 - 1;

	return result;
}

void main ()
{
	vec4 nd = get_normal_depth(param);

	vec2 cparam = 2 * (texture(uv_colorindex_texture, param).xy - 0.5f);
	float dist = length(cparam);

	float aoc =  texture(aoc_texture, param).x;

	vec3 material = vec3((nd.xyz + 1) * 0.5f);
	material = min( material / material .x, material / material .y);
	material = min( material, material / material .z);

	float luminance = 0.3 * material.r + 0.5 * material.g + 0.2 * material.b;
	vec3 shadowedmat =  0.5 * (material + normalize(vec3(1, 1, 1)) * dot(material, normalize(vec3(1, 1, 1))));

	vec4 diffuse = vec4(material, 1) * max(dot(light.dir, nd.xyz), 0);

	gl_FragColor = diffuse * ( 1 - aoc) + ambient * vec4(shadowedmat, 1);
	gl_FragDepth = texture(normaldepth_texture, param).w;
}