using System;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel;
using System.ComponentModel.Composition;
using OpenTK.Graphics.OpenGL;
using opentk.PropertyGridCustom;
using opentk.Scene;
using opentk.Scene.ParticleSystem;

namespace opentk.ShadingSetup
{
	public enum ParticleShapeType
	{
		SmoothDot = 0x1,
		TextureSmoothDot = 0x3
	}

	/// <summary>
	///
	/// </summary>
	public class SmoothSetup: IShadingSetup
	{
		private RenderPass m_Pass;
		private TextureBase m_Texture;
		private UniformState m_Uniforms;

		public ParticleShapeType ParticleShape
		{
			get;
			set;
		}

		public float SmoothShapeSharpness
		{
			get;
			set;
		}


		public float ParticleBrightness
		{
			get;
			set;
		}

		public int TextureResolution
		{
			get;
			set;
		}

		public SmoothSetup ()
		{
			ParticleShape = ParticleShapeType.SmoothDot;
			ParticleBrightness = 144;
			SmoothShapeSharpness = 2;
		}

		//
		private Vector3[,] TestTexture (int w, int h)
		{
			var result = new Vector3[h, w];
			var center = new Vector2(w/2, h/2);

			for (int i = 0; i < h; i++)
			{
				for (int j = 0; j < w; j++)
				{
					var position = new Vector2(j, i) - center;
					position = Vector2.Divide(position, center);
					var len = position.Length;

					if(len > 1)
						result[i,j] = new Vector3(0, 0, 0);
					else result[i, j] = new Vector3(len, 0, 0);
				}
			}

			return result;
		}

		//
		private void PrepareTexture()
		{
			if(m_Texture == null)
			{
				m_Texture = new DataTexture<Vector3> {
					Name = "custom_texture",
					InternalFormat = PixelInternalFormat.Rgb,
					Data2D = TestTexture(2, 2),
					Params = new TextureBase.Parameters
					{
						GenerateMipmap = true,
						MinFilter = TextureMinFilter.LinearMipmapLinear,
						MagFilter = TextureMagFilter.Linear,
					}};
					
				//m_Texture = opentk.Resources.Textures.FromVectors("", 100, 1, MathHelper2.RandomVectorSet(100, new Vector2d(1)));
		  }

		  if( m_Texture.Width != TextureResolution)
		  {
				((DataTexture<Vector3>)m_Texture).Data2D = TestTexture(TextureResolution, TextureResolution);
		  }
		}

		#region IRenderSetup implementation
		RenderPass IShadingSetup.GetPass (ParticleSystemBase p)
		{
			PrepareTexture();

			if(m_Pass != null)
				return m_Pass;

			m_Uniforms = new UniformState(p.Uniforms);
			m_Uniforms.Set ("particle_shape", ValueProvider.Create (() => (int)this.ParticleShape));
			m_Uniforms.Set ("particle_brightness", ValueProvider.Create (() => this.ParticleBrightness));
			m_Uniforms.Set ("smooth_shape_sharpness", ValueProvider.Create (() => this.SmoothShapeSharpness));

			//
			m_Pass = new SeparateProgramPass
			(
				 "light",
				 "SmoothShading",
				 null,
				 null,

				 //pass code
				 (window) =>
				 {
					GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
					GL.Disable (EnableCap.DepthTest);
					GL.Enable (EnableCap.Blend);
					GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
					GL.BlendEquation (BlendEquationMode.FuncAdd);

					//TODO: viewport size actually doesn't propagate to shader, because uniform state has been already activated
					p.SetViewport (window);
					GL.DrawArrays (BeginMode.Points, 0, p.PARTICLES_COUNT);
				 },

				 //pass state
				 FramebufferBindingSet.Default,
				 p.ParticleStateArrayObject,
				 m_Uniforms,
				 new TextureBindingSet(
				   new TextureBinding { VariableName = "custom_texture", Texture = m_Texture }
				 )
			);

			return m_Pass;
		}

		string IShadingSetup.Name
		{
			get
			{
				return "SmoothSetup";
			}
		}
		#endregion
	}
}

