#version 430
layout (points) in;
layout (triangle_strip, max_vertices = 4) out;

out VertexData
{
	vec2 param;
} OUT;

//generates quad shaped sprite,
void main ()
{
	gl_Position = vec4(-1, -1, -1, 1);
	OUT.param = vec2(0, 0);
	EmitVertex();

	gl_Position = vec4(-1, 1, -1, 1);
	OUT.param = vec2(0, 1);
	EmitVertex();

	gl_Position = vec4(1, -1, -1, 1);
	OUT.param = vec2(1, 0);
	EmitVertex();

	gl_Position = vec4(1, 1, -1, 1);
	OUT.param = vec2(1, 1);
	EmitVertex();
}