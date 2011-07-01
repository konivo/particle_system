#version 330
uniform mat4 projection_transform;
uniform float particle_brightness;
uniform float smooth_shape_sharpness;

uniform sampler2D custom_texture;
uniform sampler2D normaldepth_texture;
uniform sampler2D aoc_texture;
uniform sampler2D uv_colorindex_texture;

uniform vec4[3] colors2;

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
	aoc = pow(aoc, 2)* 0.7;
	vec4 color = vec4(vec3(1, 1, 1) * pow(1.0f - dist, smooth_shape_sharpness), 1);

	vec4 diffuse = color * dot(light.dir, nd.xyz) + vec4(0.2, 0.2, 0.2, 1);

	//gl_FragColor = vec4(aoc, aoc, aoc, 1);
	gl_FragColor = vec4((nd.xyz + 1) * 0.5f, 1) * (1 - vec4(aoc, aoc, aoc, aoc)) + aoc * vec4(-0.2, -0.2, -0.2, 1);
	//gl_FragColor = vec4((nd.xyz + 1) * 0.5f, 1);
	//gl_FragColor = vec4(aoc, aoc, aoc, 1);
	//gl_FragColor = diffuse * (1 - vec4(aoc, aoc, aoc, aoc)) + aoc * vec4(-0.1, -0.1, -0.1, 1);
	gl_FragDepth = texture(normaldepth_texture, param).w;
}