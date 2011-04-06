#version 330
uniform mat4 modelview_transform;

in vec3 pos;

void main () {
	gl_Position = modelview_transform * vec4(pos, 1);
}