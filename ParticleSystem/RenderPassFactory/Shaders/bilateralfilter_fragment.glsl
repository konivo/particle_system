#version 330
//
uniform sampler2D source_texture;
//
uniform vec2 viewport_size;
//
uniform bool horizontal;
//
uniform vec4 K = vec4(5, 5, 5, 0);

const float[] g = float[](0, 0.05, 0.09, 0.12, 0.15, 0.16 );

//param in range (0, 0) to (1, 1)
in VertexData
{
	vec2 param;
}
IN_VertexData;

//computed ambient occlusion estimate
out vec4 OUT_FragData_result;

//
void main ()
{
	float ksize = 1;
	vec2 blurSize = horizontal ? vec2(ksize/viewport_size.x, 0) : vec2(0, ksize/viewport_size.y);

	vec4 sum = vec4(0.0);
	vec4 val_S = texture2D(source_texture, IN_VertexData.param) * K;
	float s2norm = dot(val_S, val_S);
	float normFactor = 0;

	for( int i = -4; i <= 4 ; i++)
	{
		vec4 tval = texture2D(source_texture, IN_VertexData.param - i * blurSize);
		vec4 tval_K = tval * K;
		float k1 = s2norm - dot(tval_K, val_S);
		float k2 = dot(abs(tval_K - val_S), vec4(1));
		int gindex = 5 - abs(i);
		int iindex = 5 - clamp( int(abs(0.5f * k2 + 0.5f * k1)), 0, 5);

		sum += tval * g[gindex] * g[iindex];
		normFactor += g[gindex] * g[iindex];
	}

	sum /= normFactor;
	OUT_FragData_result = sum;
}
