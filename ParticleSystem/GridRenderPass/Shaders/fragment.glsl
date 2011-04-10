#version 330

in float outparam;

void main ()
{
	//vec4 color = vec4(0, 1 - abs(2*outparam - 1), 0, 1);
	vec4 color = vec4(1, 0, 0, 1);
	gl_FragColor = vec4(color.xyz, 0.3 * outparam);
}	