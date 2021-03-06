#version 450
#extension GL_ARB_separate_shader_objects : enable

layout(set=0, binding = 0) uniform UniformBufferObject {
    mat4 prjViewWorld; 
    mat4 view;
} ubo;




layout(location = 0) in vec3 inPosition;
layout(location = 0) out vec3 fragColor;

out gl_PerVertex {
    vec4 gl_Position;
};

void main() {
    gl_Position = ubo.prjViewWorld   * vec4(inPosition, 1.0);
    fragColor = vec3(0.5,0.5,0.5);
}