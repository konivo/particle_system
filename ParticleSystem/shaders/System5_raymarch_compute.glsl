#version 440
layout(local_size_x=8, local_size_y=8) in;

////////////////////////////////////////////////////////////////////////////////
//uniforms//
uniform mat4 projection_transform;
uniform mat4 modelviewprojection_transform;
uniform mat4 modelviewprojection_inv_transform;
uniform mat4 modelview_inv_transform;
uniform vec2 viewport_size;
uniform float pRayMarchStepFactor;
uniform float k1, k2, k3, k4, time;

layout(rgba32f) uniform image2D u_NormalDepth;

////////////////////////////////////////////////////////////////////////////////
//constants//
const float epsilon = 0.001;
const float nearPlaneZ = 1;

//chunk algorithm
const int c_ChunkSize_x = 4;
const int c_ChunkSize_y = 4;
const ivec2 u_ChunkSize = {c_ChunkSize_x, c_ChunkSize_y};


////////////////////////////////////////////////////////////////////////////////
//types//
//
struct
{
	vec4 pos;
	vec4 ray_dir;
	vec4 look_dir;
	vec4 ray_intr;
	vec4 x_delta;
	vec4 y_delta;
	//-1, 1 parameterization
	//view dependent parametrization
	vec2 param;
} Camera;

////////////////////////////////////////////////////////////////////////////////
//utility and library functions//
//
vec4 get_clip_coordinates (vec2 param, float imagedepth)
{
	return vec4((param * 2) - 1, imagedepth, 1);
}

ivec2 get_pixel_pos(vec2 uv, vec2 viewport)
{
	return ivec2((uv * 2 - 1) * viewport);
}

//
vec4 reproject (mat4 transform, vec4 vector)
{
	vec4 result = transform * vector;
	result /= result.w;
	return result;
}

//returns value t, where given ray intersects sphere. Only positive return values are valid
//so it computes intersection only when ray starts outside spehere and aims toward  it
float SphereRayIntersection(vec4 sphere, vec3 raycenter, vec3 rayDirection)
{
	vec3 k = raycenter - sphere.xyz;

	vec3 koef = vec3(
			dot(rayDirection,rayDirection),
			2.0*dot(rayDirection, k),
			dot(k,k) - sphere.w*sphere.w);


	float discr = koef.y*koef.y - 4.0*koef.x*koef.z;

	if(discr < 0.0)
		return -1.0;
	else
		return (-koef.y - sqrt(discr))/(2.0*koef.x);
}

//returns true when sphere contains given point
bool SphereContains(in vec4 s, in vec3 point)
{
	return length(s.xyz - point) < s.w;
}

////////////////////////////////////////////////////////////////////////////////
//random functions//
const int[] PERMUTATION_TABLE = int[](151,160,137,91,90,15,131,13);
#define PERM(i) PERMUTATION_TABLE[(i)&0x7]

