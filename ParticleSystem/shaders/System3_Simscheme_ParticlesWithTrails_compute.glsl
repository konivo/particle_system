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
uniform float u_Dt = 0.005;
uniform int u_TrailSize = 100;
uniform int u_TrailBundleSize = 1;
uniform int u_StepsPerFrame = 1;
uniform float u_ParticleScale = 600;

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
	mat4[] rotation;
};

buffer Dimension
{
	vec4[] dimension;
};

buffer Attribute1
{
	vec4[] attribute1;
};

buffer MapParameters
{
	float[] a;
};

layout(std140) buffer Meta
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
	int particleCount = position.length();
	
	int trailSize = max(u_TrailSize, 1);
	int trailCount = (particleCount + trailSize - 1) / trailSize;
	int trailBundleSize = max(u_TrailBundleSize, 1);
	int trailBundleCount = (trailCount + trailBundleSize - 1) / trailBundleSize;

	int stepsPerFrame = max(u_StepsPerFrame, 1);
	float particleScale = u_ParticleScale;
	
	int firsttrail = int(gl_GlobalInvocationID.x) * trailBundleSize * trailSize;
	int lasttrail = min(firsttrail + trailBundleSize, particleCount);
	
	vec4 dp = vec4(0);
	float size = 0f;
	vec4 dpA = vec4(0);
	vec4 dpB = vec4(0);
	vec4 delta2 = vec4(0);;
	vec4 middlepoint = vec4(0);;
	vec4 endpoint = vec4(0);;

	for (int j = 0; j < stepsPerFrame; j++)
	{
		for (int i = firsttrail ; i < lasttrail ; i++)
		{
			//i is the trail's first element
			int pi = i + meta[i].Leader;
			float K = u_Dt;

			size = max(meta[pi].Size, 0.0001f);
			dp = Func (position[pi]);

			//
			vec4 b0 = vec4( dp.xyz, 0);
			vec4 b2 = vec4( cross( b0.xyz, rotation[pi][1].xyz), 0);
			vec4 b1 = vec4( cross( b2.xyz, b0.xyz), 0);

			b0 = normalize(b0);
			b1 = normalize(b1);
			b2 = normalize(b2);

			//
			//if(IntegrationStep == IntegrationStepType.LimitDelta)
			{
				K *= min(1, 10 * (size * particleScale)/ (length(dp) * u_Dt));
			}

			//if(Interpolation == InterpolationType.Cubic)
				K *= 0.5f;

			dp *= K;

			//
			float localCount = ceil(length(dp) / (size * particleScale));
			localCount = min(localCount, trailSize);
			//if(Interpolation == InterpolationType.Cubic)
			{
				dpA = 2 * dp;
				middlepoint = position[pi] + dp;
				dpB = Func (middlepoint);
				dpB *= K;
				endpoint = middlepoint + dpB;

				dpB = Func (endpoint);
				dpB *= 2 * K;
			}

			for(int li = 0; li < localCount; li++)
			{
				meta[i].Leader = (meta[i].Leader + trailBundleSize) % (trailSize * trailBundleSize);

				int ii = i + meta[i].Leader;
				if (ii >= particleCount)
				{
					ii = i;
					meta[i].Leader = 0;
				}

				//if(Interpolation == InterpolationType.Cubic)
				{
					float t = (1 + li) / localCount;
					float p1 = 2*t*t*t - 3*t*t + 1;
					float p2 = t*t*t - 2*t*t + t;
					float p3 = -p1 + 1;
					float p4 = p2 + t*t - t;

					position[ii] =
						p1 *  position[pi] +
						p2 * dpA +
						p3 * endpoint +
						p4 * dpB;

					dimension[ii] = vec4 (size, size, size, size);
					rotation[ii] = mat4 (b0, b1, b2, vec4(0,0,0,1));
				}
				/*else
				{
					position[ii] = position[pi] + ((1 + li) / localCount) * dp;
					dimension[ii] = vec4 (size, size, size, size);
					rotation[ii] = mat4(b0, b1, b2, vec4(0,0,0,1));
				}*/

				//switch (ComputeMetadataMode) {
				//case ComputeMetadata.Speed:
					//attribute1[ii] = dp;
				//break;
				//case ComputeMetadata.Tangent:
					attribute1[ii] = b0;
				//break;
				//default:
				//break;
				//}
			}
		}
	}
}
