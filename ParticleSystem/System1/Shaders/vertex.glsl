#version 330
uniform mat4 modelview_transform; 

in vec3 vertex_pos;
in vec4 sprite_pos;
in vec4 sprite_colorandsize;

out vec2 sprite_coord;

void main () {
	gl_Position = modelview_transform * vec4(vertex_pos.xyz * sprite_colorandsize.w + sprite_pos.xyz, 1);
	sprite_coord = vertex_pos.xy;
}