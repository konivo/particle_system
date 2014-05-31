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

subroutine vec4 Map(vec4 p);
//subroutine void SimulationScheme(int firsttrail, int lasttrail, int trailsize);

////////////////////////////////////////////////////////////////////////////////
//uniforms//
uniform float u_Dt = 0.005;
uniform int u_TrailSize = 100;
uniform int u_TrailBundleSize = 1;
uniform int u_StepsPerFrame = 1;
uniform float u_ParticleScale = 600;
subroutine uniform Map u_Map;
//subroutine uniform SimulationScheme u_SimulationScheme;

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

////////////////////////////////////////////////////////////////////////////////
//map subroutines//

///////////////
subroutine(Map)
vec4 Func(vec4 p)
{
	return vec4(
		a[0] * (p.y - p.x),
		p.x * (a[1] - p.z) - p.y,
		p.x * p.y - p.z * a[2],
		0);
}

///////////////
subroutine(Map)
vec4 Lorenz(vec4 p)
{
	return vec4(
		a[0] * (p.y - p.x),
		p.x * (a[1] - p.z) - p.y,
		p.x * p.y - p.z * a[2],
		0);
}


///////////////
subroutine(Map)
vec4 HopfMap(vec4 p)
{
	float A = a[0];
	float a = a[1];
	float x = p.x;
	float y = p.y;
	float z = p.z;
	float k = A * pow(dot(p, p), -2);
	
	vec4 result = 
	{
		k * 2 * (-a * y + x *z),
		k * 2 * (a *x + y * z),
		k * (a * a - x * x - y * y + z * z),
		0
	};
	
	return result;
}

///////////////
vec4 SpiralBMapInternal(float k, float acc, vec4 center, vec4 vin, vec4 vout)
{
	vec4 tmp = vin - center;
	float dist = length(tmp);

	for(int i = 0; i < 3; i++)
	{
		//if(float.IsNaN(tmp.X) || float.IsNaN(tmp.Y))
			//break;

		vec4 d = 
			normalize(vec4(
				k * tmp.y,
				-k * tmp.x,
				1,
				-0
			));

		d *= 1f/max(pow(dist, 0.5), 0.1f);
		vout += acc * d;
		tmp = vec4(tmp.zxy,0);
		vout = vec4(vout.zxy, 0);
	}
	
	return vout;
}

subroutine(Map)
vec4 SpiralBMap(vec4 vin)
{
	//
	const int c_CenterParamCount = 6;
	const int c_CenterCount = a.length()/ c_CenterParamCount;
	
	vec4 o = vec4(0, 0, 0, 0);
	for(int i = 0; i < a.length(); i+= c_CenterParamCount)
	{
		vec4 center = vec4(a[i], a[i + 1], a[i + 2], 0);
		o = SpiralBMapInternal(a[i + 3], a[i + 4], center, vin, o);
	}

	o /= c_CenterCount * 3;
	return o;
}

///////////////
vec4 Swirl2DMapInternal(float k, float acc, vec4 center, vec4 vin)
{
	vec4 tmp = vin - center;
	float dist = length(tmp.xy);

	vec4 d = 
		normalize(vec4(
			tmp.y,
			-tmp.x,
			0,
			0
		));

	d *= 1f/max(pow(dist, k), 0.1f);
	return acc * d;
}

subroutine(Map)
vec4 Swirl2DMap(vec4 vin)
{
	//
	const int c_CenterParamCount = 6;
	const int c_CenterCount = a.length()/ c_CenterParamCount;
	
	vec4 o = vec4(0, 0, 0, 0);
	for(int i = 0; i < a.length(); i+= c_CenterParamCount)
	{
		vec4 center = vec4(a[i], a[i + 1], a[i + 2], 0);
		o += Swirl2DMapInternal(a[i + 3], a[i + 4] * a[i + 5], center, vin);
	}

	o /= c_CenterCount * 3;
	return o;
}

///////////////
vec4 Swirl3DMapInternal(float k, float acc, vec4 center, vec4 n, vec4 vin)
{
	vec4 tmp = vin - center;
	float dist = length(tmp.xyz);
	
	vec3 d = cross (tmp.xyz, n.xyz);
	d *= 1/length(d) * acc/max(pow(dist, k), 0.1);
	return vec4(d, 0);
}

