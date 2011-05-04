#version 330
uniform mat4 projection_transform;
uniform float particle_brightness;
uniform float smooth_shape_sharpness;
uniform int particle_shape;
uniform sampler2D custom_texture;

in Outdata{
	vec2 param;
	vec3 fcolor;
	float z_orig;
	float z_maxdelta;
};

void main ()
{
	vec4 color;
	vec2 cparam = 2 * (param - 0.5f);
	gl_FragDepth = gl_FragCoord.z;

	switch(particle_shape)
	{
		//hard dot
		case 1:
			float dist = length(cparam) * 1.1f;
			color = vec4(fcolor * pow(1.1f - dist, smooth_shape_sharpness),  particle_brightness * 0.001f);

			gl_FragColor = color;
			break;
			
		//todo: dz need not to change linearly with distance
		//sphere
		case 2:
			float dist2 = dot(cparam, cparam);

			if(dist2 > 1)
				discard;

			float z_delta = sqrt(1 - dist2) * z_maxdelta;
			vec4 z = projection_transform * vec4(0, 0, z_orig, 1);
			z /= z.w;

			vec4 zd = projection_transform * vec4(0, 0, z_orig + z_delta, 1);
			zd /= zd.w;

			color = vec4(fcolor * pow(1 - dist2, smooth_shape_sharpness) * particle_brightness * 0.01f, 1.0f);
			gl_FragDepth = (gl_FragCoord.z + zd.z) - z.z;
			gl_FragColor = color;
			break;

		//texture
		case 3:
			gl_FragColor = texture (custom_texture, param);
		break;

		default:
			gl_FragColor = vec4(fcolor, 0.01);
			break;
	}
}	