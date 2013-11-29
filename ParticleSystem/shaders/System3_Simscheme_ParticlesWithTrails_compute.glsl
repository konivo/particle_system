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

uniform float u_Dt = 0.005;

////////////////////////////////////////////////////////////////////////////////
//constants//

//chunk algorithm
const int c_ChunkSize_x = 4;
const int c_ChunkSize_y = 1;
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

buffer MapParameters
{
	float[] a;
};

buffer Meta
{
	MetaInformation[] meta;
};


vec4 Func(vec4 p)
{
	return vec4(
		a[0] * (p.y - p.x),
		p.x * (a[1] - p.z) - p.y,
		p.x * p.y - p.z * a[2],
		0);
}

void main()
{
	int workGroupOffset = int(gl_GlobalInvocationID.x) * c_ChunkSize_x * c_ChunkSize_y;
	int workGroupEnd = min(workGroupOffset + c_ChunkSize_x * c_ChunkSize_y, position.length());
	
	for(int i = workGroupOffset; i < workGroupEnd; i++)
	{
		position[i] += Func(position[i]) * u_Dt;
		rotation[i] = mat3(1);
	}
}
