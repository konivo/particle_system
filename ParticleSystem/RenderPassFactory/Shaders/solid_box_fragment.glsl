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

const float EXP_SCALE_FACTOR = 50;

in SpriteData
{
//particle position in space
  vec3 pos;

//input particle dimensions
	float radius;

//input particle color
	vec3 color;

	vec3 normal;
} Sprite;

out Fragdata
{
	vec4 uv_colorindex_none;
	vec4 normal_depth;
};

//
void main ()
{
	//
	switch(mode)
	{
		case 0:
		case 1:
			gl_FragDepth = normal_depth.w = gl_FragCoord.z;
			normal_depth.xyz = normalize(Sprite.normal) * 0.5f + 0.5f;
			break;
		case 2:
			gl_FragDepth = exp(gl_FragCoord.z * EXP_SCALE_FACTOR - EXP_SCALE_FACTOR);
			break;
		default:
			break;
	}
}