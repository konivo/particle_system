#version 400
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

const float[] g = float[](0, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15 );

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

#pragma include <RenderPassFactory.Shaders.common.include>

//
void main ()
{
	float ksize = 8;

	vec4 p_nd = texture(normaldepth_texture, param) * 2 - 1;
	vec4 p_clip = get_clip_coordinates(param, p_nd.w);
	vec4 p_pos = reproject(modelviewprojection_inv_transform, p_clip);

	vec4 tangent = texture2D(tangent_texture, param) * 2 - 1;
	vec4 projected_tangent =reproject(modelviewprojection_transform, p_pos + vec4(tangent.xyz, 0)) - reproject(modelviewprojection_transform, p_pos);

	vec4 sum = vec4(0.0);
	//projected_tangent.y = -projected_tangent.y;
	vec2 blurSize = ksize * normalize(projected_tangent.xy)/viewport_size;
	vec4 val_S = texture(normaldepth_texture, param) * K;
	float s2norm = dot(val_S, val_S);
	float normFactor = 0;

	for( int i = -9; i <= 9 ; i++)
	{
		vec4 tval = texture2D(normaldepth_texture, param - i * blurSize);
		vec4 tval_K = tval * K;
		float k1 = s2norm - dot(tval_K, val_S);
		float k2 = dot(abs(tval_K - val_S), vec4(1));
		int gindex = 10 - abs(i);
		int iindex = 10 - clamp( int(abs(0.5f * k2 + 0.5f * k1)), 0, 10);

		sum += tval * g[gindex] * g[iindex];
		normFactor += g[gindex] * g[iindex];
	}

	sum /= normFactor;
	result = sum;
}