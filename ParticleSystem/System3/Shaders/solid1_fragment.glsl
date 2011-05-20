#version 330
uniform mat4 projection_transform;
uniform mat4 modelviewprojection_transform;

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

//
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

	gl_FragDepth = normal_depth.w = (projected_i.z + 1) * 0.5;
	normal_depth.xyz = normalize(intersection - Sprite.pos.xyz) * 0.5f + 0.5f;

	uv_colorindex_none = vec4(Camera.param, 0.5f, 0);
}