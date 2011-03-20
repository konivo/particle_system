#version 330
uniform mat4 modelview_transform; 

in vec3 sprite_pos;
in vec3 sprite_dimensions;

out Axes{
	vec4 x;
	vec4 y;
	vec4 z;
} axes;

out vec3 dimensions;

void main () {
	gl_Position = modelview_transform * vec4(sprite_pos, 1);
	axes.x = modelview_transform * vec4(1, 0, 0, 0);
	axes.y = modelview_transform * vec4(0, 1, 0, 0);
	axes.z = modelview_transform * vec4(0, 0, 1, 0);

	dimensions = sprite_dimensions;
}