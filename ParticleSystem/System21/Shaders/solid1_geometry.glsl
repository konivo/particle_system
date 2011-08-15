#version 330
uniform mat4 modelview_transform;
uniform mat4 projection_transform;
uniform mat4 modelviewprojection_transform;
uniform mat4 modelview_inv_transform;
uniform float particle_scale_factor;

layout (points) in;
layout (triangle_strip, max_vertices = 12) out;

in SpriteData
{
	//particle position in space
  vec3 pos;

	//input particle dimensions
	float radius;

	//input particle color
	vec3 color;

} SpriteIN[];

//
out SpriteData
{
	//particle position in space
  vec3 pos;

	//input particle dimensions
	float radius;

	//input particle color
	vec3 color;

} SpriteOUT;

//
out CameraData
{
	vec4 ray_dir;
	vec4 pos;

	//-1, 1 parameterization
	//view dependent parametrization
	vec2 param;

} CameraOUT;

//generates quad shaped sprite,
void main ()
{
	SpriteOUT.pos = SpriteIN[0].pos;
	SpriteOUT.radius = particle_scale_factor * SpriteIN[0].radius;
	SpriteOUT.color = SpriteIN[0].color;

	//is focal length infinite?
	bool focal_len_infinite = projection_transform[2][3] == 0;

	//
	vec4 camera_center = modelview_inv_transform[3];
	//modelview is expected to be orthonormal
	vec4 camera_dir = - modelview_inv_transform[2];

	vec4 spritepos = vec4(SpriteIN[0].pos, 1);
	vec4 p0 =
		focal_len_infinite?
		camera_dir * dot(camera_dir, spritepos - camera_center) :
		spritepos - camera_center;

	//offset in the camera plane (makes sense for an orthographic projection)
	vec4 sprite_camplane_offset = spritepos - p0;

	float p0_l_i = 1 / length(p0);

	//sprite plane world vectors
	vec4 world_plane_x_dir = normalize(vec4(-p0.z, 0, p0.x, 0)) * SpriteOUT.radius; //? todo: how to robustly find orthogonal non-zero vector?
	vec4 world_plane_y_dir = vec4(cross(p0.xyz, world_plane_x_dir.xyz) * p0_l_i, 0);
	vec4 world_plane_z_dir = vec4( p0.xyz * (1 - SpriteOUT.radius * p0_l_i), 0);

	//sprite plane put into projective space
	vec4 proj_plane_point =
		focal_len_infinite?
		modelviewprojection_transform * (world_plane_z_dir + sprite_camplane_offset):
		modelviewprojection_transform * (world_plane_z_dir + camera_center);
	vec4 proj_plane_x_dir =  modelviewprojection_transform * world_plane_x_dir;
	vec4 proj_plane_y_dir =  modelviewprojection_transform * world_plane_y_dir;

	//implementation for orthographic projection differs too much from frustum,
	//thus they take the separate way
	if(focal_len_infinite)
	{
		CameraOUT.ray_dir = camera_dir;

		//
		CameraOUT.param = vec2(0, 0);
		CameraOUT.pos = - world_plane_x_dir - world_plane_y_dir + sprite_camplane_offset;
		gl_Position = - proj_plane_x_dir - proj_plane_y_dir + proj_plane_point;
		EmitVertex();

		CameraOUT.param = vec2(0, 1);
		CameraOUT.pos = - world_plane_x_dir + world_plane_y_dir + sprite_camplane_offset;
		gl_Position = - proj_plane_x_dir + proj_plane_y_dir + proj_plane_point;
		EmitVertex();

		CameraOUT.param = vec2(1, 0);
		CameraOUT.pos = world_plane_x_dir - world_plane_y_dir + sprite_camplane_offset;
		gl_Position = proj_plane_x_dir - proj_plane_y_dir + proj_plane_point;
		EmitVertex();

		CameraOUT.param = vec2(1, 1);
		CameraOUT.pos = world_plane_x_dir + world_plane_y_dir + sprite_camplane_offset;
		gl_Position = proj_plane_x_dir + proj_plane_y_dir + proj_plane_point;
		EmitVertex();
	}
	else
	{
		CameraOUT.pos = camera_center;
		//
		CameraOUT.param = vec2(0, 0);
		CameraOUT.ray_dir = - world_plane_x_dir - world_plane_y_dir + world_plane_z_dir;
		gl_Position = - proj_plane_x_dir - proj_plane_y_dir + proj_plane_point;
		EmitVertex();

		CameraOUT.param = vec2(0, 1);
		CameraOUT.ray_dir = - world_plane_x_dir + world_plane_y_dir + world_plane_z_dir;
		gl_Position = - proj_plane_x_dir + proj_plane_y_dir + proj_plane_point;
		EmitVertex();

		CameraOUT.param = vec2(1, 0);
		CameraOUT.ray_dir = world_plane_x_dir - world_plane_y_dir + world_plane_z_dir;
		gl_Position = proj_plane_x_dir - proj_plane_y_dir + proj_plane_point;
		EmitVertex();

		CameraOUT.param = vec2(1, 1);
		CameraOUT.ray_dir = world_plane_x_dir + world_plane_y_dir + world_plane_z_dir;
		gl_Position = proj_plane_x_dir + proj_plane_y_dir + proj_plane_point;
		EmitVertex();
	}
}