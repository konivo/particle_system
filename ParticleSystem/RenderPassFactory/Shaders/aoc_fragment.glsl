#version 430
#pragma include <RenderPassFactory.Shaders.common.include>

///////////////////////
//model-view matrices//
uniform mat4 modelviewprojection_transform;
uniform mat4 modelviewprojection_inv_transform;
uniform mat4 projection_transform;
uniform mat4 projection_inv_transform;
uniform vec2 viewport_size;

/////////////////////
//SSAO settings//
//how many samples will be used for current pixel (max is 256)
uniform int sampling_pattern_len;

//texture holding depth in projection space and normal in camera space
uniform sampler2D normaldepth_texture;

//maximum distance of an occluder in the world space
uniform float OCCLUDER_MAX_DISTANCE = 35.5;

//if true, occluder projection will be equal to max size.
//TODO: when true, occluder max distance has to be recomputed
uniform bool USE_CONSTANT_OCCLUDER_PROJECTION = false;

//these two constants will limit how big area in image space will be sampled.
//farther areas will be smaller in size and thus will contain less samples,
//less far areas will be bigger in screen size and will be covered by more samples.
//Samples count should change with square of projected screen size?
uniform float PROJECTED_OCCLUDER_DISTANCE_MIN_SIZE = 2;
uniform float PROJECTED_OCCLUDER_DISTANCE_MAX_SIZE = 35;

//determines how big fraction of the samples will be used for the minimal computed projection of occluder distance
uniform float MINIMAL_SAMPLES_COUNT_RATIO = 0.5;
uniform float AOC_STRENGTH = 0.1;
uniform float AOC_BIAS = 0.1;


//param in range (0, 0) to (1, 1)
in VertexData
{
	vec2 param;
}
IN_VertexData;

//computed ambient occlusion estimate
out float OUT_FragData_aoc;

//
vec4 get_normal_depth (vec2 param)
{
	vec4 result = texture(normaldepth_texture, param);
	result = result * 2 - 1;

	return result;
}

//
vec2 compute_occluded_radius_projection(float camera_space_dist)
{
	//for constant projection this is important just for determining aspect ratio
	vec2 projection = reproject(projection_transform, vec4(OCCLUDER_MAX_DISTANCE, OCCLUDER_MAX_DISTANCE, camera_space_dist, 1)).xy * 0.5;

	if(USE_CONSTANT_OCCLUDER_PROJECTION)
		return projection * PROJECTED_OCCLUDER_DISTANCE_MAX_SIZE / (viewport_size.x * projection.x);
	else
		return clamp(projection,
			vec2(PROJECTED_OCCLUDER_DISTANCE_MIN_SIZE / viewport_size.x),
	  	vec2(PROJECTED_OCCLUDER_DISTANCE_MAX_SIZE / viewport_size.x));
}

//
int compute_step_from_occluded_screen_size(vec2 rf)
{
//compute projection screen size
	rf *= viewport_size;

//compute number of samples needed (step size)
	float ssize = sampling_pattern_len * clamp(MINIMAL_SAMPLES_COUNT_RATIO, 0, 1);
	float msize = sampling_pattern_len * (1 - clamp(MINIMAL_SAMPLES_COUNT_RATIO, 0, 1));

	float min_dist_squared = pow(PROJECTED_OCCLUDER_DISTANCE_MIN_SIZE, 2);
	float max_dist_squared = pow(PROJECTED_OCCLUDER_DISTANCE_MAX_SIZE, 2);
	float rf_squared = pow(max(rf.x, rf.y), 2);

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
	OUT_FragData_aoc = 0.0f;
	init_sampling();

//p is the sample, for which aoc is computed
	vec4 p_nd = get_normal_depth(IN_VertexData.param);
	vec4 p_clip = get_clip_coordinates(IN_VertexData.param, p_nd.w);
	vec4 p_pos = reproject(modelviewprojection_inv_transform, p_clip);
	vec4 p_campos = reproject(projection_inv_transform, p_clip);

//screen space radius of sphere of influence  (projection of its size in range -1, 1)
  vec2 rf =	compute_occluded_radius_projection( p_campos.z );

//compute number of samples needed (step size)
 	int step = compute_step_from_occluded_screen_size(rf);

//for each sample compute occlussion estimation and add it to result
	for(int i = 0; i < sampling_pattern_len; i+= step)
	{
		vec2 oc_param = IN_VertexData.param + get_sampling_point(i) * rf;

		vec4 o_nd = get_normal_depth( oc_param);
		vec4 o_clip = get_clip_coordinates( oc_param, o_nd.w);
		vec4 o_pos = reproject( modelviewprojection_inv_transform, o_clip);

		float o_r =  reproject(projection_inv_transform, vec4(2.0 / viewport_size.x, 0, o_nd.w, 1)).x;

		//correction to prevent occlusion from itself or from neighbours which are on the same tangent plane
		o_pos -= o_r * vec4(o_nd.xyz, 0);

		float o_p_distance = distance(o_pos, p_pos);
		float s_omega = 2 * PI * (1 - cos( asin( clamp(o_r / o_p_distance, 0, 1))));
		OUT_FragData_aoc +=
			o_p_distance <= OCCLUDER_MAX_DISTANCE ?
			s_omega * max(dot( normalize(o_pos.xyz - p_pos.xyz), normalize(p_nd.xyz)), 0):
			0;
	}

	OUT_FragData_aoc = pow(OUT_FragData_aoc, AOC_BIAS) * AOC_STRENGTH;
}
