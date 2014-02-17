#version 440
////////////////////////////////////////////////////////////////////////////////
//types//

subroutine void SetFragmentDepth();
subroutine void SetOutputs();

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

	flat vec3 normal;
} Sprite;

out vec4 uv_colorindex_none;
out vec4 normal_depth;

////////////////////////////////////////////////////////////////////////////////
//subroutines//

///////////////
subroutine(SetFragmentDepth)
void FragDepthDefault()
{
	gl_FragDepth = gl_FragCoord.z;
}

///////////////
subroutine(SetFragmentDepth)
void FragDepthExponential()
{
	gl_FragDepth =  exp(gl_FragCoord.z * EXP_SCALE_FACTOR - EXP_SCALE_FACTOR);
}

///////////////
subroutine(SetOutputs)
void SetOutputsDefault()
{
	normal_depth.w = gl_FragCoord.z;
	normal_depth.xyz = normalize(Sprite.normal) * 0.5f + 0.5f;
	uv_colorindex_none = vec4(Sprite.color, 0);
}

///////////////
subroutine(SetOutputs)
void SetOutputsNone()
{ }

////////////////////////////////////////////////////////////////////////////////
//kernel//
void main ()
{
	u_SetOutputs();
	
	// Setup the outputs
	u_SetFragmentDepth();
}