vec4[] sph = vec4[](
vec4( 0.04, -0.08, 0, 0.1366834171),
vec4( -0.8, -0.88, -0.76, 0.169273743),
vec4( 0.42, -0.36, -0.08, 0.2582417582),
vec4( -0.34, -0.44, -0.4, 0.1072164948),
vec4( 0.42, -0.82, 0.18, 0.2372093023),
vec4( 0.54, -0.56, -0.26, 0.1782051282),
vec4( 0.54, 0.9, -0.06, 0.2487046632),
vec4( -0.4, 0.7, -0.92, 0.1923076923),
vec4( -0.82, -0.14, -0.18, 0.1086419753),
vec4( -0.16, 0.1, -0.38, 0.2370860927),
vec4( 0.02, 0.7, 0.98, 0.196350365),
vec4( 0.86, 0.92, -0.94, 0.2063583815),
vec4( -0.9, 0.84, 0.1, 0.2402597403),
vec4( -0.2, 0.7, 0.96, 0.2363636364),
vec4( 0.72, 0.12, 0.92, 0.1846153846),
vec4( -0.1, -0.72, 0.92, 0.1756756757),
vec4( 0.26, 0.98, -0.78, 0.1959183673),
vec4( -0.7, 0.62, -0.56, 0.2576419214));//,
/*vec4( 0.14, -0.94, 0.12, 0.4470046083),
vec4( 0.36, -0.28, 0.02, 0.4439461883),
vec4( 0.96, 0.76, 0.56, 0.4672897196),
vec4( -0.34, 0.82, 0.6, 0.3136363636),
vec4( 0.78, 0.16, 0.48, 0.3157894737),
vec4( 0.56, -0.12, -0.3, 0.2682926829),
vec4( 0.14, 0.32, 0.78, 0.2842105263),
vec4( -0.98, 0, -0.04, 0.2056074766),
vec4( -0.2, -0.28, -0.86, 0.2873563218),
vec4( 0.62, -0.96, -0.14, 0.3236714976));//,
/*vec4( 0.56, -0.56, 0.14, 0.4425531915),
vec4( 0.6, 0.38, 0.64, 0.3026315789),
vec4( 0.86, -0.1, -0.38, 0.3233082707),
vec4( -0.4, 0.96, -0.94, 0.3052631579),
vec4( 0.42, 0.9, 0.18, 0.3992673993),
vec4( -0.02, -0.14, 0.88, 0.3830508475),
vec4( -0.26, 0.26, 0.54, 0.3219178082),
vec4( 0.92, 0.18, -0.9, 0.3054662379),
vec4( 0.46, -0.66, -0.04, 0.345323741),
vec4( 0.88, -0.42, -0.36, 0.3867595819),
vec4( -0.98, 0.7, -0.48, 0.3702422145),
vec4( 1, -0.84, 0.68, 0.2664092664),
vec4( -0.06, -0.28, -0.58, 0.3293172691),
vec4( -0.24, -0.1, -0.46, 0.3234200743),
vec4( 0.48, 0.74, -0.24, 0.4086956522),
vec4( 0.34, -0.16, 0.28, 0.2666666667),
vec4( -0.66, -0.32, 0.62, 0.3826530612),
vec4( -0.42, 0.8, 0.9, 0.335),
vec4( 0.48, 0.22, 0.24, 0.4736842105),
vec4( -0.74, -0.92, -0.06, 0.2741935484),
vec4( 0.32, -0.94, 0.96, 0.3679245283),
vec4( -0.96, 0.24, -0.5, 0.25)
	);*/
	
vec3 _noiseSinOmega1a = vec3(15.25263, 31.356474, 6.45575);
float _LatticeValue(vec3 pos)
{
	return fract(sin(dot(pos * 128, _noiseSinOmega1a))* 45678.36364)*2.0 - 1.0 ;
}

float _LatticeValue(float pos)
{
	return fract(sin(pos * 45678.36364))*2.0 - 1.0 ;
}

float _LatticeValue(int pos)
{
	return fract(sin(pos * 45678.36364))*2.0 - 1.0 ;
}

float random(ivec2 p, float amount)
{
	return amount * sin(float(p.x + p.y));
}

vec2 random(ivec2 p, vec2 amount)
{
	int A = PERM(p.x) + p.y,
			B = PERM(p.x + 1) + p.y;
	return amount * vec2(A, B)/255.0;
}

vec3 random(vec3 p, vec3 amount)
{
	float A = _LatticeValue(p.x),
			B = _LatticeValue(p.y + A * 1000),
			C = _LatticeValue(p.z + B * 1000);
	return amount * vec3(_LatticeValue(p.x + C * 1000), B, C);
}

vec3 random(vec3 p)
{
	return random(p, vec3(1));
}

////////////////////////////////////////////////////////////////////////////////
//morph functions//
vec3 morph_rotate(vec3 pos)
{
	float phi = pos.y / max(length(pos.xz), 1);

//matrices are specified in column-major order

	mat3 rotmatrix = mat3(
		cos(phi), 0, sin(phi),
		0,	1,	0,
		-sin(phi), 0, cos(phi)
	);

	return rotmatrix * pos;
}

vec3 morph_mod(vec3 pos, vec3 grid)
{
	return pos - (floor(pos  / grid + 0.5))* grid;
}

vec3 SinTrans(float amp, float octave, vec3 v1)
{
	return vec3(v1.y + cos((v1.y - v1.z) * octave)/octave, v1.z + sin((v1.z - v1.x) * octave)/octave, v1.x - cos((v1.x - v1.y) * octave)/octave);
}

