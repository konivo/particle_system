#version 330
uniform mat4 projection_transform;
uniform float particle_brightness;
uniform float smooth_shape_sharpness;

uniform sampler2D custom_texture;
uniform sampler2D depth_texture;
uniform sampler2D aoc_texture;
uniform sampler2D uv_colorindex_texture;

in VertexData
{
	vec2 param;
};

void main ()
{
	gl_FragColor = texture(uv_colorindex_texture, param);
	gl_FragDepth = texture(depth_texture, param).x;
}