#version 330
uniform mat4 modelview_transform;

in vec3 cube_pos;
in vec3 cube_dimensions;

out Axes{
	vec4 x;
	vec4 y;
	vec4 z;
} axes;

void main () {
	gl_Position = modelview_transform * vec4(cube_pos, 1);
	axes.x = modelview_transform * vec4(cube_dimensions.x * 0.5, 0, 0, 0);
	axes.y = modelview_transform * vec4(0, cube_dimensions.y * 0.5, 0, 0);
	axes.z = modelview_transform * vec4(0, 0, cube_dimensions.z * 0.5, 0);
}