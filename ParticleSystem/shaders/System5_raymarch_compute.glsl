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

//chunk algorithm
uniform ivec2 u_ChunkSize = {4, 4};
 
layout(rgba32f) uniform image2D u_NormalDepth;

////////////////////////////////////////////////////////////////////////////////
//constants//
const float epsilon = 0.001;
const float nearPlaneZ = 1;


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
vec4( 0.62, -0.96, -0.14, 0.3236714976),
vec4( 0.56, -0.56, 0.14, 0.4425531915),
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
	float t = 4*abs(sin(fract(time/period)* 2 * 3.14)) + 6.2;
	float d = sphere_sdb(vec4(0,0,0,70.2), mpos);
	float K = 109;
	float dN = 20;//t*10;//needs to be customized with respeck to K
	vec3 mmpos = morph_mod(mpos, vec3(40, 40, 40));
	for(int i = 0; i < sph.length();i+=2)
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
	float t = 4*abs(sin(fract(time/period)* 2 * 3.14)) + 6.2;
	float d = 100000;
	float K = 109;
	float dN = 20;//t*10;//needs to be customized with respeck to K
	for(int i = 0; i < sph.length();i++)
	{
		float di = sphere_sdb(normalize(sph[i])*vec4(70.5,50.5,70.5,4.2), mpos);
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
	float t = 8*abs(sin(fract(time/period)* 2 * 3.14)) + 12.2;
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
	float factor = 19;
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
	res = min(mindi - pow(res - pow(mindi, -factor), -1/factor) + 8, dN);
	res = pow(max(d, res) , -1.5) + pow(dO, -1.5);
	return pow(res , -1/1.50) - 4.97521;
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
	vec3 center = vec3(0, 0, 0);
	vec3 v = pos;//.zxy;
	
	for(int i = 0; i < 3; i++)
	{
		vec3 temp = v;
		//v = SinTrans(0, 1, v, center, normalize(morph_rotate(v)));
		//v = SinTrans(0, 1, v, morph_rotate(center), normalize(v));
		//v = SinTrans(0, 1, v, morph_rotate(center), normalize(morph_rotate(v)));
		//center = temp;

		v = SinTrans(0, 1, v, center, normalize(v));
		//v = SinTrans(0, 1, v, center, normalize(morph_rotate(v))) -	morph_rotate(center) * 0.1f;
		//v = SinTrans(5  , 1, v, center, normalize(v)) -	morph_rotate(v* 0.1f) ;
		center = temp;

		//v = SinTrans(0, 1, v, center, normalize(v));
		//center = morph_rotate(temp);
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
	vec3 mpos = DomainMorphFunction(pos);
	//vec3 mpos = pos;
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

//
void main ()
{
	//
	ivec2 size = imageSize(u_NormalDepth);
	vec2 isize = 1./imageSize(u_NormalDepth);
	vec2 param = vec2(gl_GlobalInvocationID.xy * u_ChunkSize) * isize;
	ivec2 startPixelID = ivec2(gl_GlobalInvocationID.xy) * u_ChunkSize;
	
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
	vec3 tracePoint, gradient, initTracePoint, lastTracePoint;

	//test for intersection with bounds of function
	vec4 bs = vec4(0, 0, 0, 2000);
  vec4 testbs = bs;
  testbs.w += 1;

	float intTime = 0;
	float upperLimit = epsilon;
	float lowerLimit = -epsilon;

	bool intersect = false;
	bool startInside = SphereContains(bs, Camera.pos.xyz);;
	
	//distance to a camera's nearplane from camera position
	float nearPlaneDist = nearPlaneZ/dot(Camera.look_dir, Camera.ray_dir);

	//transformation into sdb function domain
	mat3 morphFunction;

	for(int cx = 0; cx < u_ChunkSize.x; cx++)
	for(int cy = 0; cy < u_ChunkSize.y; cy++)
	{
		vec4 ray_dir = normalize(cx * Camera.x_delta + cy * Camera.y_delta + (Camera.ray_intr - Camera.pos));
		ivec2 pixelID = ivec2(gl_GlobalInvocationID.xy * u_ChunkSize) + ivec2(cx, cy);
		
		intTime = SphereRayIntersection(bs, Camera.pos.xyz, ray_dir.xyz);
		numberOfIterations = 0;
		float stepFactor = 1;
		float lastStep = 1;
		
		if (intTime >= 0.0 || startInside)
		{
			if(intersect && cy != 0 && cx != 0)
			{
				float k = dot(Camera.look_dir.xyz, tracePoint - Camera.pos.xyz)/dot(Camera.look_dir, Camera.ray_dir);				
				lastTracePoint = tracePoint = tracePoint + Camera.y_delta.xyz * k;
			}
			//initial phase, computation of start
			else if (!startInside) 
			{
				intTime = intTime < nearPlaneDist? nearPlaneDist: intTime;
				lastTracePoint = tracePoint = Camera.pos.xyz + ray_dir.xyz * intTime;
			}
			//start from near camera plane
			else 
			{
				lastTracePoint = tracePoint = Camera.pos.xyz + ray_dir.xyz * nearPlaneDist;
			}
			
			intersect = false;
			
			while (numberOfIterations < 350) {
	
				if(!SphereContains(testbs, tracePoint))
					break;
	
				numberOfIterations++;
	
				// traverse along the ray until intersection is found
	
				//at each successive step, step length is given by F(x)/lambda
				//morphFunction = DomainMorphFunction(tracePoint);
				//float step = SDBValue(morphFunction * tracePoint);
				//step /= length(dDomainMorphFunction(tracePoint) * Camera.ray_dir.xyz);
				float intTimeOffset = _LatticeValue(tracePoint) * 0.0194105321041013013001272034252352;
				float step = SDBValue(tracePoint) - abs(intTimeOffset);
	
				/////////////////////////////////////////////
				//backward movement strategy
				/*if(step < 0)
				{	
					stepFactor *= k2/10;
				}
				//revert back to normal
				else if(stepFactor < 1)
				{
					stepFactor = stepFactor * 1.2;
				}
				stepFactor = clamp(stepFactor, 0.1, 1);
				*/
				
				/////////////////////////////////////////////
				//binary search movement strategy
				if(step < 0)
				{
					//binary search, step halfway of what remains from the last positive step
					step = lastStep * 0.5;
					tracePoint = lastTracePoint;
				}	
				
				//move forward
				if (step < upperLimit && step > lowerLimit)
				{
					intersect = true;
					gradient = EstimateGradient(tracePoint, 0.01);
					break;
				} 
				else
				{
					lastTracePoint = tracePoint;
					lastStep = step;
					tracePoint += ray_dir.xyz * step * stepFactor;
				}
			}
		}
		else
		{
			intersect = false;
		}
		
		//kind of culling
		if(!intersect)
		{
			imageStore(u_NormalDepth, pixelID, vec4(0.0, 0.0, 0.0, 0));
		}
		else
		{	
			//from ray origin, direction and sphere center and radius compute intersection
			vec3 intersection = tracePoint;
			vec4 projected_i = modelviewprojection_transform * vec4(intersection, 1);
			projected_i /= projected_i.w;
		
			vec4 result;
			result.w = (projected_i.z + 1) * 0.5;
			//result.w = numberOfIterations/50.0;
			result.xyz = normalize(gradient) * 0.5f + 0.5f;
			
			//result = vec4(0.5,0.99,0.9,0);
			imageStore(u_NormalDepth, pixelID, result);
		}
		
		barrier();
	}
}
