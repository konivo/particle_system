#version 330
uniform mat4 modelview_transform;

layout (points) in;
layout (line_strip, max_vertices = 12) out;

in Axes{
	vec4 x;
	vec4 y;
	vec4 z;
} axes[];

void main () {

	gl_Position = (gl_in[0].gl_Position - axes[0].x - axes[0].y);
	EmitVertex();

	gl_Position = (gl_in[0].gl_Position - axes[0].x + axes[0].y);
	EmitVertex();

	gl_Position = (gl_in[0].gl_Position + axes[0].x + axes[0].y);
	EmitVertex();

	gl_Position = (gl_in[0].gl_Position + axes[0].x - axes[0].y);
	EmitVertex();

	gl_Position = (gl_in[0].gl_Position - axes[0].x - axes[0].y);
	EmitVertex();
}