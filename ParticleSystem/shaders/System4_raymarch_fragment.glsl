#version 330
uniform mat4 projection_transform;
uniform mat4 modelviewprojection_transform;
uniform mat4 modelviewprojection_inv_transform;
uniform mat4 modelview_inv_transform;
uniform vec2 viewport_size;
uniform float pRayMarchStepFactor;

uniform float k1, k2, k3, k4, time;

const float epsilon = 0.001;
const float nearPlaneZ = 1;

in VertexData
{
	vec2 param;
};

out Fragdata
{
	vec4 normal_depth;
};
 
//
struct
{
	vec4 ray_dir;
	vec4 pos;
	vec4 look_dir;
	//-1, 1 parameterization
	//view dependent parametrization
	vec2 param;
} Camera;

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

//utilities and library function
//returns true when sphere contains given point
bool SphereContains(in vec4 s, in vec3 point)
{
	return length(s.xyz - point) < s.w;
}

const int[] PERMUTATION_TABLE = int[](151,160,137,91,90,15,131,13);

#define PERM(i) PERMUTATION_TABLE[(i)&0x7]

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

//====================================================================================


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

vec4[] sph = vec4[](
		vec4(45, 18, 31, 9),
		vec4(15, 43, 40, 8),
		vec4( 6, 37, 33, 6),
		vec4(24, 25,  7, 4),
		vec4(29, 32, 11, 3),
		vec4(19, 14, 17, 5),
		vec4(38, 27, 12, 3),
		vec4(28,  8,  5, 3),
		vec4(10, 26, 50, 3),
		vec4(36, 46, 22, 9),
		vec4(4, 4, 20,  9),
		vec4(-10, 40, 10, 8),
		vec4( 6, 18, -27, 1),
		vec4(-42, 29, 31, 4),
		vec4(27, 14, 33, 8),
		vec4(-14, -33, 44, 8),
		vec4(24, -1, -28, 10),
		vec4(-46, -38, 13, 5),
		vec4(-23, 43,  1, 8),
		vec4(-26, 20, -43,  9),
		vec4( 7, -22, -21, 3),
		vec4(16, -49, 39, 9),
		vec4(22, 25, -10, 4),
		vec4(-50, -20, 38, 9),
		vec4(-35,  5, 30, 4),
		vec4(-11, -39, -17, 12),
		vec4(-7, 21, 47, 6),
		vec4(-13, 36,  2, 5),
		vec4(-5, 40, -12, 6),
		vec4(37, 11,  0, 6),
		vec4(-2, -8, 10, 7),
		vec4(-24,  4, -29, 9),
		vec4(48,  8, 42, 9),
		vec4(-47, 45, 50, 11),
		vec4(15, -25, 46, 5),
		vec4(32, -34, -36, 7),
		vec4(-48, 34, 17,  3)
	);


