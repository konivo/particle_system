#version 410
/*
view constants
*/
uniform mat4 projection_transform;
uniform mat4 modelviewprojection_transform;
uniform mat4 modelviewprojection_inv_transform;
uniform mat4 modelview_inv_transform;
uniform mat4 modelview_transform;
uniform vec2 viewport_size;

/*
rendering constants
*/
uniform float pRayMarchStepFactor = 0.4194;
uniform float k1, k2 = 0.9, k3, k4 = -1.412, time;
const float epsilon = 0.01;
const float nearPlaneZ = 1;

/*
0 - normal
1 - shadow
2 - shadow, exponential map
*/
uniform int mode;

/*
output constants
*/
const float EXP_SCALE_FACTOR = 50;

in SpriteData
{
//particle position in space
  vec3 pos;

//input particle dimensions
	float radius;

//input particle color
	vec3 color;
} Sprite;
 
//  
in CameraData
{
	vec4 ray_dir; 
	vec4 pos;
	//-1, 1 parameterization
	//view dependent parametrization
	vec2 param;
} Camera;


out Fragdata
{
	vec4 uv_colorindex_none;
	vec4 normal_depth;
};

/*====================================================================================
utility functions
*/
vec4 get_clip_coordinates (vec2 param, float imagedepth)
{
	return vec4((param * 2) - 1, imagedepth, 1);
}

ivec2 get_pixel_pos(vec2 uv, vec2 viewport)
{
	return ivec2((uv * 2 - 1) * viewport);
}

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

	float kx = dot(rayDirection,rayDirection),
	ky = 2.0*dot(rayDirection, k),
	kz1 = dot(k,k),
	kz2 = sphere.w*sphere.w;


	float discr = (ky*ky - 4.0*kx*kz1) + 4.0*kx*kz2;

	if(discr < 0.0)
		return -1.0;
	else
		return (-ky - sqrt(discr))/(2.0*kx);
}

//utilities and library function
//returns true when sphere contains given point
bool SphereContains(in vec4 s, in vec3 point)
{
	return length(s.xyz - point) < s.w;
}

//====================================================================================

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

int[] PERMUTATION_TABLE = int[](151,160,137,91,90,15,131,13);

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

/*
float SmoothVoronoi( in vec3 x )
{
    ivec3 p = floor( x );
    vec3  f = fract( x );

    float res = 0.0;
		for( int k=-1; k<=1; k++ )
    for( int j=-1; j<=1; j++ )
    for( int i=-1; i<=1; i++ )
    {
        ivec3 b = ivec3( i, j, k );
        vec3  r = vec3( b ) - f + random( p + b );
        float d = length( r );

        res += exp( -32.0*d );
    }
    return -(1.0/32.0)*log( res );
}*/

//====================================================================================

//subroutine(SetOutputFragmentDataRoutine)
void SetDefaultFragmentData(vec3 intersection, vec3 gradient)
{
	vec4 projected_i = modelviewprojection_transform * vec4(intersection, 1);
	projected_i /= projected_i.w;

	gl_FragDepth = normal_depth.w = (projected_i.z + 1) * 0.5;
	normal_depth.xyz = normalize(gradient) * 0.5f + 0.5f;

	uv_colorindex_none = vec4((Sprite.color + 1) * 0.5, 0);
}

//subroutine(SetOutputFragmentDataRoutine)
void SetShadowFragmentData(vec3 intersection)
{
	vec4 projected_i = modelviewprojection_transform * vec4(intersection, 1);
	projected_i /= projected_i.w;
	gl_FragDepth = (projected_i.z + 1) * 0.5;
}

//
void SetExpShadowFragmentData(vec3 intersection)
{
	vec4 projected_i = modelviewprojection_transform * vec4(intersection, 1);
	gl_FragDepth = exp(((projected_i.z/projected_i.w + 1) * 0.5) * EXP_SCALE_FACTOR - EXP_SCALE_FACTOR);
}

//
void main_NOTUSED ()
{
	float t = SphereRayIntersection(vec4(Sprite.pos, Sprite.radius), Camera.pos.xyz, Camera.ray_dir.xyz);

	//kind of culling
	if(t < 0)
		discard;

	//from ray origin, direction and sphere center and radius compute intersection
	vec3 intersection = t * Camera.ray_dir.xyz + Camera.pos.xyz;

	//
	switch(mode)
	{
		case 0:
			SetDefaultFragmentData(intersection, vec3(0));
			break;
		case 1:
			SetShadowFragmentData(intersection);
			break;
		case 2:
			SetExpShadowFragmentData(intersection);
			break;
		default:
			break;
	}
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
vec4( 0.04, -0.08, 0, 0.3366834171),
vec4( -0.8, -0.88, -0.76, 0.469273743),
vec4( 0.42, -0.36, -0.08, 0.2582417582),
vec4( -0.34, -0.44, -0.4, 0.4072164948),
vec4( 0.42, -0.82, 0.18, 0.3372093023),
vec4( 0.54, -0.56, -0.26, 0.3782051282),
vec4( 0.54, 0.9, -0.06, 0.2487046632),
vec4( -0.4, 0.7, -0.92, 0.1923076923),
vec4( -0.82, -0.14, -0.18, 0.3086419753),
vec4( -0.16, 0.1, -0.38, 0.4370860927),
vec4( 0.02, 0.7, 0.98, 0.496350365),
vec4( 0.86, 0.92, -0.94, 0.3063583815),
vec4( -0.9, 0.84, 0.1, 0.2402597403),
vec4( -0.2, 0.7, 0.96, 0.2363636364),
vec4( 0.72, 0.12, 0.92, 0.3846153846),
vec4( -0.1, -0.72, 0.92, 0.2756756757),
vec4( 0.26, 0.98, -0.78, 0.2959183673),
vec4( -0.7, 0.62, -0.56, 0.2576419214),
vec4( 0.14, -0.94, 0.12, 0.4470046083),
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
vec4( -0.96, 0.24, -0.5, 0.2 5)
	);
	
