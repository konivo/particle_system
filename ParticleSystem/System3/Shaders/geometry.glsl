#version 330
uniform mat4 modelview_transform;
uniform float particle_scale_factor;

layout (points) in;
layout (triangle_strip, max_vertices = 12) out;

in Axes{
	vec4 x;
	vec4 y;
	vec4 z;
} axes[];

in vec2[] scale;
in vec3[] dimensions;

out vec2 param;

void main ()
{
	vec4 dx = particle_scale_factor * dimensions[0].x * vec4(scale[0].x, 0, 0, 0);
	vec4 dy = particle_scale_factor * dimensions[0].y * vec4(0, scale[0].y, 0, 0);

	param = vec2(0, 0);
	gl_Position = gl_in[0].gl_Position - dx - dy;
	EmitVertex();

	param = vec2(0, 1);
	gl_Position = gl_in[0].gl_Position - dx + dy;
	EmitVertex();

	param = vec2(1, 0);
	gl_Position = gl_in[0].gl_Position + dx - dy;
	EmitVertex();

	param = vec2(1, 1);
	gl_Position = gl_in[0].gl_Position + dx + dy;
	EmitVertex();
}