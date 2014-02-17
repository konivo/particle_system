using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.Scene;
using opentk.ShadingSetup;

namespace opentk
{
	public static partial class RenderPassFactory
	{
		/// <summary>
		/// given color and depth textures, render them.
		/// </summary>
		private static RenderPass CreateSolidBox
		(
			 FramebufferBindingSet targets,
			 BufferObject<Vector4> sprite_pos_buffer,
			 BufferObject<Vector4> sprite_color_buffer,
			 BufferObject<Vector4> sprite_dimensions_buffer,
			 BufferObject<Matrix4> sprite_rotation_local_buffer,
			 BufferObject<Matrix4> sprite_rotation_buffer,
			 IValueProvider<Vector2> viewport,
			 IValueProvider<int> particles_count,
			 IValueProvider<float> particle_scale_factor,
			 IValueProvider<string> fragdepthroutine,
			 IValueProvider<string> outputroutine,
			 ModelViewProjectionParameters mvp,
			 UniformState subroutineMapping,
			 IEnumerable<Shader> subroutines
		)
		{
			var uniform_state = subroutineMapping != null? new UniformState (subroutineMapping): new UniformState();
			uniform_state.Set ("viewport_size", viewport);
			uniform_state.Set ("particle_scale_factor", particle_scale_factor);
			uniform_state.Set ("u_SetFragmentDepth", ShaderType.FragmentShader, fragdepthroutine);
			uniform_state.Set ("u_SetOutputs", ShaderType.FragmentShader, outputroutine);
			mvp.SetUniforms("", uniform_state);

			var array_state =
				new ArrayObject (
					new VertexAttribute { AttributeName = "sprite_pos", Buffer = sprite_pos_buffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float },
					new VertexAttribute { AttributeName = "sprite_color", Buffer = sprite_color_buffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float },
					new VertexAttribute { AttributeName = "sprite_dimensions", Buffer = sprite_dimensions_buffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float },
					new VertexAttribute { AttributeName = "sprite_rotation_local", Buffer = sprite_rotation_local_buffer, Size = 16, Stride = 64, Type = VertexAttribPointerType.Float },
					new VertexAttribute { AttributeName = "sprite_rotation", Buffer = sprite_rotation_buffer, Size = 16, Stride = 64, Type = VertexAttribPointerType.Float }
				);

			var shaders = SeparateProgramPass.GetShaders("solid_box", "RenderPassFactory");
			shaders = shaders.Concat(subroutines ?? new Shader[0]).ToArray();

			//
			var resultPass = new SeparateProgramPass
			(
				 //the name of the pass-program
				 "solid_box_RenderPassFactory",
				 //before state
				 null,
				 //before render
				 null,
				 //render code
				 (window) =>
				 {
					GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
				  GL.Enable (EnableCap.DepthTest);
					GL.DepthMask(true);
					GL.DepthFunc (DepthFunction.Less);
					GL.Disable (EnableCap.Blend);

					//setup viewport
					GL.Viewport(0, 0, (int)viewport.Value.X, (int)viewport.Value.Y);
					GL.DrawArrays (BeginMode.Points, 0, particles_count.Value);
				 },
				 //shaders
				 shaders,

				 //pass state
				 array_state,
				 uniform_state,
				 targets
			);

			return resultPass;
		}

		/// <summary>
		/// given color and depth textures, render them.
		/// </summary>
		public static RenderPass CreateSolidBox
		(
			 TextureBase normal_depth_target,
			 TextureBase uv_colorindex_target,
			 TextureBase depth_texture,
			 BufferObject<Vector4> sprite_pos_buffer,
			 BufferObject<Vector4> sprite_color_buffer,
			 BufferObject<Vector4> sprite_dimensions_buffer,
			 BufferObject<Matrix4> sprite_rotation_local_buffer,
			 BufferObject<Matrix4> sprite_rotation_buffer,
			 IValueProvider<int> particles_count,
			 IValueProvider<float> particle_scale_factor,
			 ModelViewProjectionParameters mvp
		)
		{
			var viewport = ValueProvider.Create (() => new Vector2 (depth_texture.Width, depth_texture.Height));
			var fragdepthroutine = ValueProvider.Create (() => "FragDepthDefault");
			var outputroutine = ValueProvider.Create (() => "SetOutputsDefault");

			return CreateSolidBox
			(
				 new FramebufferBindingSet(
				  new DrawFramebufferBinding { Attachment = FramebufferAttachment.DepthAttachment, Texture = depth_texture },
				  new DrawFramebufferBinding { VariableName = "uv_colorindex_none", Texture = uv_colorindex_target },
				  new DrawFramebufferBinding { VariableName = "normal_depth", Texture = normal_depth_target }
				 ),
				 sprite_pos_buffer, sprite_color_buffer, sprite_dimensions_buffer, sprite_rotation_local_buffer, sprite_rotation_buffer,
				 viewport,
				 particles_count, particle_scale_factor, fragdepthroutine, outputroutine,
				 mvp,
				 null,
				 null
			);
		}

		/// <summary>
		/// given color and depth textures, render them.
		/// </summary>
		public static RenderPass CreateSolidBox
		(
			 TextureBase depth_texture,
			 BufferObject<Vector4> sprite_pos_buffer,
			 BufferObject<Vector4> sprite_color_buffer,
			 BufferObject<Vector4> sprite_dimensions_buffer,
			 BufferObject<Matrix4> sprite_rotation_local_buffer,
			 BufferObject<Matrix4> sprite_rotation_buffer,
			 IValueProvider<int> particles_count,
				IValueProvider<float> particle_scale_factor,
				IValueProvider<string> fragdepthroutine,
			 ModelViewProjectionParameters mvp
		)
		{
			var viewport = ValueProvider.Create (() => new Vector2 (depth_texture.Width, depth_texture.Height));
			var outputroutine = ValueProvider.Create (() => "SetOutputsNone");
			return CreateSolidBox
			(
				 new FramebufferBindingSet(
				  new DrawFramebufferBinding { Attachment = FramebufferAttachment.DepthAttachment, Texture = depth_texture }
				 ),
				 sprite_pos_buffer, sprite_color_buffer, sprite_dimensions_buffer, sprite_rotation_local_buffer, sprite_rotation_buffer,
				 viewport,
				 particles_count, particle_scale_factor, fragdepthroutine, outputroutine,
				 mvp,
				 null,
				 null
			);
		}
	}
}

