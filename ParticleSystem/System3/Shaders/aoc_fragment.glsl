#version 330
uniform mat4 modelviewprojection_transform;
uniform mat4 modelviewprojection_inv_transform;
uniform mat4 projection_transform;
uniform mat4 projection_inv_transform;
uniform vec2 viewport_size;

//todo: shall be uniformly distributed. Look for some advice, how to make it properly
uniform vec2[100] sampling_pattern;

//
uniform sampler2D normaldepth_texture;

//maximum distance of an occluder in the world space
const float OCCLUDER_MAX_DISTANCE = 5;

//these two constants will limit how big area in image space will be sampled.
//farther areas will be smaller in size and thus will contain less samples,
//less far areas will be bigger in screen size and will be covered by more samples.
//Samples count should change with square of projected screen size?
const float OCCLUDER_MIN_PROJECTED_SIZE = 2;
const float OCCLUDER_MAX_PROJECTED_SIZE = 200;
//
const float PI = 3.141592654f;

//param in range (0, 0) to (1, 1)
in VertexData
{
	vec2 param;
};

//computed ambient occlusion estimate
out Fragdata
{
	float aoc;
};

//
vec4 get_normal_depth (vec2 param)
{
//todo: p_nd.w is in screen space, thus in range 0, 1. We need it to be in range -1, 1, also account with offsets and so on
	vec4 result = texture(normaldepth_texture, param);
	result = result * 2 - 1;

	return result;
}

//
vec4 get_clip_coordinates (vec2 param, float imagedepth)
{
	return vec4((param * 2) - 1, imagedepth, 1);
}

//
vec4 reproject (mat4 transform, vec4 vector)
{
	vec4 result = transform * vector;
	result /= result.w;
	return result;
}

//
void main ()
{
	aoc = 0.0f;

//p is the sample, for which aoc is computed
	vec4 p_nd = get_normal_depth(param);
	vec4 p_clip = get_clip_coordinates(param, p_nd.w);
	vec4 p_pos = reproject(modelviewprojection_inv_transform, p_clip);
	vec4 p_campos = reproject(projection_inv_transform, p_clip);

//screen space radius of sphere of influence  (projection of its size in range -1, 1)
  float rf =	clamp(
					  		reproject(projection_transform, vec4(OCCLUDER_MAX_DISTANCE, 0, p_campos.z, 1)).x / 2,
					  		OCCLUDER_MIN_PROJECTED_SIZE / viewport_size.x,
					  		OCCLUDER_MAX_PROJECTED_SIZE / viewport_size.x);
 	int step = int(ceil(sampling_pattern.length/ pow(rf * viewport_size.x, 2)));

//for each sample compute occlussion estimation and add it to result
	for(int i = 0; i < sampling_pattern.length; i+= step)
	{
		vec2 oc_param = param + sampling_pattern[i] * rf;

		vec4 o_nd = get_normal_depth( oc_param);
		vec4 o_clip = get_clip_coordinates( oc_param, o_nd.w);
		vec4 o_pos = reproject( modelviewprojection_inv_transform, o_clip);

		float o_r =  reproject(projection_inv_transform, vec4(1/viewport_size.x, 0, o_nd.w, 1)).x;

		float s_omega = 2 * PI * (1 - cos( asin( o_r/ distance(o_pos, p_pos))));
		aoc += s_omega * max(dot( normalize(o_pos.xyz - p_pos.xyz), p_nd.xyz), 0);
	}
}