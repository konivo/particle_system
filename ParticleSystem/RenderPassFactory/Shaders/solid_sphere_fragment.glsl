#version 410
////////////////////////////////////////////////////////////////////////////////
//types//

subroutine float SetFragmentDepth(vec3 pos, vec4 projected);
subroutine void SetOutputs(vec3 pos, vec4 projected);

////////////////////////////////////////////////////////////////////////////////
//uniforms//
uniform mat4 modelview_transform;
uniform mat4 projection_transform;
uniform mat4 modelviewprojection_transform;
subroutine uniform SetFragmentDepth u_SetFragmentDepth;
subroutine uniform SetOutputs u_SetOutputs;

////////////////////////////////////////////////////////////////////////////////
//common constants//
const float EXP_SCALE_FACTOR = 50;


////////////////////////////////////////////////////////////////////////////////
//inputs and outputs//

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


out vec4 uv_colorindex_none;
out vec4 normal_depth;

//returns value t, where given ray intersects sphere. Only positive return values are valid
//so it computes intersection only when the ray starts outside an sphere and aims toward  it
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

////////////////////////////////////////////////////////////////////////////////
//subroutines//


///////////////
subroutine(SetFragmentDepth)
float FragDepthDefault(vec3 intersection, vec4 projected)
{
	return (projected.z + 1) * 0.5;
}

///////////////
subroutine(SetFragmentDepth)
float FragDepthExponential(vec3 intersection, vec4 projected)
{
	return exp(((projected.z + 1) * 0.5) * EXP_SCALE_FACTOR - EXP_SCALE_FACTOR);
}

///////////////
subroutine(SetOutputs)
void SetOutputsDefault(vec3 intersection, vec4 projected)
{
	normal_depth.w = (projected.z + 1) * 0.5;
	normal_depth.xyz = normalize(intersection - Sprite.pos.xyz) * 0.5f + 0.5f;
	uv_colorindex_none = vec4((Sprite.color + 1) * 0.5, 0);
}

///////////////
subroutine(SetOutputs)
void SetOutputsNone(vec3 intersection, vec4 projected)
{ }

////////////////////////////////////////////////////////////////////////////////
//kernel//
void main ()
{
	float t = SphereRayIntersection(vec4(Sprite.pos, Sprite.radius), Camera.pos.xyz, Camera.ray_dir.xyz);

	//kind of culling
	if(t < 0)
		discard;

	//from ray origin, direction and sphere center and radius compute intersection
	vec3 intersection = t * Camera.ray_dir.xyz + Camera.pos.xyz;
	vec4 projected_i = modelviewprojection_transform * vec4(intersection, 1);
	projected_i /= projected_i.w;

	u_SetOutputs(intersection, projected_i);
	
	// Setup the outputs
	gl_FragDepth =  (projected_i.z + 1) * 0.5;//u_SetFragmentDepth(intersection, projected_i);
}
