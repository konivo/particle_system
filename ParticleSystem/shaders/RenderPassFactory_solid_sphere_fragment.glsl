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
uniform float pRayMarchStepFactor = 1;
uniform float k1, k2 = 0.8, k3, k4, time;
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

//
float torus_sdb(float r1, float r2, vec3 pos)
{
	float d1 = (length(pos.xy) - r1);
	d1 = sqrt(d1*d1 + pos.z*pos.z) - r2;

	return d1;
}


float SDBValue(vec3 pos)
{
	//vec3 mpos = DomainMorphFunction(pos);
	vec3 mpos = pos; 
	return torus_sdb(Sprite.radius * 3/4.0, Sprite.radius/4, mpos - Sprite.pos) * pRayMarchStepFactor;
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
  testbs.w += 1;

	float intTime = SphereRayIntersection(bs, Camera.pos.xyz, Camera.ray_dir.xyz);
	float upperLimit = 1*epsilon;
	float lowerLimit = -1*epsilon;

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
