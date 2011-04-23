#version 330
uniform mat4 modelview_transform;
uniform mat4 modelviewprojection_transform;
uniform mat4 projection_transform;
uniform float particle_scale_factor;

//particle position in space
in vec3 sprite_pos;

//input particle dimensions
in vec3 sprite_dimensions;

//input particle color
in vec3 sprite_color;

//particle color
out vec3 color;

//particle camera z-coordinate (negative value)
out float z_orig;

//particle camera z-coordinate delta (important for solid sphere particle)
out float z_maxdelta;

//horizontal and vertical clipping space deltas
out vec2 xy_maxdelta;

void main () {

	vec4 p0 = modelview_transform * vec4(sprite_pos, 1);
	vec4 p1 = p0 + vec4(1, 0, 0, 0) * particle_scale_factor * sprite_dimensions.x;
	vec4 p2 = p0 + vec4(0, 1, 0, 0) * particle_scale_factor * sprite_dimensions.y;

	z_orig = p0.z;
	z_maxdelta = particle_scale_factor * sprite_dimensions.z;

	p0 = projection_transform * p0;
	p1 = projection_transform * p1;
	p2 = projection_transform * p2;

	p0 /= p0.w;
	p1 /= p1.w;
	p2 /= p2.w;

	xy_maxdelta = vec2(length(p1 - p0), length(p2 - p0));

	color = sprite_color;
	gl_Position = p0;
}