vec3 SinTrans(float amp, float octave, vec3 v1, vec3 center)
{
	v1 -= center;
	return vec3(v1.y + cos((v1.y - v1.z) * octave)/octave, v1.z + sin((v1.z - v1.x) * octave)/octave, v1.x - cos((v1.x - v1.y) * octave)/octave);
}

vec3 SinTrans(float amp, float octave, vec3 v1, vec3 center, vec3 plane)
{
	vec3 k = amp * sin(dot(v1 - center, plane) * octave)/octave * plane;
	return v1 + k;
}

////////////////////////////////////////////////////////////////////////////////
//sdb functions//
float sphere_sdb(vec4 sphere, vec3 pos)
{
	return length(pos - sphere.xyz) - sphere.w;
}

//
vec3 sphere_sdb_grad(vec4 sphere, vec3 pos)
{
	return normalize(pos - sphere.xyz);
}

//
float torus_sdb(float r1, float r2, vec3 pos)
{
	float d1 = (length(pos.xy) - r1);
	d1 = sqrt(d1*d1 + pos.z*pos.z) - r2;

	return d1;
}

vec3 torus_sdb_grad(float r1, float r2, vec3 pos)
{
	vec3 rs = vec3(pos.xy,0);
	rs = pos - (r1*rs)/length(rs);
	return normalize(rs);
}
	
float setofspheres_sdb(vec3 mpos)
{
	float d = 100000;
	for(int i = 0; i < sph.length();i++)
	{
		float di = sphere_sdb(sph[i]*vec4(1,1,1,1), mpos);
		d = min(d, di);
	}
	return d;
}

float spherecarvedbyspheres_sdb(vec3 mpos)
{
	float factor = 22;//abs(sin(mpos.x*2)+ 1) * 10 + 5;
  float res1 = 0.0;
  float res2 = 0.0;
	float cellSize1 = 2;
	float cellSize2 = 1;

  ivec3 p = ivec3(floor( mpos / cellSize1 ));
  vec3  f = fract( mpos / cellSize1);
	float d0 = sphere_sdb(vec4(0,0,0,30.2), mpos);
	float d1 = sphere_sdb(vec4(0,0,0,53.7), mpos);

  for( int u=0; u < 1;u+=1)
	for( int k=-1; k<=1; k++ )
  for( int j=-1; j<=1; j++ )
  for( int i=-1; i<=1; i++ )
  {
      ivec3 b = ivec3( i, j, k );
      vec3  r = vec3( b ) - f + (random(b + p + u)+ 1)/2;
      float d = cellSize1 * length( r );

      res1 += pow(d, -factor);
	}

	/*ivec3 p2 = ivec3(floor( mpos / cellSize2 ));
  vec3  f2 = fract( mpos / cellSize2);

	for( int u=0; u < 1;u+=1)
	for( int k=-1; k<=1; k++ )
  for( int j=-1; j<=1; j++ )
  for( int i=-1; i<=1; i++ )
  {
      ivec3 b = ivec3( i, j, k );
      vec3  r = vec3( b ) - f2 + (random(b + p2 + u)+ 1)/2;
      float d = cellSize2 * length( r );

      res2 += pow(d, -factor);
	}*/

	return max(d0 - /*2*pow(res2, -1/factor) -*/ 2*pow(res1, -1/factor) - k4, d1 );
}

float spherecarvedbyspheres1_sdb(vec3 mpos)
{
	float period = 6;
	float t = 4*abs(sin(fract(time/period)* 2 * 3.14)) + 8.2;
	float d = sphere_sdb(vec4(0,0,0,70.2), mpos);
	float K = 109;
	float dN = 20;//t*10;//needs to be customized with respeck to K
	vec3 mmpos = morph_mod(mpos, vec3(30, 20, 40));
	for(int i = 0; i < sph.length();i+=1)
	{
		float di = -sphere_sdb(normalize(sph[i])*vec4(13.5,13.5,13.5,12.2), mmpos);
		d = max(d, di);
	}
	mmpos = morph_mod(mpos, vec3(27, 27, 27));
	for(int i = 1; i < sph.length();i+=2)
	{
		float di = -sphere_sdb(normalize(sph[i])*vec4(10.5,10.5,10.5,9.2), mmpos);
		d = max(d, di);
	}
	mmpos = morph_mod(mpos, vec3(11, 11, 11));
	for(int i = 1; i < sph.length();i+=2)
	{
		float di = -sphere_sdb(normalize(sph[i])*vec4(3.5,3.5,3.5,4.2), mmpos);
		d = max(d, di);
	}
	return d;
}