float setofspheres_sdb(float radius, vec3 mpos)
{
	float d = 100000;
	for(int i = 0; i < sph.length()/6;i++)
	{
		float di = sphere_sdb(sph[i]*vec4(radius/2), mpos);
		d = min(d, di);
	}
	return d;
}

float spherecarvedbyspheres_sdb(float radius, vec3 mpos)
{
	float factor = 15;//abs(sin(mpos.x*2)+ 1) * 10 + 5;
  float res1 = 0.0;
  float res2 = 0.0;
	float cellSize1 = radius/13;
	float cellSize2 = radius/64;

  ivec3 p = ivec3(floor( mpos / cellSize1 ));
  vec3  f = fract( mpos / cellSize1);
	float d0 = sphere_sdb(vec4(0,0,0,radius * 0.85), mpos);
	float d1 = sphere_sdb(vec4(0,0,0,radius* 0.99), mpos);

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
	return max(d0 - 2*pow(res1, -1/factor) - k4, d1 );
}


float SDBValue(vec3 pos)
{
	//vec3 mpos = DomainMorphFunction(pos);
	vec3 mpos = pos; 
	return spherecarvedbyspheres_sdb (Sprite.radius, mpos - Sprite.pos) * pRayMarchStepFactor;
	//return setofspheres_sdb(Sprite.radius, mpos - Sprite.pos) * pRayMarchStepFactor;
	//return torus_sdb(Sprite.radius * 3/4.0, Sprite.radius/4, mpos - Sprite.pos) * pRayMarchStepFactor;
	//return sphere_sdb(vec4(0, 0, 0, Sprite.radius), mpos - Sprite.pos) * pRayMarchStepFactor;
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
	//Camera.pos = modelview_inv_transform * vec4(0, 0, 0, 1);
	//Camera.ray_dir = normalize( reproject(modelviewprojection_inv_transform, get_clip_coordinates(Camera.param, -1)) - Camera.pos );
	vec4 Camera_look_dir = modelview_inv_transform * vec4(0, 0, -1, 0);

	//here is expected ray with unit direction, and starting from camera origin
	int numberOfIterations = 0;

	//starting point
	vec3 tracePoint, gradient;

	//test for intersection with bounds of function
	vec4 bs = vec4(Sprite.pos, Sprite.radius);
  vec4 testbs = bs;
  testbs.w += Sprite.radius * 0.1;

	float intTime = SphereRayIntersection(bs, Camera.pos.xyz, Camera.ray_dir.xyz);
	float upperLimit = epsilon;
	float lowerLimit = -epsilon;

	bool intersect = false;
	bool startInside = SphereContains(bs, Camera.pos.xyz);

	//transformation into sdb function domain
	mat3 morphFunction;

	if (intTime >= 0.0 || startInside)
	{
		//initial phase, computation of start
		float nearPlaneDist = nearPlaneZ/dot(Camera_look_dir, Camera.ray_dir);

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
		while (numberOfIterations < 60) {

			if(!SphereContains(testbs, tracePoint))
				break;

			numberOfIterations++;

			// traverse along the ray until intersection is found

			//at each successive step, step length is given by F(x)/lambda
			//morphFunction = DomainMorphFunction(tracePoint);
			//float step = SDBValue(morphFunction * tracePoint);
			//step /= length(dDomainMorphFunction(tracePoint) * Camera.ray_dir.xyz);
			float intTimeOffset = _LatticeValue(tracePoint) * 0.0000094105321041013013001272034252352;
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
	//vec4 projected_i = modelviewprojection_transform * vec4(intersection, 1);
	//projected_i /= projected_i.w;
	
	//gl_FragDepth = normal_depth.w = (projected_i.z + 1) * 0.5;
	//normal_depth.xyz = normalize(gradient) * 0.5f + 0.5f;

	//
	switch(mode)
	{
		case 0:
			SetDefaultFragmentData(intersection, normalize(gradient));
			break;
		case 1:
			SetShadowFragmentData(intersection);
			break;
		case 2:
			SetExpShadowFragmentData(intersection);
			break;
		default:
			break;
	}
}
