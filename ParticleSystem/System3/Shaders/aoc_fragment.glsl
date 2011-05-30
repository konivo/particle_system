#version 330
uniform mat4 modelviewprojection_transform;
uniform mat4 modelviewprojection_inv_transform;
uniform mat4 projection_transform;
uniform mat4 projection_inv_transform;
uniform vec2 viewport_size;

//todo: shall be uniformly distributed. Look for some advice, how to make it properly
uniform vec2[96] sampling_pattern;
uniform int sampling_pattern_len;

//
uniform sampler2D normaldepth_texture;

//maximum distance of an occluder in the world space
const float OCCLUDER_MAX_DISTANCE = 2.5;

//these two constants will limit how big area in image space will be sampled.
//farther areas will be smaller in size and thus will contain less samples,
//less far areas will be bigger in screen size and will be covered by more samples.
//Samples count should change with square of projected screen size?
const float PROJECTED_OCCLUDER_DISTANCE_MIN_SIZE = 2;
const float PROJECTED_OCCLUDER_DISTANCE_MAX_SIZE = 200;

//determines how big fraction of the samples will be used for the minimal computed projection of occluder distance
const float MINIMAL_SAMPLES_COUNT_RATIO = 1.02;

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
float compute_occluded_radius_projection(float camera_space_dist)
{
	return
		clamp(reproject(projection_transform, vec4(OCCLUDER_MAX_DISTANCE, 0, camera_space_dist, 1)).x,
	  			PROJECTED_OCCLUDER_DISTANCE_MIN_SIZE / viewport_size.x,
	  			PROJECTED_OCCLUDER_DISTANCE_MAX_SIZE / viewport_size.x);
}

//
int compute_step_from_occluded_screen_size(float rf)
{
//compute projection screen size
	rf *= viewport_size.x;

//compute number of samples needed (step size)
	float ssize = sampling_pattern_len * clamp(MINIMAL_SAMPLES_COUNT_RATIO, 0, 1);
	float msize = sampling_pattern_len * (1 - clamp(MINIMAL_SAMPLES_COUNT_RATIO, 0, 1));

	float min_dist_squared = pow(PROJECTED_OCCLUDER_DISTANCE_MIN_SIZE, 2);
	float max_dist_squared = pow(PROJECTED_OCCLUDER_DISTANCE_MAX_SIZE, 2);
	float rf_squared = pow(rf, 2);

//linear interpolation between (msize + ssize) and ssize, parameter is squared projection size
	float samples_cnt =
		(msize / (max_dist_squared - min_dist_squared)) *
		(rf_squared - min_dist_squared) +  ssize;

 	float step = clamp(sampling_pattern_len / samples_cnt, 1, sampling_pattern_len);

 	return int(step);
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
  float rf =	compute_occluded_radius_projection( p_campos.z );

//compute number of samples needed (step size)
 	int step = compute_step_from_occluded_screen_size(rf);

//for each sample compute occlussion estimation and add it to result
	for(int i = 0; i < sampling_pattern_len; i+= step)
	{
		vec2 oc_param = param + sampling_pattern[i] * rf;

		vec4 o_nd = get_normal_depth( oc_param);
		vec4 o_clip = get_clip_coordinates( oc_param, o_nd.w);
		vec4 o_pos = reproject( modelviewprojection_inv_transform, o_clip);

		float o_r =  reproject(projection_inv_transform, vec4(1/viewport_size.x, 0, o_nd.w, 1)).x;

		//correction to prevent occlusion from itself or from neighbours which are on the same tangent plane
		o_pos -= o_r * vec4(o_nd.xyz, 0);

		float o_p_distance = distance(o_pos, p_pos);
		float s_omega = 2 * PI * (1 - cos( asin( o_r/ o_p_distance)));
		aoc +=
			o_p_distance <= OCCLUDER_MAX_DISTANCE ?
			s_omega * max(dot( normalize(o_pos.xyz - p_pos.xyz), p_nd.xyz), 0):
			0;
	}

	//aoc = step / float(sampling_pattern_len);
	//aoc = rf * viewport_size.x/ PROJECTED_OCCLUDER_DISTANCE_MAX_SIZE;
	//
	aoc *= 100;
}