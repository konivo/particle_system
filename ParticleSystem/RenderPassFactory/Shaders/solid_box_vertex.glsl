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

//input particle color
in mat4 sprite_rotation;

//input particle color
in mat4 sprite_rotation_local;


out SpriteData
{
	//particle position in space
  vec3 pos;

	//input particle dimensions
	float radius;

	//input particle color
	vec3 color;

	//
	out mat4 mvp;

	//
	out mat4 model_transform;

} OUT;

void main ()
{
	OUT.model_transform = sprite_rotation;
	OUT.model_transform[3] = vec4(sprite_pos, 1);

	OUT.pos = sprite_pos;
	OUT.radius = sprite_dimensions.x;
	OUT.color = sprite_color;
	OUT.mvp = modelviewprojection_transform * OUT.model_transform;
}