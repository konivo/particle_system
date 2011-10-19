#version 410
uniform mat4 modelview_transform;
uniform mat4 projection_transform;
uniform mat4 modelviewprojection_transform;
/*
0 - normal
1 - shadow
2 - shadow, exponential map
*/
uniform int mode;

//subroutine void SetOutputFragmentDataRoutine(vec3 ray_sphere_intersection);
//subroutine uniform SetOutputFragmentDataRoutine SetOutputFragmentData;

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


//subroutine(SetOutputFragmentDataRoutine)
void SetDefaultFragmentData(vec3 intersection)
{
	vec4 projected_i = modelviewprojection_transform * vec4(intersection, 1);
	projected_i /= projected_i.w;

	gl_FragDepth = normal_depth.w = (projected_i.z + 1) * 0.5;
	normal_depth.xyz = normalize(intersection - Sprite.pos.xyz) * 0.5f + 0.5f;

	uv_colorindex_none = vec4(Camera.param, 0.5f, 0);
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
	gl_FragDepth = exp(((projected_i.z/projected_i.w + 1) * 0.5) * 200 - 200);
}

//
void main ()
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
			SetDefaultFragmentData(intersection);
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