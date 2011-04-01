#version 330

in vec2 param;

void main ()
{
	float dist = length(param - 0.5f) * 2.2;

	vec4 color = vec4(0, pow(1.1 - dist, 4) , 0, max(pow(0.9 - dist, 12), 0.1));
	gl_FragColor = vec4(color.xyzw);
}	