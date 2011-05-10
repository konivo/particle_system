#version 330
uniform mat4 projection_transform;
uniform float particle_brightness;
uniform float smooth_shape_sharpness;
uniform int particle_shape;
uniform sampler2D custom_texture;

in Outdata
{
	vec2 param;
	vec3 fcolor;
	float z_orig;
	float z_maxdelta;
};

out Fragdata
{
	float WorldDepth;
	vec4 UV_ColorIndex_None;
};

void main ()
{
	vec2 cparam = 2 * (param - 0.5f);
	float dist2 = dot(cparam, cparam);

	if(dist2 > 1)
		discard;

	float z_delta = sqrt(1 - dist2) * z_maxdelta;
	vec4 z = projection_transform * vec4(0, 0, z_orig, 1);
	z /= z.w;

	vec4 zd = projection_transform * vec4(0, 0, z_orig + z_delta, 1);
	zd /= zd.w;

	gl_FragDepth = (gl_FragCoord.z + zd.z) - z.z;

	UV_ColorIndex_None = vec4(param, 0.5f, 0);
	WorldDepth = z_orig + z_delta;
}