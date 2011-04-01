#version 330
uniform mat4 modelview_transform;

layout (points) in;
layout (triangle_strip, max_vertices = 12) out;

in Axes{
	vec4 x;
	vec4 y;
	vec4 z;
} axes[];

in vec3[] dimensions;

out vec2 param;

void main ()
{
	param = vec2(0, 0);
	gl_Position = gl_in[0].gl_Position - dimensions[0].x * axes[0].x - dimensions[0].y * axes[0].y;
	EmitVertex();

	param = vec2(0, 1);
	gl_Position = gl_in[0].gl_Position - dimensions[0].x * axes[0].x + dimensions[0].y * axes[0].y;
	EmitVertex();

	param = vec2(1, 0);
	gl_Position = gl_in[0].gl_Position + dimensions[0].x * axes[0].x - dimensions[0].y * axes[0].y;
	EmitVertex();

	param = vec2(1, 1);
	gl_Position = gl_in[0].gl_Position + dimensions[0].x * axes[0].x + dimensions[0].y * axes[0].y;
	EmitVertex();
}