subroutine(Map)
vec4 Swirl3DMap(vec4 vin)
{
	//
	const int c_CenterParamCount = 9;
	const int c_CenterCount = a.length()/ c_CenterParamCount;
	
	vec4 o = vec4(0, 0, 0, 0);
	for(int i = 0; i < a.length(); i+= c_CenterParamCount)
	{
		vec4 center = vec4(a[i], a[i + 1], a[i + 2], 0);
		vec4 n = vec4(a[i + 3], a[i + 4], a[i + 5], 0);
		o += Swirl3DMapInternal(a[i + 6], a[i + 7] * a[i + 8], n, center, vin);
	}

	o /= c_CenterCount * 3;
	return o;
}

////////////////////////////////////////////////////////////////////////////////
//SimulationScheme subroutines//

///////////////
//subroutine(SimulationScheme)
void SimulationSchemeCubicTrail(int firsttrail, int lasttrail, int trailsize)
{
	int particleCount = position.length();
	int bundleSize = lasttrail - firsttrail;
	float size = 0f;
	vec4 dp = vec4(0);
	vec4 dpA = vec4(0);
	vec4 dpB = vec4(0);
	vec4 delta2 = vec4(0);;
	vec4 middlepoint = vec4(0);;
	vec4 endpoint = vec4(0);;

	for (int i = firsttrail ; i < lasttrail ; i++)
	{
		//i is the trail's first element
		int pi = i + meta[i].Leader;
		float K = u_Dt;

		size = max(meta[pi].Size, 0.0001f);
		dp = u_Map(position[pi]);

		//
		vec4 b0 = vec4( dp.xyz, 0);
		vec4 b2 = vec4( cross( b0.xyz, rotation[pi][1].xyz), 0);
		vec4 b1 = vec4( cross( b2.xyz, b0.xyz), 0);

		b0 = normalize(b0);
		b1 = normalize(b1);
		b2 = normalize(b2);

		//limit the delta by the particle's size to ensure they remain 'together'
		K *= min(1, 10 * (size * u_ParticleScale)/ (length(dp) * u_Dt));
		K *= 0.5f;
		dp *= K;

		//
		float localCount = ceil(length(dp) / (size * u_ParticleScale));
		localCount = min(localCount, trailsize);
		
		//
		dpA = 2 * dp;
		middlepoint = position[pi] + dp;
		dpB = u_Map (middlepoint);
		dpB *= K;
		endpoint = middlepoint + dpB;
		dpB = u_Map (endpoint);
		dpB *= 2 * K;

		for(int li = 0; li < localCount; li++)
		{
			meta[i].Leader = (meta[i].Leader + bundleSize) % (trailsize * bundleSize);

			int ii = i + meta[i].Leader;
			if (ii >= particleCount)
			{
				ii = i;
				meta[i].Leader = 0;
			}

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
				
			attribute1[ii] = b0;
		}
	}
	/*	
	for(int pi = firsttrail; pi < min(firsttrail + trailsize * bundleSize, particleCount); pi++)
	{
		dp = u_Map(position[pi]);
		float K = u_Dt;
		//
		vec4 b0 = vec4( dp.xyz, 0);
		vec4 b2 = vec4( cross( b0.xyz, rotation[pi][1].xyz), 0);
		vec4 b1 = vec4( cross( b2.xyz, b0.xyz), 0);

		b0 = normalize(b0);
		b1 = normalize(b1);
		b2 = normalize(b2);
		dp *= K;
		
		position[pi] = position[pi] + dp;
		rotation[pi] = mat4(b0, b1, b2, vec4(0,0,0,1));
	}*/
}

