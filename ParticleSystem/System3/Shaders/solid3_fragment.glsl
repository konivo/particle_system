#version 330
uniform mat4 projection_transform;
uniform float particle_brightness;
uniform float smooth_shape_sharpness;

uniform sampler2D custom_texture;
uniform sampler2D normaldepth_texture;
uniform sampler2D aoc_texture;
uniform sampler2D uv_colorindex_texture;

uniform vec4[3] colors2;

in VertexData
{
	vec2 param;
};

void main ()
{
	vec2 cparam = 2 * (texture(uv_colorindex_texture, param).xy - 0.5f);
	float dist = length(cparam);

	float aoc = texture(aoc_texture, param).x;
	vec4 color = vec4(vec3(1, 1, 1) * pow(1.0f - dist, smooth_shape_sharpness), 1);


	gl_FragColor = color * (1 - vec4(aoc, aoc, aoc, 0)* 1.5);
	gl_FragDepth = texture(normaldepth_texture, param).w;
}