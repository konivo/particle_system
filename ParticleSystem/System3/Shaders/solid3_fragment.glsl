#version 330
uniform mat4 projection_transform;
uniform float particle_brightness;
uniform float smooth_shape_sharpness;

uniform sampler2D custom_texture;
uniform sampler2D normaldepth_texture;
uniform sampler2D aoc_texture;
uniform sampler2D uv_colorindex_texture;

in VertexData
{
	vec2 param;
};

void main ()
{
	gl_FragColor = vec4(0, texture(aoc_texture, param).w, 0, 1);
	//gl_FragColor = vec4(texture(normaldepth_texture, param).xyz, 1);
	gl_FragDepth = texture(normaldepth_texture, param).w;
}