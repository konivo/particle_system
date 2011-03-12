#version 330

uniform vec4 color;
uniform float red;
uniform float blue;
uniform float green;

uniform float[] colors;
uniform vec4[] colors2;

in vec2 sprite_coord;

void main () {
	
	vec4 color = vec4(0);
	color += colors2[0];
	color += colors2[1];
	color += colors2[2];
	
	color = vec4(1, 0, 0, 0);
	float len = length(sprite_coord);
	float alpha = step(0, 1 - len)* pow(len, 4);

	gl_FragColor = vec4(color.xyz, alpha);
}	