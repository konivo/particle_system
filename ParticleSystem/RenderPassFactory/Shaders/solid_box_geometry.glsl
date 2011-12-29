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

	mat4 model_transform;

	//
	mat4 mvp;

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
	flat vec3 normal;

} SpriteOUT;

//generates quad shaped sprite,
void main ()
{
	SpriteOUT.pos = SpriteIN[0].pos;
	SpriteOUT.radius = particle_scale_factor * SpriteIN[0].radius;
	SpriteOUT.color = SpriteIN[0].color;
	mat4 mvp = SpriteIN[0].mvp;

	vec4 p = mvp * vec4(0, 0, 0, 1);
	vec4 x = mvp * vec4(SpriteOUT.radius, 0, 0, 0);
	vec4 y = mvp * vec4(0, SpriteOUT.radius, 0, 0);
	vec4 z = mvp * vec4(0, 0, SpriteOUT.radius, 0);

	vec3 zN = SpriteIN[0].model_transform[2].xyz;		//local space's z-dir expressed in world space coordinates
	vec3 yN = SpriteIN[0].model_transform[1].xyz;		//y-dir
	vec3 xN = SpriteIN[0].model_transform[0].xyz;		//x-dir

	//---------------
	gl_Position = p - x - y - z;
	EmitVertex();

	//
	gl_Position = p - x - y + z;
	EmitVertex();

		//
	gl_Position = p + x - y - z;
	SpriteOUT.normal = -yN; //OUT.model vec3(0, -1, 0);
	EmitVertex();

		//
	gl_Position = p + x - y + z;
	EmitVertex();

	////
	//
	gl_Position = p + x + y - z;
	SpriteOUT.normal = xN; //vec3(1, 0, 0);
	EmitVertex();

	//
	gl_Position = p + x + y + z;
	EmitVertex();

	////
	//
	gl_Position = p - x + y - z;
	SpriteOUT.normal = yN; //vec3(0, 1, 0);
	EmitVertex();

	//
	gl_Position = p - x + y + z;
	EmitVertex();

	////
	//
	gl_Position = p - x - y - z;
	SpriteOUT.normal = -xN; //vec3(-1, 0, 0);
	EmitVertex();

	//
	gl_Position = p - x - y + z;
	EmitVertex();
	EndPrimitive();

	//---------------
	gl_Position = p - x - y + z;
	SpriteOUT.normal = zN; //vec3(0, 0, 1);
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
	SpriteOUT.normal = -zN; //vec3(0, 0, -1);
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