///////////////
//subroutine(SimulationScheme)
void SimulationSchemeCubicTrailMoveAll(int firsttrail, int lasttrail, int trailsize)
{
	int particleCount = position.length();
	int bundleSize = lasttrail - firsttrail;
	float size = 0f;
	vec4 dp = vec4(0);
	vec4 dpA = vec4(0);
	vec4 dpB = vec4(0);
	vec4 delta2 = vec4(0);;
	vec4 middlepoint = vec4(0);;
	vec4 endpoint = vec4(0);;

	for (int i = firsttrail ; i < lasttrail ; i++)
	{
		//i is the trail's first element
		int pi = i + meta[i].Leader;
		float K = u_Dt;

		size = max(meta[pi].Size, 0.0001f);
		dp = u_Map(position[pi]);

		//
		vec4 b0 = vec4( dp.xyz, 0);
		vec4 b2 = vec4( cross( b0.xyz, rotation[pi][1].xyz), 0);
		vec4 b1 = vec4( cross( b2.xyz, b0.xyz), 0);

		b0 = normalize(b0);
		b1 = normalize(b1);
		b2 = normalize(b2);

		//limit the delta by the particle's size to ensure they remain 'together'
		K *= min(1, 10 * (size * u_ParticleScale)/ (length(dp) * u_Dt));
		K *= 0.5f;
		dp *= K;

		//
		float localCount = ceil(length(dp) / (size * u_ParticleScale));
		localCount = min(localCount, trailsize);
		
		//
		dpA = 2 * dp;
		middlepoint = position[pi] + dp;
		dpB = u_Map (middlepoint);
		dpB *= K;
		endpoint = middlepoint + dpB;
		dpB = u_Map (endpoint);
		dpB *= 2 * K;

		for(int li = 0; li < localCount; li++)
		{
			meta[i].Leader = (meta[i].Leader + bundleSize) % (trailsize * bundleSize);

			int ii = i + meta[i].Leader;
			if (ii >= particleCount)
			{
				ii = i;
				meta[i].Leader = 0;
			}

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
				
			attribute1[ii] = b0;
		}
	}
	
	for(int pi = firsttrail; pi < min(firsttrail + trailsize * bundleSize, particleCount); pi++)
	{
		dp = u_Map(position[pi]);
		float K = u_Dt;
		//
		vec4 b0 = vec4( dp.xyz, 0);
		vec4 b2 = vec4( cross( b0.xyz, rotation[pi][1].xyz), 0);
		vec4 b1 = vec4( cross( b2.xyz, b0.xyz), 0);

		b0 = normalize(b0);
		b1 = normalize(b1);
		b2 = normalize(b2);
		dp *= K;
		
		position[pi] = position[pi] + dp;
		rotation[pi] = mat4(b0, b1, b2, vec4(0,0,0,1));
	}
}

///////////////
//subroutine(SimulationScheme)
void SimulationSchemeCubicStillRoot(int firsttrail, int lasttrail, int trailsize)
{
	int particleCount = position.length();
	int bundleSize = lasttrail - firsttrail;
	float size = 0f;
	vec4 dp = vec4(0);
	vec4 dpA = vec4(0);
	vec4 dpB = vec4(0);
	vec4 delta2 = vec4(0);;
	vec4 middlepoint = vec4(0);;
	vec4 endpoint = vec4(0);;

	for (int j = 1; j < trailsize; j++)
	for (int i = firsttrail ; i < lasttrail ; i++)
	{
		//i is the trail's first element
		int pi = i + (j - 1) * bundleSize;
		float K = u_Dt;

		size = max(meta[pi].Size, 0.0001f);
		dp = u_Map(position[pi]);

		//
		vec4 b0 = vec4( dp.xyz, 0);
		vec4 b2 = vec4( cross( b0.xyz, rotation[pi][1].xyz), 0);
		vec4 b1 = vec4( cross( b2.xyz, b0.xyz), 0);

		b0 = normalize(b0);
		b1 = normalize(b1);
		b2 = normalize(b2);

		//limit the delta by the particle's size to ensure they remain 'together'
		K *= min(1, 10 * (size * u_ParticleScale)/ (length(dp) * u_Dt));
		K *= 0.5f;
		dp *= K;

		position[pi + bundleSize] = dp + position[pi];
		dimension[pi + bundleSize] = vec4 (size, size, size, size);
		rotation[pi + bundleSize] = mat4 (b0, b1, b2, vec4(0,0,0,1));			
		attribute1[pi + bundleSize] = b0;
	}
}

