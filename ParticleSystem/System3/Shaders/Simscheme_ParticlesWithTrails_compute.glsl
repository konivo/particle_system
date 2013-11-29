#version 440
layout(local_size_x=8, local_size_y=1) in;

////////////////////////////////////////////////////////////////////////////////
//types//
struct MetaInformation
{
	int LifeLen;
	int Leader;
	float Size;
	vec3 Velocity;
};

////////////////////////////////////////////////////////////////////////////////
//uniforms//
uniform mat4 projection_transform;
uniform mat4 modelviewprojection_transform;
uniform mat4 modelviewprojection_inv_transform;
uniform mat4 modelview_inv_transform;
uniform vec2 viewport_size;
uniform float pRayMarchStepFactor;

uniform float u_Sigma = 10;
uniform float u_Rho = 28;
uniform float u_Beta = 2.6;

uniform float u_Dt = 0.005;

////////////////////////////////////////////////////////////////////////////////
//constants//
const float epsilon = 0.001;
const float nearPlaneZ = 1;

//chunk algorithm
const int c_ChunkSize_x = 4;
const int c_ChunkSize_y = 4;
const ivec2 u_ChunkSize = {c_ChunkSize_x, c_ChunkSize_y};

////////////////////////////////////////////////////////////////////////////////
//buffer//
buffer Position
{
	vec4[] position;
};

buffer Rotation
{
	mat3[] rotation;
};

buffer Meta
{
	MetaInformation[] meta;
};


vec4 Func(vec4 p)
{
	return vec4(
		u_Sigma * (p.y - p.x),
		p.x * (u_Rho - p.z) - p.y,
		p.x * p.y - p.z * u_Beta,
		0);
}

void main()
{
	int workGroupOffset = int(gl_LocalInvocationID.x * gl_WorkGroupSize.y + gl_LocalInvocationID.y) * c_ChunkSize_x * c_ChunkSize_y;
	int workGroupEnd = workGroupOffset + c_ChunkSize_x * c_ChunkSize_y;
	
	for(int i = workGroupOffset; i < position.length() && i < workGroupEnd; i++)
	{
		position[i] = vec4(1.1 * i, 0, 0, 1);//Func(position[i]) * u_Dt;
		rotation[i] = mat3(1);
	}
}
