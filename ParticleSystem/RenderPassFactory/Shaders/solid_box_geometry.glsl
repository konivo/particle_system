#version 330
uniform mat4 modelview_transform;
uniform mat4 projection_transform;
uniform mat4 modelviewprojection_transform;
uniform mat4 modelview_inv_transform;
uniform float particle_scale_factor;

layout (points) in;
layout (triangle_strip, max_vertices = 18) out;

in SpriteData
{
	//particle position in space
  vec3 pos;

	//input particle dimensions
	float radius;

	//input particle color
	vec3 color;

	//
	mat4 rotation;

	//
	mat4 rotation_local;

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

	//
	vec3 normal;

} SpriteOUT;

//generates quad shaped sprite,
void main ()
{
	SpriteOUT.pos = SpriteIN[0].pos;
	SpriteOUT.radius = particle_scale_factor * SpriteIN[0].radius;
	SpriteOUT.color = SpriteIN[0].color;

	vec4 p = modelviewprojection_transform * vec4(SpriteOUT.pos, 1);
	vec4 x = modelviewprojection_transform * vec4(SpriteOUT.radius, 0, 0, 0);
	vec4 y = modelviewprojection_transform * vec4(0, SpriteOUT.radius, 0, 0);
	vec4 z = modelviewprojection_transform * vec4(0, 0, SpriteOUT.radius, 0);

	//---------------
	gl_Position = p - x - y - z;
	EmitVertex();

	//
	gl_Position = p - x - y + z;
	EmitVertex();

		//
	gl_Position = p + x - y - z;
	SpriteOUT.normal = vec3(0, -1, 0);
	EmitVertex();

		//
	gl_Position = p + x - y + z;
	EmitVertex();

	////
	//
	gl_Position = p + x + y - z;
	SpriteOUT.normal = vec3(1, 0, 0);
	EmitVertex();

	//
	gl_Position = p + x + y + z;
	EmitVertex();

	////
	//
	gl_Position = p - x + y - z;
	SpriteOUT.normal = vec3(0, 1, 0);
	EmitVertex();

	//
	gl_Position = p - x + y + z;
	EmitVertex();

	////
	//
	gl_Position = p - x - y - z;
	SpriteOUT.normal = vec3(-1, 0, 0);
	EmitVertex();

	//
	gl_Position = p - x - y + z;
	EmitVertex();
	EndPrimitive();

	//---------------
	gl_Position = p - x - y + z;
	SpriteOUT.normal = vec3(0, 0, 1);
	EmitVertex();

	//
	gl_Position = p - x + y + z;
	EmitVertex();

	//
	gl_Position = p + x - y + z;
	EmitVertex();

	//
	gl_Position = p + x + y + z;
	EmitVertex();
	EndPrimitive();

	//---------------
	gl_Position = p - x - y - z;
	SpriteOUT.normal = vec3(0, 0, -1);
	EmitVertex();

	//
	gl_Position = p - x + y - z;
	EmitVertex();

	//
	gl_Position = p + x - y - z;
	EmitVertex();

	//
	gl_Position = p + x + y - z;
	EmitVertex();
	EndPrimitive();


}