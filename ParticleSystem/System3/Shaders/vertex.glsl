#version 330
uniform mat4 modelview_transform;
uniform mat4 modelviewprojection_transform;
uniform mat4 projection_transform;

in vec3 sprite_pos;
in vec3 sprite_dimensions;
in vec3 sprite_color;

out vec3 scale;
out vec3 dimensions;
out vec3 color;

void main () {

	vec4 p0 = modelview_transform * vec4(sprite_pos, 1);
	vec4 p1 = p0 + vec4(1, 0, 0, 0);
	vec4 p2 = p0 + vec4(0, 1, 0, 0);
	vec4 p3 = p0 + vec4(0, 0, 1, 0);

	p0 = projection_transform * p0;
	p1 = projection_transform * p1;
	p2 = projection_transform * p2;
	p3 = projection_transform * p3;

	p0 /= p0.w;
	p1 /= p1.w;
	p2 /= p2.w;
	p3 /= p3.w;

	scale = vec3(length(p1 - p0), length(p2 - p0), length(p3 - p0));
	dimensions = sprite_dimensions * 20;

	color = sprite_color;
	gl_Position = p0;
}