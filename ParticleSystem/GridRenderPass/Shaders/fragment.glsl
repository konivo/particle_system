#version 330

void main ()
{
	vec4 color = vec4(0, 1, 0, 1);
	gl_FragColor = vec4(color.xyz, 1);
}	