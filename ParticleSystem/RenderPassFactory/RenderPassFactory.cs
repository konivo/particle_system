using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
namespace opentk
{
	public static class RenderPassFactory
	{
		public static RenderPass CreateFullscreenQuad (string passName, string passNamespace, IValueProvider<Vector2> viewportSize, Action<GameWindow> beforeState, Action<GameWindow> beforeRender, params StatePart[] stateParts)
		{
			var shaders =
				RenderPass.GetShaders("fullquad", "RenderPassFactory")
				.Concat(
					        RenderPass.GetShaders(passName, passNamespace)
				);

			Action<GameWindow> render =
			x =>
			{
				GL.Viewport(0, 0, (int)viewportSize.Value.X, (int)viewportSize.Value.Y);
				//draw one point
				GL.DrawArrays (BeginMode.Points, 0, 1);
			};

			return new SeparateProgramPass<object>(passName, beforeState, beforeRender, render, shaders, stateParts);
		}

		/*

		 #version 330
uniform mat4 modelviewprojection_transform;
uniform mat4 modelviewprojection_inv_transform;
uniform mat4 projection_transform;
uniform mat4 projection_inv_transform;
uniform vec2 viewport_size;

//todo: shall be uniformly distributed. Look for some advice, how to make it properly
uniform vec2[256] sampling_pattern;
uniform int sampling_pattern_len;

//
uniform sampler2D normaldepth_texture;
		*/
		public static RenderPass CreateAoc (
		TextureBase normalDepth, //computed world-space normal and clipping space depth (depth in range 0, 1)
		TextureBase aoc, //where the result will be stored, results will be stored to the first component of the texture
		IValueProvider<Matrix4> modelviewprojection,
		IValueProvider<Matrix4> modelviewprojection_inv,
		IValueProvider<Matrix4> projection,
		IValueProvider<Matrix4> projection_inv,
		IValueProvider<int> samplesCount //how many samples will be used for occlusion estimation
		)
		{
			var viewport = ValueProvider.Create(() => new Vector2(aoc.Width, aoc.Height));

			var uniformState = new UniformState ();
			uniformState.Set ("viewport_size", viewport);
			uniformState.Set ("sampling_pattern", MathHelper2.RandomVectorSet(samplesCount.Value, new Vector2(1, 1) ));
			uniformState.Set ("sampling_pattern_len", samplesCount);

			uniformState.Set ("modelviewprojection_transform", modelviewprojection);
			uniformState.Set ("modelviewprojection_inv_transform", modelviewprojection_inv);
			uniformState.Set ("projection_transform", projection);
			uniformState.Set ("projection_inv_transform", projection_inv);

			return CreateFullscreenQuad(
			                            "aoc", "RenderPassFactory",
			                            viewport,
			                            null,
			                            window =>
			                            {
				GL.Clear(ClearBufferMask.ColorBufferBit);
						GL.Disable (EnableCap.DepthTest);
						GL.Disable (EnableCap.Blend);
			                            },
			                            //pass state
			new FramebufferBindingSet(
					   new DrawFramebufferBinding { VariableName = "Fragdata.aoc", Texture = aoc }
			),
				 uniformState,
				 new TextureBindingSet(
				   new TextureBinding { VariableName = "normaldepth_texture", Texture = normalDepth }
				 )
			                            );
		}
	}
}

