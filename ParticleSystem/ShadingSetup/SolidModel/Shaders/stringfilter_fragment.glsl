#version 400
#pragma include <RenderPassFactory.Shaders.common.include>

uniform mat4 modelviewprojection_transform;
uniform mat4 modelviewprojection_inv_transform;
uniform mat4 projection_transform;
uniform mat4 projection_inv_transform;
//
uniform sampler2D normaldepth_texture;
//
uniform sampler2D tangent_texture;
//
uniform vec2 viewport_size;
//
uniform bool horizontal;
//
uniform vec4 K = vec4(0, 0, 0, 0);

const float[] g = float[](0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 );
const float[] h = float[](0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 );

const int SAMPLES_COUNT = 3;

//param in range (0, 0) to (1, 1)
in VertexData
{
	vec2 param;
};

//computed ambient occlusion estimate
out Fragdata
{
	vec4 result;
};

//
void main ()
{
	float ksize = 0.05251915;

	vec4 p_nd = texture(normaldepth_texture, param) * 2 - 1;
	vec4 p_clip = get_clip_coordinates(param, p_nd.w);
	vec4 p_pos = reproject(modelviewprojection_inv_transform, p_clip);

	vec4 tangent = texture2D(tangent_texture, param) * 2 - 1;
	vec4 projected_tangent =reproject(modelviewprojection_transform, p_pos + vec4(tangent.xyz, 0)) - reproject(modelviewprojection_transform, p_pos);

	vec4 sum = vec4(0.0);
	vec2 blurSize = ksize * projected_tangent.xy;
	vec4 val_S = texture(normaldepth_texture, param) * K;
	float s2norm = dot(val_S, val_S);
	float normFactor = 0;

	for( int i = -SAMPLES_COUNT; i <= SAMPLES_COUNT ; i++)
	{
		vec4 tval = texture2D(normaldepth_texture, param - i * blurSize);
		vec4 tval_K = tval * K;
		float k1 = s2norm - dot(tval_K, val_S);
		float k2 = dot(abs(tval_K - val_S), vec4(1));
		int gindex = (SAMPLES_COUNT + 1) - abs(i);
		int iindex = (SAMPLES_COUNT + 1) - clamp( int(abs(0.5f * k2/* + 0.5f * k1*/)), 0, (SAMPLES_COUNT + 1));

		vec3 oldNormal = tval.xyz * 2 - 1;
		vec3 newNormal = normalize(oldNormal - dot(oldNormal, tangent.xyz) * tangent.xyz);
		sum += vec4(newNormal * 0.5 + 0.5, tval.w) * g[gindex] * h[iindex];
		normFactor += g[gindex] * h[iindex];
	}

	sum /= normFactor;
	result = sum;
}

//
void main2 ()
{
	float ksize = 0.061915;

	vec4 p_nd = texture(normaldepth_texture, param) * 2 - 1;
	vec4 tangent = texture2D(tangent_texture, param) * 2 - 1;
	vec3 t = normalize(tangent.xyz);
	vec3 newNormal = normalize(p_nd.xyz - dot(p_nd.xyz, t) * t);

	result = vec4(newNormal * 0.5 + 0.5, p_nd.w * 0.5 + 0.5);
}

//
void main1 ()
{
	float ksize = 0.61915;

	vec4 p_nd = texture(normaldepth_texture, param) * 2 - 1;
	vec4 p_clip = get_clip_coordinates(param, p_nd.w);
	vec4 p_pos = reproject(modelviewprojection_inv_transform, p_clip);

	vec4 tangent = texture2D(tangent_texture, param) * 2 - 1;
	vec4 projected_tangent =reproject(modelviewprojection_transform, p_pos + vec4(tangent.xyz, 0)) - reproject(modelviewprojection_transform, p_pos);

	vec4 sum = vec4(0.0);
	//projected_tangent = normalize(projected_tangent);
	vec2 blurSize = ksize * projected_tangent.xy;
	vec4 val_S = texture(normaldepth_texture, param) * K;
	float s2norm = dot(val_S, val_S);
	float normFactor = 0;

	for( int i = -SAMPLES_COUNT; i <= SAMPLES_COUNT ; i++)
	{
		vec4 tval = texture2D(normaldepth_texture, param - i * blurSize);
		vec4 tval_K = tval * K;
		float k1 = s2norm - dot(tval_K, val_S);
		float k2 = dot(abs(tval_K - val_S), vec4(1));
		int gindex = (SAMPLES_COUNT + 1) - abs(i);
		int iindex = (SAMPLES_COUNT + 1) - clamp( int(abs(0.5f * k2/* + 0.5f * k1*/)), 0, (SAMPLES_COUNT + 1));

		sum += tval * g[gindex] * h[iindex];
		normFactor += g[gindex] * h[iindex];
	}

	sum /= normFactor;
	result = sum;
}

