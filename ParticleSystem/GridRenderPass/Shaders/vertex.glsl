#version 330
uniform mat4 modelview_transform;

in vec3 pos;
in float param;

out float outparam;

void main () {
	gl_Position = modelview_transform * vec4(pos, 1);
	outparam = param;
}