float setofmeltedspheres_sdb(vec3 mpos)
{
	float period = 6;
	float t = 2*abs(sin(fract(time/period)* 2 * 3.14)) + 1.2;
	float d = 100000;
	float K = 109;
	float dN = 20;//t*10;//needs to be customized with respeck to K
	for(int i = 0; i < sph.length();i++)
	{
		float di = sphere_sdb(normalize(sph[i])*vec4(30.5,30.5,30.5,1.2), mpos);
		d = min(d, di);
		dN = dN - K/(di + t/2);
		//dN = min(dN, di);
		//dN = dN - K/max(di, t/2);
	}
	return min(d, dN);
}

float setofmeltedspheres2_sdb(vec3 mpos)
{
	float period = 3;
	float t = 1*sin(fract(time/period)* 2 * 3.14) + 11.2;
	float d = 100000;
	float K = 99;
	float dN = t*10;//needs to be customized with respeck to K
	for(int i = 0; i < sph.length();i++)
	{
		float di = sphere_sdb(sph[i]*vec4(1.5,1.5,1.5,0.5), mpos);
		d = min(d, di);
		
		if(di < 1)
			dN = dN - (K - di + 1);
		else
			dN = dN - K/di;
	}
	return min(d, dN);
}

float setofmeltedspheres3_sdb(vec3 mpos)
{
	float period = 6;
	float t = 8*abs(sin(fract(time/period)* 2 * 3.14)) + 6.2;
	float factor = 4;
	float res = 0;
	for(int i = 0; i < sph.length();i++)
	{
		float di = sphere_sdb(normalize(sph[i])*vec4(36.5,33.5,33.5,1.2), mpos);		
		res += pow(di, -factor);
	}
	return pow(res, -1/factor) - t;
}

float setofsqueezedspheres_sdb(vec3 mpos)
{
	float period = 6;
	float t = 4*abs(sin(fract(time/period)* 2 * 3.14)) + 6.2;
	float factor = 2.5;
	float d = sphere_sdb(vec4(0,0,0,50.2), mpos);
	float dN = 30000000;//t*10;//needs to be customized with respeck to K
	float res = 0;
	float mindi = 30000000;
	for(int i = 0; i < sph.length();i+=1)
	{
		float di = sphere_sdb(normalize(sph[i])*vec4(26.5,26.5,26.5,31.2), mpos);
		res += pow(di, -factor);
		mindi = min(mindi, di);
	}
	res -= pow(mindi, -factor);
	dN = min( mindi - pow(res, -1/factor) -0, dN);

	return dN;
	//return max(d, -dN);
}

float spherewithrelief_sdb(vec3 mpos)
{
	float period = 6;
	float t = 3.2*(sin(fract(time/period)* 2 * 3.14));
	float factor = 3;
	float d = sphere_sdb(vec4(0,0,0,22), mpos);
	float dO = sphere_sdb(vec4(0,0,0,21), mpos);
	float dN = 30000000;
	float res = 0;
	float mindi = 333330;
	for(int i = 0; i < sph.length();i+=1)
	{
		float di = sphere_sdb(normalize(sph[i])*vec4(26.5,23.5,23.5,0.1), mpos);
		res += pow(di , -factor);
		mindi = min(mindi, di);
	}
	res = min(mindi - pow(res - pow(mindi, -factor), -1/factor) + 4, dN);
	res = pow(max(d, res) , -1.5) + pow(dO, -1.5);
	return pow(res , -1/1.50) - 2.97521;
}

