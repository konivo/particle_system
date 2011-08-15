#version 330
uniform mat4 projection_transform;
uniform mat4 modelviewprojection_transform;
uniform mat4 modelviewprojection_inv_transform;
uniform mat4 modelview_inv_transform;
uniform vec2 viewport_size;

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
}
*/

vec3 SinTrans(float amp, float octave, vec3 v1)
{
	return vec3(v1.y + cos((v1.y - v1.z) * octave)/octave, v1.z + sin((v1.z - v1.y) * octave)/octave, v1.x - cos((v1.x - v1.z) * octave)/octave);
}

vec3 DomainMorphFunction(vec3 pos)
{
	vec3 v1 = pos;

	//v1 = vec3(pos.x, sin(pos.y / 30) * 30, cos(pos.z/ 30) * 30);
	//v1 = morph_rotate(v1);
	//v1 = morph_rotate(v1.yzx);
	//v1 = morph_rotate(v1);
	//v1 = morph_rotate(v1.yzx);
	//v1 = morph_rotate(v1.yzx);
	//v1 = vec3(v1.x, sin(v1.y / 30) * 30, cos(v1.z/ 30) * 30);

	v1 = SinTrans(0, 1, pos);
	v1 = SinTrans(0, 1, v1);
	v1 = SinTrans(0, 1, v1);
	return v1;
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
	return torus_sdb(50, 10, mpos) * 0.4;
	//return sphere_sdb(vec4(0, 0, 0, 28), mpos) * 0.1;
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

		while (numberOfIterations < 300) {

			if(!SphereContains(testbs, tracePoint))
				break;

			numberOfIterations++;

			// traverse along the ray until intersection is found

			//at each successive step, step length is given by F(x)/lambda
			//morphFunction = DomainMorphFunction(tracePoint);
			//float step = SDBValue(morphFunction * tracePoint);
			//step /= length(dDomainMorphFunction(tracePoint) * Camera.ray_dir.xyz);
			float intTimeOffset = random(get_pixel_pos(param, viewport_size), 0.4252352);
			float step = SDBValue(tracePoint) - intTimeOffset;

			//move forward
			if (step < epsilon) {
				intersect = true;
				gradient = EstimateGradient(tracePoint, 0.01);
				break;
			} else {
				tracePoint += Camera.ray_dir.xyz * step;
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
