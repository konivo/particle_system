#version 330
uniform float particle_brightness;
uniform float smooth_shape_sharpness;
uniform int particle_shape;

in vec2 param;
in vec3 fcolor;
in float zscale;

void main ()
{
	vec4 color;
	vec2 param = 2 * (param - 0.5f);
	float dist = length(param);
	gl_FragDepth = gl_FragCoord.z;

	switch(particle_shape)
	{
		//hard dot
		case 1:
			dist *= 1.1f;
			color = vec4(fcolor * pow(1.1 - dist, smooth_shape_sharpness),  particle_brightness * 0.001);

			gl_FragColor = color;
			break;
			
		//todo: dz need not to change linearly with distance
		//sphere
		case 2:
			float depth_delta = (1 - dist) * zscale;
			if(dist > 1)
				discard;

			color = vec4(fcolor * pow(1 - dist, smooth_shape_sharpness) * particle_brightness * 0.01f, 1.0f);
			gl_FragDepth = gl_FragCoord.z - depth_delta;
			gl_FragColor = color;
			break;

		//bubble
		case 3:
		break;

		default:
			gl_FragColor = vec4(fcolor, 0.01);
			break;
	}
}	