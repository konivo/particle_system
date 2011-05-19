#version 330
uniform mat4 modelview_transform;
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

	vec4 p0 = modelview_transform * vec4(SpriteIN[0].pos, 1);
	float p0_l = length(p0.xyz);

	vec4 cam_plane_x_dir = normalize(vec4(-p0.z, 0, p0.x, 0));
	vec4 cam_plane_y_dir = vec4(cross(p0.xyz, cam_plane_x_dir.xyz) / p0_l, 0);
	vec4 cam_plane_point = vec4( p0.xyz * ((p0_l - SpriteOUT.radius)/p0_l), 1);

	vec4 world_plane_x_dir =  modelview_inv_transform * cam_plane_x_dir;
	vec4 world_plane_y_dir =  modelview_inv_transform * cam_plane_y_dir;
	vec4 world_plane_point =  modelview_inv_transform * cam_plane_point;

	CameraOUT.pos =  modelview_inv_transform * vec4(0, 0, 0, 1);

	//
	CameraOUT.param = vec2(0, 0);
	CameraOUT.ray_dir = ((-world_plane_x_dir - world_plane_y_dir) * SpriteOUT.radius + world_plane_point);
	gl_Position = modelviewprojection_transform * CameraOUT.ray_dir;
	CameraOUT.ray_dir -= CameraOUT.pos;
	EmitVertex();

	CameraOUT.param = vec2(0, 1);
	CameraOUT.ray_dir = ((-world_plane_x_dir + world_plane_y_dir) * SpriteOUT.radius + world_plane_point);
	gl_Position = modelviewprojection_transform * CameraOUT.ray_dir;
	CameraOUT.ray_dir -= CameraOUT.pos;
	EmitVertex();

	CameraOUT.param = vec2(1, 0);
	CameraOUT.ray_dir = ((+world_plane_x_dir - world_plane_y_dir) * SpriteOUT.radius + world_plane_point);
	gl_Position = modelviewprojection_transform * CameraOUT.ray_dir;
	CameraOUT.ray_dir -= CameraOUT.pos;
	EmitVertex();

	CameraOUT.param = vec2(1, 1);
	CameraOUT.ray_dir = ((world_plane_x_dir + world_plane_y_dir) * SpriteOUT.radius + world_plane_point);
	gl_Position = modelviewprojection_transform * CameraOUT.ray_dir;
	CameraOUT.ray_dir -= CameraOUT.pos;
	EmitVertex();
}