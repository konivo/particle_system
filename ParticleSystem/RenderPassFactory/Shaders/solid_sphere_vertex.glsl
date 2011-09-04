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


out SpriteData
{
	//particle position in space
  vec3 pos;

	//input particle dimensions
	float radius;

	//input particle color
	vec3 color;
} OUT;

void main ()
{
	OUT.pos = sprite_pos;
	OUT.radius = sprite_dimensions.x;
	OUT.color = sprite_color;
}