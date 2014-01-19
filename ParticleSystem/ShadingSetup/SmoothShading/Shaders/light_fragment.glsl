#version 330
uniform mat4 projection_transform;
uniform float particle_brightness;
uniform float smooth_shape_sharpness;
uniform int particle_shape;

uniform sampler2D custom_texture;

in VertexData
{
	vec2 param;
	vec3 fcolor;
	float z_orig;
	float z_maxdelta;
	vec4 world_cam_x_dir;
	vec4 world_cam_y_dir;
	vec4 world_cam_z_dir;
};

void main ()
{
	vec4 color;
	vec2 cparam = 2 * (param - 0.5f);

	switch(particle_shape)
	{
		//hard dot
		case 1:
			float dist = length(cparam) * 1.1f;
			color = vec4(0.5 * (normalize(fcolor) + 1) * pow(1.1f - dist, smooth_shape_sharpness),  particle_brightness * 0.001f);
			break;

		//texture
		case 3:
			color = texture (custom_texture, param);
		break;

		default:
			color = vec4((fcolor * 2 - 1), 0.01);
			break;
	}

	gl_FragColor = color;
}	