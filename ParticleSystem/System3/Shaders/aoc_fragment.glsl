#version 330

uniform mat4 modelview_transform;
uniform mat4 modelviewprojection_transform;
uniform mat4 projection_transform;
uniform vec2[100] sampling_pattern;

uniform sampler2D normaldepth_texture;

//maximum distance of an occluder in the world space
const float occluder_max_distance = 5;
const float PI = 3.141592654f;

in VertexData
{
	vec2 param;
};

out Fragdata
{
	float aoc;
};

void main ()
{
	aoc = 0.0f;

//inverse transform from image to world space
	mat4 mvp_inv = inverse(modelviewprojection_transform);
	mat4 proj_inv = inverse(projection_transform);

//todo: p_nd.w is in screen space, thus in range 0, 1. We need it to be in range -1, 1
//p is the sample, for which aoc is computed
	vec4 p_nd = texture(normaldepth_texture, param);
	vec4 p_pos = mvp_inv * vec4((param * 2) - 1, p_nd.w, 1);
	vec4 p_campos = proj_inv * vec4((param * 2) - 1, p_nd.w, 1);
	p_pos /= p_pos.w;
	p_campos /= p_campos.w;

//screen space radius of sphere of influence
  vec4 _rf =  projection_transform * vec4(occluder_max_distance, 0, p_campos.z, 1);
  _rf /= _rf.w;

  float rf = _rf.x / 2;

//for each sample compute occlussion estimation and add it to result
	for(int i = 0; i < sampling_pattern.length; i++)
	{
		vec2 oc_param = param + sampling_pattern[i] * rf;

		vec4 o_nd = texture(normaldepth_texture, oc_param);
		vec4 o_pos = mvp_inv * vec4((oc_param * 2) - 1, o_nd.w, 1);
		o_pos /= o_pos.w;

		vec4 _o_r =  proj_inv * vec4(1/300.0f, 0, o_nd.w, 1);
  	_o_r /= _o_r.w;
  	float o_r = _o_r.x;

//
		float op_distance = length(o_pos - p_pos);
		float s_omega = 2 * PI * (1 - cos( asin( o_r/ op_distance)));

		aoc += s_omega * max(dot( o_pos.xyz - p_pos.xyz, p_nd.xyz), 0);
	}

	aoc /= 2 * PI;
}