float spherewithrelief2_sdb(vec3 mpos)
{
	float factor = 15;//abs(sin(mpos.x*2)+ 1) * 10 + 5;
  float res = 0.0;
	float cellSize = 2;

  ivec3 p = ivec3(floor( mpos / cellSize ));
  vec3  f = fract( mpos / cellSize);
	float d0 = sphere_sdb(vec4(0,0,0,20.2), mpos);
	float d1 = sphere_sdb(vec4(0,0,0,33.7), mpos);

  for( int u=0; u < 1;u+=1)
	for( int k=-1; k<=1; k++ )
  for( int j=-1; j<=1; j++ )
  for( int i=-1; i<=1; i++ )
  {
      ivec3 b = ivec3( i, j, k );
      vec3  r = vec3( b ) - f + (random(b + p + u)+ 1)/2;
      float d = cellSize * length( r );

      res += pow(d, -factor);
	}
	return max(d0 - pow(res, -1/factor) - k4, d1 );
}

float spherewithrelief3_sdb(vec3 mpos)
{
	float cellSize = 5;
	float f_cellSize = 0.85;
	float f_norm_pow = 48;
	float f_bias = 2;
	float f_scale = 1;
	float scale = 1;
	float bias = 1;

	float res = 0.0;
  ivec3 p = ivec3(floor( mpos / cellSize ));
  vec3  f = fract( mpos / cellSize);
	float d0 = sphere_sdb(vec4(0,0,0,45.2), mpos);
	float d1 = sphere_sdb(vec4(0,0,0,53.7), mpos);
	float factor = .12;

	for( int k=-1; k<=1; k++ )
  for( int j=-1; j<=1; j++ )
  for( int i=-1; i<=1; i++ )
  {
      ivec3 b = ivec3( i, j, k );
      vec3  r = vec3( b ) - f + (random(b + p )+ 1)/2;
      float d = f_cellSize * length(r);

      factor += pow(d, -f_norm_pow);
	}
	factor = f_scale * pow(factor, -1/f_norm_pow) + f_bias;

  for( int u=0; u < 1;u+=1)
	for( int k=-1; k<=1; k++ )
  for( int j=-1; j<=1; j++ )
  for( int i=-1; i<=1; i++ )
  {
      ivec3 b = ivec3( i, j, k );
      vec3  r = vec3( b ) - f + (random(b + p + u)+ 1)/2;
      float d = cellSize * length( r );

      res += pow(d, -factor);
	}
	return max(d0 + scale*pow(res, -1/factor) - bias, d1);
}

vec3 DomainMorphFunction(vec3 pos)
{
//spiralovy posun sintrans
	vec3 center = vec3(1, 0, 0);
	vec3 v = pos;
	vec3 gridDim = vec3(15);
	vec3 v1 = pos + 5.5;
	vec3 v2 = floor(v1/ gridDim);
	vec3 v3 = ceil(v1/ gridDim);
	vec3 cellCenter = (v2 + v3)/2;
	vec3 v_t = v1/ gridDim - v2 ;
	float period = 10;
	float t = fract(time/period)* 3 + 4;
	//float smoothiness = 1./2;//0.8485;//t/period;
	float boxSize = 18;//t/5;
	
		for(int i = 0; i < 3; i++)
		{
				vec3 fr = fract(v/boxSize);
				vec3 k1 = vec3(pow(1-fr.x, t), pow(1-fr.y, t), pow(1-fr.z, t));
				vec3 k2 = vec3(pow(fr.x, t), pow(fr.y, t), pow(fr.z, t));
				vec3 k3 = vec3(0);
				vec3 k4 = vec3(0);
				v = boxSize * (floor(v/boxSize) * (k1 + k3) + ceil(v/boxSize) * (k2 + k4) + (1 - k1 - k2 - k3 -k4)* (floor(v/boxSize) + 0.5))/1;
		}


	//v = morph_mod(v, vec3(150, 150, 150));

	return v;
}

mat3 dDomainMorphFunction(vec3 pos)
{
	float phi = pos.y * 0.1;

	mat3 rotmatrix = mat3(
		cos(phi), 0, sin(phi),
		- 0.1 * sin(phi) * pos.x - 0.1 * cos(phi) * pos.z ,	1,	0.1 * cos(phi) * pos.x - 0.1 * sin(phi) * pos.z,
		- sin(phi), 0, cos(phi)
	);

	return rotmatrix;
}

//====================================================================================


