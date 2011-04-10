#version 330
uniform mat4 modelview_transform;
uniform mat4 modelviewprojection_transform;
uniform mat4 projection_transform;

in vec3 sprite_pos;
in vec3 sprite_dimensions;

out vec2 scale;
out vec3 dimensions;

void main () {

	vec4 p0 = modelview_transform * vec4(sprite_pos, 1);
	vec4 p1 = p0 + vec4(1, 0, 0, 0);
	vec4 p2 = p0 + vec4(0, 1, 0, 0);

	p0 = projection_transform * p0;
	p1 = projection_transform * p1;
	p2 = projection_transform * p2;

	p0 /= p0.w;
	p1 /= p1.w;
	p2 /= p2.w;

	scale = vec2(length(p1 - p0), length(p2 - p0));
	dimensions = sprite_dimensions * 20;

	gl_Position = p0;
}