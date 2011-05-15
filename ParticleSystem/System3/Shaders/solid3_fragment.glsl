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
	float aoc = texture(aoc_texture, param).x;
	gl_FragColor = vec4(1, 1, 1, 1) * (1 - vec4(aoc, aoc, aoc, 1));
	gl_FragDepth = texture(normaldepth_texture, param).w;
}