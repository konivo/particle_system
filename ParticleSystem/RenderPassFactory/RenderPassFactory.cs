using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
namespace opentk
{
	public static partial class RenderPassFactory
	{
		public static RenderPass CreateFullscreenQuad (string passName, string passNamespace, IValueProvider<Vector2> viewportSize, Action<GameWindow> beforeState, Action<GameWindow> beforeRender, params StatePart[] stateParts)
		{
			var shaders = RenderPass.GetShaders ("fullquad", "RenderPassFactory").Concat (RenderPass.GetShaders (passName, passNamespace));
			
			Action<GameWindow> render = x =>
			{
				GL.Viewport (0, 0, (int)viewportSize.Value.X, (int)viewportSize.Value.Y);
				//draw one point
				GL.DrawArrays (BeginMode.Points, 0, 1);
			};
			
			return new SeparateProgramPass<object> (passName, beforeState, beforeRender, render, shaders, stateParts);
		}

		//
		public static RenderPass CreateBlurFilter
		(
			 TextureBase source,
			 TextureBase interm,
			 TextureBase result)
		{
			return CreateSeparableFilter("blur", "RenderPassFactory", source, interm, result);
		}

		//
		public static RenderPass CreateBilateralFilter
		(
			 TextureBase source,
			 TextureBase interm,
			 TextureBase result)
		{
			return CreateSeparableFilter("bilateralfilter", "RenderPassFactory", source, interm, result);
		}

		//
		public static RenderPass CreateFxaa3Filter
		(
			 TextureBase source,
			 TextureBase result)
		{
			return CreateFilter("fxaa3", "RenderPassFactory", source, result);
		}

		/// <summary>
		/// creates non-separable filter pass
		/// </summary>
		public static RenderPass CreateFilter
		(
			 string filterName,
			 string filterNamespace,
			 TextureBase source,
			 TextureBase result)
		{
			var viewport = ValueProvider.Create (() => new Vector2 (result.Width, result.Height));
			var uniformState = new UniformState ();
			uniformState.Set ("viewport_size", viewport);

			var _1stPass = CreateFullscreenQuad (filterName, filterNamespace, viewport,
			null
			,
			window =>
			{
				GL.Clear (ClearBufferMask.ColorBufferBit);
				GL.Disable (EnableCap.DepthTest);
				GL.Disable (EnableCap.Blend);
			//pass state
			},
			new FramebufferBindingSet (
			new DrawFramebufferBinding { VariableName = "Fragdata.result", Texture = result }),
			uniformState,
			new TextureBindingSet (new TextureBinding { VariableName = "source_texture", Texture = source }));
			return _1stPass;
		}

		/// <summary>
		/// create two passes in one compound pass. First pass has set "horizontal" uniform boolean to true,
		/// the second has it set to false.
		/// </summary>
		public static RenderPass CreateSeparableFilter
		(
			 string filterName,
			 string filterNamespace,
			 TextureBase source,
			 TextureBase interm,
			 TextureBase result)
		{
			var viewport = ValueProvider.Create (() => new Vector2 (result.Width, result.Height));
			var horizontal = false;
			var uniformState = new UniformState ();
			uniformState.Set ("viewport_size", viewport);
			uniformState.Set ("horizontal", ValueProvider.Create (() => horizontal));

			var _1stPass = CreateFullscreenQuad (filterName, filterNamespace, viewport,
			window =>
			{
				horizontal = false;
			}
			,
			window =>
			{
				GL.Clear (ClearBufferMask.ColorBufferBit);
				GL.Disable (EnableCap.DepthTest);
				GL.Disable (EnableCap.Blend);
			//pass state
			},
			new FramebufferBindingSet (
			new DrawFramebufferBinding { VariableName = "Fragdata.result", Texture = interm }),
			uniformState,
			new TextureBindingSet (new TextureBinding { VariableName = "source_texture", Texture = source }));

			var _2ndPass = CreateFullscreenQuad (filterName, filterNamespace, viewport,
			window =>
			{
				horizontal = true;
			}
			,
			window =>
			{
				GL.Clear (ClearBufferMask.ColorBufferBit);
				GL.Disable (EnableCap.DepthTest);
				GL.Disable (EnableCap.Blend);
			//pass state
			},
			new FramebufferBindingSet (
			new DrawFramebufferBinding { VariableName = "Fragdata.result", Texture = result }),
			uniformState,
			new TextureBindingSet (new TextureBinding { VariableName = "source_texture", Texture = interm }));

			return new CompoundRenderPass(_1stPass, _2ndPass);
		}

		/// <summary>
		/// given color and depth textures, render them.
		/// </summary>
		public static RenderPass CreateRenderTextureToBuffer
		(
			 TextureBase source,
			 TextureBase depth_source,
			 IValueProvider<Vector2> viewportSize,
			 Action<GameWindow> beforeState,
			 Action<GameWindow> beforeRender,
			 params StatePart[] stateParts
		)
		{
			//TODO: BUG see System21.State.cs: line 351. Order of states has also some influence
			var states = new StatePart[]{
				new TextureBindingSet
				(
				 new TextureBinding { VariableName = "source_texture", Texture = source },
				 new TextureBinding { VariableName = "depth_texture", Texture = depth_source }
				)
			};

			states = stateParts.Concat(states).ToArray();

			return CreateFullscreenQuad
			(
				 "rendertexture", "RenderPassFactory",
				 viewportSize,
				 beforeState,
				 beforeRender,
				 states);
		}
	}
}