float SDBValue(vec3 pos)
{
	//vec3 mpos = DomainMorphFunction(pos);
	vec3 mpos = pos;
	//return spherewithrelief_sdb(mpos) * pRayMarchStepFactor;
	return spherewithrelief_sdb(mpos) * pRayMarchStepFactor;
	//return sphere_sdb(vec4(0, 0, 0, 28), mpos) * pRayMarchStepFactor;
}

vec3 EstimateGradient(vec3 pos, float d)
{
	return
		vec3(
			SDBValue(pos + vec3(d, 0, 0)) - SDBValue(pos - vec3(d, 0, 0)),
			SDBValue(pos + vec3(0, d, 0)) - SDBValue(pos - vec3(0, d, 0)),
			SDBValue(pos + vec3(0, 0, d)) - SDBValue(pos - vec3(0, 0, d))) / (2 * d);
}

//local storage
shared vec4[gl_WorkGroupSize.x * gl_WorkGroupSize.y * c_ChunkSize_x * c_ChunkSize_y] localResult;
shared vec3[gl_WorkGroupSize.x * gl_WorkGroupSize.y * c_ChunkSize_x * c_ChunkSize_y] tracedPoints;
shared vec3[gl_WorkGroupSize.x * gl_WorkGroupSize.y * c_ChunkSize_x * c_ChunkSize_y] tracedFlags;

void newpix(inout int state, inout int cx, inout int cy, out ivec2 delta)
{	
	switch(state)
	{
/*		case 0:
			cx += (cy + 1) / u_ChunkSize.y;
			cy = (cy + 1) % u_ChunkSize.y;
			delta = ivec2(-1, -1);
			break;*/
		case 0:
			if(cx % 2 > 0)
			{
				if(cy == 0)
					delta = ivec2(-1, 0);
				else
					delta = ivec2(0, 1);
					
				if(cx == u_ChunkSize.x - 1 && cy == 0)
					state++;
					
				cx += (u_ChunkSize.y - cy) / u_ChunkSize.y;
				cy = max(cy - 1, 0);
			}
			else
			{
				if(cy == u_ChunkSize.y - 1)
					delta = ivec2(-1, 0);
				else
					delta = ivec2(0, -1);
					
				if(cx == u_ChunkSize.x - 1 && cy == u_ChunkSize.y - 1 )
					state++;
					
				cx += ++cy / u_ChunkSize.y;
				cy = min(cy, u_ChunkSize.y - 1);
			}
			break;
		case 1:
			if(cx % 2 > 0)
			{
				if(cy == u_ChunkSize.y - 1)
					delta = ivec2(1, 0);
				else
					delta = ivec2(0, -1);
					
				if(cx == 0 && cy == u_ChunkSize.y - 1 )
					state++;
					
				cx -= ++cy / u_ChunkSize.y;
				cy = min(cy, u_ChunkSize.y - 1);
			}
			else
			{
				if(cy == 0)
					delta = ivec2(1, 0);
				else
					delta = ivec2(0, 1);
					
				if(cx == 0 && cy == 0 )
					state++;
					
				cx -= (u_ChunkSize.y - cy) / u_ChunkSize.y;
				cy = min(cy - 1, 0);

			}
			break;
		case 2:
			cx = u_ChunkSize.x;
			cy = u_ChunkSize.y;
			break;
	}
}

