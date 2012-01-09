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
};

//computed ambient occlusion estimate
out Fragdata
{
	vec4 result;
};

//
void main ()
{
	vec2 blurSize = horizontal ? vec2(1.0/viewport_size.x, 0) : vec2(0, 1.0/viewport_size.y);

	vec4 sum = vec4(0.0);
	vec4 val_K = texture2D(source_texture, param) * K;
	float val_S = val_K.x + val_K.y + val_K.z + val_K.w;

	float normFactor = 0;

	for( int i = -4; i <= 4 ; i++)
	{
		vec4 tval = texture2D(source_texture, param - i * blurSize);
		vec4 tval_K = tval * K;
		int gindex = 5 - abs(i);
		int iindex = 5 - clamp( int( tval_K.x + tval_K.y + tval_K.z + tval_K.w - val_S), 0, 5);

		sum += tval * g[gindex] * g[iindex];
		normFactor += g[gindex] * g[iindex];
	}

	sum /= normFactor;
	result = sum;
}