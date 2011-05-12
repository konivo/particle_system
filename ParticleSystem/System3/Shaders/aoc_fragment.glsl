#version 330
uniform mat4 modelview_transform;
uniform mat4 modelviewprojection_transform;
uniform mat4 projection_transform;

uniform sampler2D depth_texture;

in VertexData
{
	vec2 param;
};

out Fragdata
{
	float aoc;
};

void main ()
{
	aoc = 0.1f;
}