//
void main ()
{
	//
	ivec2 size = imageSize(u_NormalDepth);
	vec2 isize = 1./imageSize(u_NormalDepth);
	vec2 param = vec2(gl_GlobalInvocationID.xy * u_ChunkSize) * isize;
	ivec2 startPixelID = ivec2(gl_GlobalInvocationID.xy) * u_ChunkSize;
	int workGroupOffset = int(gl_LocalInvocationID.x * gl_WorkGroupSize.y + gl_LocalInvocationID.y) * c_ChunkSize_x * c_ChunkSize_y;
	
	if(startPixelID.x >= size.x ||startPixelID.y >= size.y)
	{
		return;
	}
	
	//camera initialization
	Camera.pos = modelview_inv_transform * vec4(0, 0, 0, 1);
	Camera.ray_intr = reproject(modelviewprojection_inv_transform, get_clip_coordinates(param, -1));
	Camera.ray_dir = normalize(Camera.ray_intr - Camera.pos);
	Camera.x_delta = (reproject(modelviewprojection_inv_transform, get_clip_coordinates(param + vec2(isize.x, 0), -1)) - Camera.ray_intr);
	Camera.y_delta = (reproject(modelviewprojection_inv_transform, get_clip_coordinates(param + vec2(0, isize.y), -1)) - Camera.ray_intr);
	Camera.look_dir = modelview_inv_transform * vec4(0, 0, -1, 0);

	//here is expected ray with unit direction, and starting from camera origin
	int numberOfIterations = 0;
	//starting point
	vec3 tracePoint, initTracePoint, lastTracePoint;
	//test for intersection with bounds of function
	vec4 bs = vec4(0, 0, 0, 68);
  vec4 testbs = bs; testbs.w += 1;
	//
	float upperLimit = epsilon;
	float lowerLimit = -epsilon;
	//flag indicating the intersection of the ray with the implicit surface
	bool intersect = false;
	//does the camera start inside or not?
	bool startInside = SphereContains(bs, Camera.pos.xyz);
	//intersection time of currently computed ray with the bounding sphere 
	float intTime = SphereRayIntersection(bs, Camera.pos.xyz, Camera.ray_dir.xyz);
	//distance to a camera's nearplane from camera position
	float nearPlaneDist = nearPlaneZ/dot(Camera.look_dir, Camera.ray_dir);
	//
	float stepFactor = 1;
	float lastStep = 1;
	float step;
	//current ray direction	
	vec4 ray_dir;
	//current pixel id
	int cx, cy, cxystate = 0;
	ivec2 cxydelta;
	ivec2 pixelID;
	//
	vec4 result;
	vec4 projected_i;
	vec3 gradient;
	
	for(cx = 0; cx < u_ChunkSize.x; cx++)
		for(cy = 0; cy < u_ChunkSize.y; cy++)
		{
			tracedFlags[workGroupOffset + cx * u_ChunkSize.y + cy] = vec3(pow(2., 32));
		}
	cx = cy = 0;
	
	if (intTime >= 0.0 || startInside)
	{
		ray_dir = normalize(Camera.ray_intr - Camera.pos);
		
		if (!startInside) 
		{
			intTime = intTime < nearPlaneDist? nearPlaneDist: intTime;
			lastTracePoint = tracePoint = Camera.pos.xyz + ray_dir.xyz * intTime;
		}
		//start from near camera plane
		else 
		{
			lastTracePoint = tracePoint = Camera.pos.xyz + ray_dir.xyz * nearPlaneDist;
		}
		
		while(cx < u_ChunkSize.x && cy < u_ChunkSize.y)
		{
			if(!SphereContains(testbs, tracePoint) || numberOfIterations > k1 || intersect)
			{
				//store the result into the a local storage
				tracedPoints[workGroupOffset + cx * u_ChunkSize.y + cy] = tracePoint;
				tracedFlags[workGroupOffset + cx * u_ChunkSize.y + cy] = vec3(step, lastStep, numberOfIterations);
				
				//move to next pixel
				newpix(cxystate, cx, cy, cxydelta);
				ray_dir = normalize(cx * Camera.x_delta + cy * Camera.y_delta + (Camera.ray_intr - Camera.pos));
				
				float k = dot(Camera.look_dir.xyz, tracedPoints[workGroupOffset] - Camera.pos.xyz)/dot(Camera.look_dir, Camera.ray_dir);
				ivec2 prevcxy = clamp(ivec2(cx, cy) + cxydelta, ivec2(0), u_ChunkSize - 1);
				ivec2 deltacxy = ivec2(cx, cy) - prevcxy;
				vec3 xy_delta = Camera.y_delta.xyz * deltacxy.y * k + Camera.x_delta.xyz * deltacxy.x * k;
				
				step = tracedFlags[workGroupOffset + cx * u_ChunkSize.y + cy].x;
				lastStep = tracedFlags[workGroupOffset + cx * u_ChunkSize.y + cy].y;
				tracePoint = tracedPoints[workGroupOffset + cx * u_ChunkSize.y + cy];
				lastTracePoint = tracePoint;
					
				float pstep = tracedFlags[workGroupOffset + prevcxy.x * u_ChunkSize.y + prevcxy.y].x;
				if(abs(step) > abs(pstep))
				{			
					tracePoint = tracedPoints[workGroupOffset + prevcxy.x * u_ChunkSize.y + prevcxy.y] + xy_delta - ray_dir.xyz * 0.01;
					lastTracePoint = tracePoint + ray_dir.xyz * 0.3;					
					step = tracedFlags[workGroupOffset + prevcxy.x * u_ChunkSize.y + prevcxy.y].x;
					lastStep = tracedFlags[workGroupOffset + prevcxy.x * u_ChunkSize.y + prevcxy.y].y;
					//step = SDBValue(tracePoint);
					//lastStep = SDBValue(lastTracePoint);
				}
				
				stepFactor = 1;				
				intersect = false;
				numberOfIterations = 0;
			}
			
			numberOfIterations++;

			// traverse along the ray until an intersection is found
			//at each successive step, step length is given by F(x)/lambda
			//morphFunction = DomainMorphFunction(tracePoint);
			//float step = SDBValue(morphFunction * tracePoint);
			//step /= length(dDomainMorphFunction(tracePoint) * Camera.ray_dir.xyz);
			

			/////////////////////////////////////////////
			//backward movement strategy
			/*if(step < 0)
			{	
				stepFactor *= k2/10;
			}
			//revert back to normal
			else if(stepFactor < 1)
			{
				stepFactor = stepFactor * 1.8;
			}
			stepFactor = clamp(stepFactor, 0.1, 1.);
			tracePoint += ray_dir.xyz * step * stepFactor;
			float intTimeOffset = _LatticeValue(tracePoint) * 0.0002194105321041013013001272034252352;
			step = SDBValue(tracePoint) - abs(intTimeOffset);*/
			//step = SDBValue(tracePoint);
			
			
			/////////////////////////////////////////////
			//binary search movement strategy
			//float intTimeOffset = _LatticeValue(tracePoint) * 0.0000194105321041013013001272034252352;
			//step = SDBValue(tracePoint) - abs(intTimeOffset);
			/*if(step < 0 && lastStep > 0 || step > 0 && lastStep < 0)
			{
				vec3 mp = k2/ 10 * tracePoint + (10 - k2)/10 * lastTracePoint;
				float mp_step = SDBValue(mp);
				vec2 s = sign(vec2(mp_step, mp_step) * vec2(step, lastStep));
				
				if(s.x < 0)
				{
					lastStep = mp_step;
					lastTracePoint = mp;
				}
				else
				{
					step = mp_step;
					tracePoint = mp;
				}
			}
			else
			{
				lastTracePoint = tracePoint;
				lastStep = step;
				
				tracePoint += ray_dir.xyz * step * stepFactor;
				step = SDBValue(tracePoint);
			}*/
			
			/////////////////////////////////////////////
			//simple 'sphere' tracing
			tracePoint += ray_dir.xyz * step * stepFactor;
			float intTimeOffset = _LatticeValue(tracePoint) * 0.0000194105321041013013001272034252352;
			step = SDBValue(tracePoint) - abs(intTimeOffset);
			
			//signal end
			intersect = step < upperLimit && step > lowerLimit;
		}
	}
	
	for(cx = 0; cx < u_ChunkSize.x; cx++)
		for(cy = 0; cy < u_ChunkSize.y; cy++)
		{
			step = tracedFlags[workGroupOffset + cx * u_ChunkSize.y + cy].x;
			
			if(step < upperLimit && step > lowerLimit)
			{
				tracePoint = tracedPoints[workGroupOffset + cx * u_ChunkSize.y + cy];
				numberOfIterations = int(tracedFlags[workGroupOffset + cx * u_ChunkSize.y + cy].z);
				
				gradient = EstimateGradient(tracePoint, 0.01);
				projected_i = reproject(modelviewprojection_transform, vec4(tracePoint, 1));
				result = vec4(normalize(gradient) * 0.5f + 0.5f, (projected_i.z + 1) * 0.5);
				//
				//result = vec4(normalize(gradient) * 0.5f + 0.5f, numberOfIterations/5.0);
			}
			else
			{
				result = vec4(0, 0, 0, 0);
			}
			
			pixelID = ivec2(gl_GlobalInvocationID.xy * u_ChunkSize) + ivec2(cx, cy);
			imageStore(u_NormalDepth, pixelID, result);
		}
}
