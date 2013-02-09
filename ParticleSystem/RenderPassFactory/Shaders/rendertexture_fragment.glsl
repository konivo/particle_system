#version 430
//
uniform sampler2D source_texture;
uniform sampler2D depth_texture;
//
uniform vec2 viewport_size;

//param in range (0, 0) to (1, 1)
in VertexData
{
	vec2 param;
}
IN_VertexData;

//
void main ()
{
	gl_FragColor = texture2D(source_texture, IN_VertexData.param);
	gl_FragDepth = texture2D(depth_texture, IN_VertexData.param).x;
}