///////////////
//subroutine(SimulationScheme)
void SimulationSchemeRope2PhaseStillRoot(int firsttrail, int lasttrail, int trailsize)
{
	int particleCount = position.length();
	int bundleSize = lasttrail - firsttrail;
	float size = 0f;
	vec4 dp = vec4(0);
	vec4 dpA = vec4(0);
	vec4 dpB = vec4(0);
	vec4 delta2 = vec4(0);
	vec4 middlepoint = vec4(0);
	vec4 endpoint = vec4(0);
	
	for(int t = firsttrail; t < lasttrail; t++)
	{
		int upperBound = min(firsttrail + trailsize * bundleSize, particleCount);
		dp = u_Map(position[t]);
		size = 0.0004f;//max(meta[pi].Size, 0.0001f);
		
		//
		vec4 b0 = vec4( dp.xyz, 0);
		vec4 b2 = vec4( cross( b0.xyz, rotation[t][1].xyz), 0);
		vec4 b1 = vec4( cross( b2.xyz, b0.xyz), 0);

		b0 = normalize(b0);
		b1 = normalize(b1);
		b2 = normalize(b2);
		
		position[t] = position[t] + dp * u_Dt;
		//dimension[t] = vec4 (0, 0, 0, 0);
		rotation[t] = mat4 (b0, b1, b2, vec4(0,0,0,1));
		attribute1[t] = b0;
		
		for(int i = t + bundleSize ; i < upperBound; i += bundleSize)
		{
			int prev = max(i - bundleSize, t);
			int next = min(i + bundleSize, upperBound - 1);
			
			vec4 lp = position[prev] - position[i];
			vec4 ln = position[next] - position[i];
			
			float lenp = min(length(lp), 10000);
			float lenn = min(length(ln), 10000);				
			
			vec4 fp = pow(lenp,4) *  (lenp > 0? 0.5 * normalize(lp): vec4(0));
			vec4 fn = pow(lenn,4) * (lenn > 0? 0.5 * normalize(ln): vec4(0));
			
			vec4 delta = u_Dt * vec4(fp.xyz + fn.xyz, 0);
			if(lenp > 10000)
			{
				
			}
			else if(lenp > 15)
			{
				position[i] = position[prev];
				rotation[i] = mat4 (b0, b1, b2, vec4(0,0,0,1));
				attribute1[i] = b0;
			}
			else if(lenp > .291)
			{
				dp = lp * dot(lp, dp)/pow(lenp, 2);
				//
				vec4 b0 = vec4( dp.xyz, 0);
				vec4 b2 = vec4( cross( b0.xyz, rotation[i][1].xyz), 0);
				vec4 b1 = vec4( cross( b2.xyz, b0.xyz), 0);
		
				b0 = normalize(b0);
				b1 = normalize(b1);
				b2 = normalize(b2);
				
				position[i] = position[i] + 1.3 * dp * u_Dt;
				rotation[i] = mat4 (b0, b1, b2, vec4(0,0,0,1));
				attribute1[i] = b0;
			}
			else
			{
				dp = u_Map(position[i]);
				//
				vec4 b0 = vec4( dp.xyz, 0);
				vec4 b2 = vec4( cross( b0.xyz, rotation[i][1].xyz), 0);
				vec4 b1 = vec4( cross( b2.xyz, b0.xyz), 0);
		
				b0 = normalize(b0);
				b1 = normalize(b1);
				b2 = normalize(b2);
				
				position[i] = position[i] + 1.0 * dp * u_Dt;
				rotation[i] = mat4 (b0, b1, b2, vec4(0,0,0,1));
				attribute1[i] = b0;
			}
			dimension[i] = vec4 (size, size, size, size);
		}
	}
}