float setofspheres_sdb(vec3 mpos)
{
	float d = 100000;
	for(int i = 0; i < sph.length;i++)
	{
		float di = sphere_sdb(sph[i]*vec4(1,1,1,1), mpos);
		d = min(d, di);
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
	for(int i = 0; i < sph.length;i++)
	{
		float di = sphere_sdb(normalize(sph[i])*vec4(70.5,50.5,70.5,4.2), mpos);
		d = min(d, di);
		//dN = dN - K/(di + t/2);
		dN = min(dN, di);
//		dN = dN - K/max(di, t/2);
	}
	return min(d, dN);
}

float setofmeltedspheres1_sdb(vec3 mpos)
{
	float period = 6;
	float t = 4*abs(sin(fract(time/period)* 2 * 3.14)) + 6.2;
	float d = 100000;
	float K = 109;
	float dN = 20;//t*10;//needs to be customized with respeck to K
	for(int i = 0; i < sph.length;i++)
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
	for(int i = 0; i < sph.length;i++)
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

//====================================================================================


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

vec3 morph_rotate(vec3 pos, vec4 axisangle)
{
	mat3 i = mat3(1);
	vec3 e = axisangle.xyz;
	float theta = axisangle.w;
	mat3 eet = mat3(
		e.x * e,
		e.y * e,
		e.z * e);
	mat3 ex = mat3(
		0, e.z, -e.y,
		-e.z, 0, e.x,
		e.y, -e.x, 0); 

//matrices are specified in column-major order

	mat3 rotmatrix = i * cos(theta) + (1 - cos(theta))* eet + ex * sin(theta);
	return rotmatrix * pos;
}

vec3 morph_mod(vec3 pos, vec3 grid)
{
	return pos - (floor(pos  / grid + 0.5))* grid;
}

/*
vec3 DomainMorphFunction(vec3 pos)
{
	return vec3(
		sphere_sdb(vec4(-60, 0, 5, 30), pos),
		sphere_sdb(vec4(3, 50, 0, 73), pos),
		sphere_sdb(vec4(38, 0, -40, 80), pos));
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
}*/
/*

vec3 DomainMorphFunction(vec3 pos)
{
	vec3 v1 = vec3(

				torus_sdb(40, 40, pos.yxz + vec3(0, 3, 0)),
				torus_sdb(40,  30, pos - vec3(-5, 0, 11)),
				torus_sdb(50, 50, pos.zyx - vec3(0, 0, 11)));

	vec3 v2 = vec3(
		torus_sdb(60, 60, v1.yxz - vec3(15, 10, 0)),
		torus_sdb(100,  30, v1 - vec3(-5, 0, 11)),
		torus_sdb(60, 40, v1.zyx - vec3(12, 2, -1)));

	vec3 v3 = vec3(
		torus_sdb(30, 2, v2.yxz - vec3(-5, 10, 0)),
		torus_sdb(30,  20, v2 - vec3(5, 10, 11)),
		torus_sdb(30, 30, v2.zyx - vec3(5, 0, -11)));

	return v3;
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
}*/


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

vec3 SinTrans(float amp, float octave, vec3 v1, vec3 center, vec3 plane, vec3 quant)
{
	vec3 k = amp * sin(dot(v1 - center, plane) * octave)/octave * quant;
	return v1 + k;
}

vec3 _noiseSinOmega1a = vec3(15.25263, 31.356474, 6.45575);
float _LatticeValue(vec3 pos)
{
	return fract(sin(dot(pos * 128, _noiseSinOmega1a))* 45678.36364)*2.0 - 1.0 ;
}

float SimplePRSF(vec3 pos)
{
	vec3 ipos = floor(pos);
	vec3 ipos1 = ceil(pos);

	vec3 tB = smoothstep(0.0, 1.0, pos - ipos);
	vec3 tA = 1 - tB;

	vec4 val_x = mix(
		vec4(_LatticeValue(ipos),
			_LatticeValue(ivec3(ipos.x, ipos1.y, ipos.z)),
			_LatticeValue(ivec3(ipos.x, ipos.y, ipos1.z)),
			_LatticeValue(ivec3(ipos.x, ipos1.y, ipos1.z))),
		vec4(_LatticeValue(ivec3(ipos1.x, ipos.yz)),
			_LatticeValue(ivec3(ipos1.x, ipos1.y, ipos.z)),
			_LatticeValue(ivec3(ipos1.x, ipos.y, ipos1.z)),
			_LatticeValue(ipos1)), 
			tB.x);

	vec2 val_y = mix(val_x.xz, val_x.yw,	tB.y);

	return tA.z*val_y.x + tB.z*val_y.y;
}

vec3 DomainMorphFunction(vec3 pos)
{
/*
//..?
	vec3 v1 = pos;

	v1 = vec3(pos.x, sin(pos.y / 30) * 30, cos(pos.z/ 30) * 30);
	v1 = morph_rotate(v1);
	v1 = morph_rotate(v1.yzx);
	v1 = morph_rotate(v1);
	v1 = morph_rotate(v1.zxy);
	v1 = morph_rotate(v1.yzx);
	v1 = vec3(v1.x, sin(v1.y / 30) * 30, cos(v1.z/ 30) * 30);
*/

/*
//zarotovana ve 3 osach 
	vec3 v1 = pos;

	v1 = SinTrans(0, 1, pos);
	v1 = morph_rotate(v1);
	v1 = SinTrans(0, 1, v1.zyx);
	v1 = morph_rotate(v1);
	v1 = SinTrans(0, 1, v1.zyx);
	v1 = morph_rotate(v1);
*/
/*
//spirala 1
//vypada jinak pokud se prohodi ten zakomentovany morph_rotate s tim druhym
	vec3 center = vec3(0, 0, 0);
	vec3 v = pos;//.zxy;
	
	for(int i = 0; i < 3; i++)
	{
		vec3 temp = v;
		v = morph_rotate(v);
		v = SinTrans(0, 1, v, center, normalize(v));
		//v = morph_rotate(v);
		center = temp;
	}
*/

//rovinna discretizace objektu
/*	vec3 center = vec3(1, 0, 0);
	vec3 v = pos;
	
	for(int i = 0; i < 3; i++)
	{
		v = SinTrans(
		1, 
		1, 								//velikost kosticek
		v, 								//
		vec3(0), 					//offset pocatku
		vec3(1, 0, 0));		//smer vektoru diskretizace
	}*/

//perute z kartonu
/*	vec3 vA =  morph_rotate(v, vec4(1, 0, 1, length(v)));
	vA =  morph_rotate(v, vec4(normalize(sin(vA/2) + 2), sin(length(v/2))));
	v =  v + vA;*/

//lampion
/*	vec3 vA =  morph_rotate(v-10, vec4(1, 1, 0, length(v)));
	vA =  morph_rotate(v - 10, vec4(normalize((vA/2) - 1), (length(v/1))));
	v =  v + vA;*/

//spiralovy posun sintrans 
//	vec3 center = vec3(-pos.y, pos.xz);//vec3(1, 0, 0);
	vec3 center = vec3(1, 0, 0);
	vec3 v = pos;
	vec3 gridDim = vec3(15);
	vec3 v1 = pos + 5.5;
	vec3 v2 = floor(v1/ gridDim);
	vec3 v3 = ceil(v1/ gridDim);
	vec3 cellCenter = (v2 + v3)/2;
	vec3 v_t = v1/ gridDim - v2 ;
	float period = 40;
	float t = fract(time/period)* period;
	float smoothiness = 0.95;//t/period;
	float boxSize = 1;//t/5;
	
	for(float i = 1; i <= 1; i++)
	{
		vec3 temp = v;
		//v = SinTrans(1, 1, v, center, normalize(morph_rotate(v)));
		//v = SinTrans(1, 1, v, morph_rotate(center), normalize(v));
		//v = SinTrans(1, 1, v, morph_rotate(center, vec4(0, 0, 1, 1)), normalize(morph_rotate(center)));
		
		//center = temp;

		//v = SinTrans(1.2, 3, v, center, normalize(v));
		//v = SinTrans(2.1, 1.3, v, center, morph_rotate(normalize(v), vec4(normalize(center * vec3(3, 0.3f, 0.3f)), 0.953f)));
		//v = SinTrans(5  , 1, v, center, normalize(v)) -	morph_rotate(v, vec4(1, 0, 0, 4.3f)) ;
		//center = temp;
		
		//v = SinTrans(0, 1, v, center, normalize(v));
		//center = morph_rotate(temp);
		
		vec3 k = 0.3512 * (
			(v_t * abs(sin(4231.4213412*v3)) + (1 - v_t) * abs(sin(4231.4213412*v2))) + 
			(v_t * abs(sin(9892.4213412*v3)) + (1 - v_t) * abs(sin(9892.4213412*v2))) + 
			(v_t * abs(sin(214532.4213412*v3)) + (1 - v_t) * abs(sin(214532.4213412*v2)))) ;
		//vec3 k = vec3(SimplePRSF(v/5))*10;
/*
		for(int i = 0; i < 3; i++)
		{
			v = SinTrans(smoothiness, boxSize, v, vec3(t), normalize(vec3(1, 0, 0)));//normalize(morph_rotate(center)));
		}
		for(int i = 0; i < 3; i++)
		{
			v = SinTrans(smoothiness, boxSize, v, vec3(0), normalize(vec3(0, 1, 0)));//normalize(morph_rotate(center)));
		}
		for(int i = 0; i < 3; i++)
		{
			v = SinTrans(smoothiness, boxSize, v, vec3(0), normalize(vec3(0, 0, 1)));//normalize(morph_rotate(center)));
		}
*/
		//vec3 vA =  morph_rotate(v, vec4(1, 0, 1, 20/length(v)));
		//vA =  morph_rotate(v, vec4(normalize(sin(vA/2) + 2), sin(length(v/2))));
		//v =  vA;
		//v = morph_rotate(v, vec4(normalize(vec3(SimplePRSF(v/25), SimplePRSF(v/25), SimplePRSF(v/25))), k));
		//v = morph_rotate(v.xyz, vec4(0, 1, 0, k*10));//SimplePRSF(v/15)*3));
		//v = morph_rotate(v.xyz, vec4(0, 0, 1, (1 - k)*10));
		//vec3 vA =  morph_rotate(v, vec4(normalize((v + k4)), length(v)*k2));
		//vA =  morph_rotate(v, vec4(normalize(vA - 20), length(v)));
		//vA =  morph_rotate(vA, vec4(normalize(vA - 20), 50/length(v)));
		//v =  morph_rotate(vA , vec4(normalize((v + t)), (length(v)+t)));
		//vA =  morph_rotate(v, vec4(normalize(vA), sin(length(v/2))));
		//vA =  morph_rotate(v, vec4(normalize(vA), sin(length(v/3))));
		//vA =  morph_rotate(v, vec4(normalize(vA + 20), 50/length(v)));
		//vA =  morph_rotate(v, vec4(normalize(vA - 30), 30/length(v)));
		
		//vA =  morph_rotate(v, vec4(normalize(vA), length(v)/5));
		//vA =  morph_rotate(v, vec4(normalize(vA), length(v)/5));
		//v =  v + vA/1;
		//v =  v + 35*normalize(morph_rotate(v, vec4(normalize(vA), length(v)/5)));
		//v =  v3 + morph_rotate(v - v3, vec4(vec3(1, 0, 0) + v, -length(v3)/1));
		//v = morph_rotate(v, vec4(0, 1, 0, 10*k));
		//v = v + k;
		//v = v + cos(sin(k * v+ sin(k * v.xzy + k) + 10*k) + cos(2 + 10*k + k* v.zyx + sin(k * v.yxz + k)) + sin(k *v.yzx + cos(5*k + k * v.zxy))) ;
		//v += k;//SimplePRSF(v/15)*5;
		//

//v = SinTrans(2.0f, 1, v, center, normalize(vec3(1,0, 0)), normalize(vec3(v.xy, 1)));
		//v = SinTrans(2.0f, 1, v, center, normalize(vec3(1,0, 0)), normalize(vec3(v.xy, 1)));

	//	v = morph_rotate(v, vec4(normalize(center), 0.91));
				
		//center = vec3(-v.y, v.xz);
		//v = SinTrans(2.0f, i, v, center, normalize(vec3(v.xy, 0)), normalize(vec3(v.xy, 0)));
		//center = temp;
		//v2 = morph_mod(v, vec3(15, 15, 15));
	}

	//v = morph_mod(v, vec3(50, 50, 50));

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
	return setofmeltedspheres_sdb(mpos) * pRayMarchStepFactor;
	//return setofspheres_sdb(mpos) * pRayMarchStepFactor;
	//return torus_sdb(65, 8, mpos.xyz) * pRayMarchStepFactor;
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
	//camera initialization
	Camera.pos = modelview_inv_transform * vec4(0, 0, 0, 1);
	Camera.ray_dir = normalize( reproject(modelviewprojection_inv_transform, get_clip_coordinates(param, -1)) - Camera.pos );
	Camera.look_dir = modelview_inv_transform * vec4(0, 0, -1, 0);

	//here is expected ray with unit direction, and starting from camera origin
	int numberOfIterations = 0;

	//starting point
	vec3 tracePoint, gradient;

	//test for intersection with bounds of function
	vec4 bs = vec4(0, 0, 0, 2000);
  vec4 testbs = bs;
  testbs.w += 1;

	float intTime = SphereRayIntersection(bs, Camera.pos.xyz, Camera.ray_dir.xyz);
	float upperLimit = epsilon;
	float lowerLimit = -100*epsilon;

	bool intersect = false;
	bool startInside = SphereContains(bs, Camera.pos.xyz);

	//transformation into sdb function domain
	mat3 morphFunction;

	if (intTime >= 0.0 || startInside)
	{
		//initial phase, computation of start
		float nearPlaneDist = nearPlaneZ/dot(Camera.look_dir, Camera.ray_dir);

		if (!startInside) {
			intTime = intTime < nearPlaneDist? nearPlaneDist: intTime;
			tracePoint = Camera.pos.xyz + Camera.ray_dir.xyz * intTime;
		}
		//start from near camera plane
		else {
			tracePoint = Camera.pos.xyz + Camera.ray_dir.xyz * (nearPlaneDist);
		}
		float stepFactor = 1;
		float lastStep = 1;
		while (numberOfIterations < 500) {

			if(!SphereContains(testbs, tracePoint))
				break;

			numberOfIterations++;

			// traverse along the ray until intersection is found

			//at each successive step, step length is given by F(x)/lambda
			//morphFunction = DomainMorphFunction(tracePoint);
			//float step = SDBValue(morphFunction * tracePoint);
			//step /= length(dDomainMorphFunction(tracePoint) * Camera.ray_dir.xyz);
			float intTimeOffset = _LatticeValue(tracePoint) * 0.0094105321041013013001272034252352;
			float step = SDBValue(tracePoint) - abs(intTimeOffset);

			//customize backward movement
			if(step < 0)
			{
				/*
				//kind of binary search, but with changeable koeficient
				//step = stepFactor * lastStep * step/(lastStep - step);
				*/

				
				//binary search, step halfway of what remains from the last positive step
//				step = -lastStep * 0.5;
				

				stepFactor *= k2/10;
			}

			//if the lastStep is below zero, then we are again in the positive space
			//and we will shorten the step by a factor
			/*if(step < 0 && lastStep > 0)
			{
				//stepFactor *= step/(step * stepFactor - lastStep * stepFactor);
				//stepFactor *= k2;
			}*/

			//if the lastStep is below zero, then we are again in the positive space
			//and we will shorten the step by a factor
			/*if(step > 0 && lastStep < 0)
			{
				stepFactor *= k3;
			}*/

			//if(numberOfIterations % 30 == 9)
				//stepFactor *= k2/10;

			lastStep = step;
			stepFactor = max(stepFactor, 0.01);
			//move forward
			if (step < upperLimit && step > lowerLimit) {
				intersect = true;
				gradient = EstimateGradient(tracePoint, 0.01);
				break;
			} else {
				tracePoint += Camera.ray_dir.xyz * step * stepFactor;
			}
		}
	}

	//kind of culling
	if(!intersect)
		discard;

	//from ray origin, direction and sphere center and radius compute intersection
	vec3 intersection = tracePoint;
	vec4 projected_i = modelviewprojection_transform * vec4(intersection, 1);
	projected_i /= projected_i.w;

	gl_FragDepth = normal_depth.w = (projected_i.z + 1) * 0.5;
	normal_depth.xyz = normalize(gradient) * 0.5f + 0.5f;
}
