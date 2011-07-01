#version 330
//
uniform sampler2D source_texture;
//
uniform vec2 viewport_size;

uniform bool horizontal;

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

	// blur in y (vertical)
	// take nine samples, with the distance blurSize between them
	sum += texture2D(source_texture, param - 4.0*blurSize) * 0.05;
	sum += texture2D(source_texture, param - 3.0*blurSize) * 0.09;
	sum += texture2D(source_texture, param - 2.0*blurSize) * 0.12;
	sum += texture2D(source_texture, param - blurSize) * 0.15;
	sum += texture2D(source_texture, param) * 0.16;
	sum += texture2D(source_texture, param + blurSize) * 0.15;
	sum += texture2D(source_texture, param + 2.0*blurSize) * 0.12;
	sum += texture2D(source_texture, param + 3.0*blurSize) * 0.09;
	sum += texture2D(source_texture, param + 4.0*blurSize) * 0.05;

	result = sum;
}