///////////////
//subroutine(SimulationScheme)
void SimulationSchemeRope2Phase(int firsttrail, int lasttrail, int trailsize)
{
	int particleCount = position.length();
	int bundleSize = lasttrail - firsttrail;
	float size = 0f;
	vec4 dp = vec4(0);
	vec4 dpA = vec4(0);
	vec4 dpB = vec4(0);
	vec4 delta2 = vec4(0);
	vec4 middlepoint = vec4(0);
	vec4 endpoint = vec4(0);

	/*for(int pi = firsttrail; pi < min(firsttrail + trailsize * bundleSize, particleCount); pi++)
	{
		float K = u_Dt;
		size = 0.0004f;//max(meta[pi].Size, 0.0001f);		
		
		dp = SpiralBMap(position[pi]);

		//
		vec4 b0 = vec4( dp.xyz, 0);
		vec4 b2 = vec4( cross( b0.xyz, rotation[pi][1].xyz), 0);
		vec4 b1 = vec4( cross( b2.xyz, b0.xyz), 0);

		b0 = normalize(b0);
		b1 = normalize(b1);
		b2 = normalize(b2);
		
		position[pi] = position[pi] + dp * K;
		dimension[pi] = vec4 (size, size, size, size);
		rotation[pi] = mat4 (b0, b1, b2, vec4(0,0,0,1));
		attribute1[pi] = b0;
	}
	*/
	for(int t = firsttrail; t < lasttrail; t++)
	{
		int upperBound = min(firsttrail + trailsize * bundleSize, particleCount);
		dp = SpiralBMap(position[t]);
		size = 0.0004f;//max(meta[pi].Size, 0.0001f);
		
		//
		vec4 b0 = vec4( dp.xyz, 0);
		vec4 b2 = vec4( cross( b0.xyz, rotation[t][1].xyz), 0);
		vec4 b1 = vec4( cross( b2.xyz, b0.xyz), 0);

		b0 = normalize(b0);
		b1 = normalize(b1);
		b2 = normalize(b2);
		
		position[t] = position[t] + dp * u_Dt;
		dimension[t] = vec4 (size, size, size, size);
		rotation[t] = mat4 (b0, b1, b2, vec4(0,0,0,1));
		attribute1[t] = b0;
		
		for(int i = t + bundleSize ; i < upperBound; i += bundleSize)
		{
			int prev = max(i - bundleSize, t);
			int next = min(i + bundleSize, upperBound - 1);
			
			vec4 lp = position[prev] - position[i];
			vec4 ln = position[next] - position[i];
			
			float lenp = min(length(lp), 10000);
			float lenn = min(length(ln), 10000);				
			
			vec4 fp = pow(lenp,4) *  (lenp > 0? 0.5 * normalize(lp): vec4(0));
			vec4 fn = pow(lenn,4) * (lenn > 0? 0.5 * normalize(ln): vec4(0));
			
			vec4 delta = u_Dt * vec4(fp.xyz + fn.xyz, 0);
			if(lenp > 10000)
			{
				
			}
			else if(lenp > 5)
			{
				position[i] = position[prev];
				rotation[i] = mat4 (b0, b1, b2, vec4(0,0,0,1));
				attribute1[i] = b0;
			}
			else if(lenp > .951)
			{
				dp = lp * dot(lp, dp)/pow(lenp, 2);
				//
				vec4 b0 = vec4( dp.xyz, 0);
				vec4 b2 = vec4( cross( b0.xyz, rotation[i][1].xyz), 0);
				vec4 b1 = vec4( cross( b2.xyz, b0.xyz), 0);
		
				b0 = normalize(b0);
				b1 = normalize(b1);
				b2 = normalize(b2);
				
				position[i] = position[i] + 1.3 * dp * u_Dt;
				rotation[i] = mat4 (b0, b1, b2, vec4(0,0,0,1));
				attribute1[i] = b0;
			}
			else
			{
				dp = SpiralBMap(position[i]);
				//
				vec4 b0 = vec4( dp.xyz, 0);
				vec4 b2 = vec4( cross( b0.xyz, rotation[i][1].xyz), 0);
				vec4 b1 = vec4( cross( b2.xyz, b0.xyz), 0);
		
				b0 = normalize(b0);
				b1 = normalize(b1);
				b2 = normalize(b2);
				
				position[i] = position[i] + 1.0 * dp * u_Dt;
				rotation[i] = mat4 (b0, b1, b2, vec4(0,0,0,1));
				attribute1[i] = b0;
			}
			dimension[i] = vec4 (size, size, size, size);
		}
	}
}

////////////////////////////////////////////////////////////////////////////////
//kernel//
void main(){
	int particleCount = position.length();
	
	int trailSize = max(u_TrailSize, 1);
	int trailCount = (particleCount + trailSize - 1) / trailSize;
	int trailBundleSize = max(u_TrailBundleSize, 1);
	int trailBundleCount = (trailCount + trailBundleSize - 1) / trailBundleSize;

	int stepsPerFrame = max(u_StepsPerFrame, 1);
	float particleScale = u_ParticleScale;
	
	int firsttrail = int(gl_GlobalInvocationID.x) * trailBundleSize * trailSize;
	int lasttrail = min(firsttrail + trailBundleSize, particleCount + 1);
	
	for (int j = 0; j < stepsPerFrame; j++)
	{
		SimulationSchemeCubicTrailMoveAll(firsttrail, lasttrail, trailSize);
	}
}
