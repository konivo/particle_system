using System;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.GridRenderPass;
using opentk.Manipulators;

namespace opentk.System3
{
	public partial class System3
	{
		//
		private ArrayObject m_ParticleRenderingState;
		//
		private Program m_ParticleRenderingProgram;
		//
		private UniformState m_UniformState;
		//
		private MatrixStack m_TransformationStack;
		//
		private MatrixStack m_Projection;
		//
		private BufferObject<Vector4> PositionBuffer;
		//
		private BufferObject<Vector4> DimensionBuffer;
		//
		private BufferObject<Vector4> ColorBuffer;

		private TextureBase Texture;
		//
		private State m_SystemState;
		//
		private Grid m_Grid;

		private OrbitManipulator m_Manip;

		private int m_PublishCounter;

		private int m_PublishSize;

		unsafe void PrepareState ()
		{
			if (m_ParticleRenderingState != null)
			{
				if (PARTICLES_COUNT != PositionBuffer.Data.Length)
				{
					PositionBuffer.Data = new Vector4[PARTICLES_COUNT];
					DimensionBuffer.Data = new Vector4[PARTICLES_COUNT];
					ColorBuffer.Data = new Vector4[PARTICLES_COUNT];
					
					InitializeSystem ();
				}
				
				Simulate (DateTime.Now);
				
				if (MapMode)
				{
					var publishSize = m_PublishSize;
					m_PublishCounter += 1;
					
					var start = m_PublishCounter * publishSize % PARTICLES_COUNT;
					var end = start + publishSize;
					end = end > PARTICLES_COUNT ? PARTICLES_COUNT : end;
					
					PositionBuffer.PublishPart (start, end - start);
					DimensionBuffer.PublishPart (start, end - start);
					ColorBuffer.PublishPart (start, end - start);
				}

				else
				{
					PositionBuffer.Publish ();
					DimensionBuffer.Publish ();
					ColorBuffer.Publish ();
				}
				
				m_SystemState.Activate ();
				return;
			}
			
			unsafe
			{
				PositionBuffer = new BufferObject<Vector4> (sizeof(Vector4), PARTICLES_COUNT) { Name = "position_buffer", Usage = BufferUsageHint.DynamicDraw };
				DimensionBuffer = new BufferObject<Vector4> (sizeof(Vector4), PARTICLES_COUNT) { Name = "dimension_buffer", Usage = BufferUsageHint.DynamicDraw };
				ColorBuffer = new BufferObject<Vector4> (sizeof(Vector4), PARTICLES_COUNT) { Name = "color_buffer", Usage = BufferUsageHint.DynamicDraw };
				
				Texture = new DataTexture<Vector3> {
					Name = "custom_texture",
					InternalFormat = PixelInternalFormat.Rgb,
					Data2D = TestTexture(30, 30),
					Params = new TextureBase.Parameters
					{
						GenerateMipmap = true,
						MinFilter = TextureMinFilter.LinearMipmapLinear,
						MagFilter = TextureMagFilter.Linear,
					}};
			}
			
			m_PublishSize = 100000;
			
			var ortho = Matrix4.CreateOrthographic (1, 1, (float)NEAR, (float)FAR);
			
			m_Projection = new MatrixStack ().Push (ortho);
			m_TransformationStack = new MatrixStack ().Push (m_Projection);
			
			m_UniformState = new UniformState ().Set ("color", new Vector4 (0, 0, 1, 1)).Set ("particle_scale_factor", ValueProvider.Create (() => this.ParticleScaleFactor)).Set ("particle_shape", ValueProvider.Create (() => (int)this.ParticleShape)).Set ("particle_brightness", ValueProvider.Create (() => this.ParticleBrightness)).Set ("smooth_shape_sharpness", ValueProvider.Create (() => this.SmoothShapeSharpness)).Set ("blue", 1.0f).Set ("colors", new float[] { 0, 1, 0, 1 }).Set ("colors2", new Vector4[] { new Vector4 (1, 0.1f, 0.1f, 0), new Vector4 (1, 0, 0, 0), new Vector4 (1, 1, 0.1f, 0) });
			
			m_ParticleRenderingState = new ArrayObject (
				new VertexAttribute { AttributeName = "sprite_pos", Buffer = PositionBuffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float },
				new VertexAttribute { AttributeName = "sprite_color", Buffer = ColorBuffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float },
				new VertexAttribute { AttributeName = "sprite_dimensions", Buffer = DimensionBuffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float });

			m_ParticleRenderingProgram = new Program ("main_program", GetShaders ().ToArray ());
			
			m_SystemState = new State (null, m_ParticleRenderingState, m_ParticleRenderingProgram, m_UniformState,
				new TextureBindingSet( new TextureBinding { VariableName = "custom_texture", Texture = Texture }));
			
			m_Manip = new OrbitManipulator (m_TransformationStack);
			m_Grid = new Grid (m_TransformationStack);
			
			m_TransformationStack.Push (m_Manip.RT);
			m_UniformState.Set ("modelview_transform", m_Manip.RT);
			m_UniformState.Set ("projection_transform", m_Projection);
			
			InitializeSystem ();
			PrepareState ();
		}

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

		private void SetCamera (GameWindow window)
		{
			float aspect = window.Height / (float)window.Width;
			float projw = VIEWPORT_WIDTH;
			GL.Viewport (0, 0, window.Width, window.Height);
			
			if (m_Projection != null)
			{
				switch (Projection)
				{
				case ProjectionType.Frustum:
					m_Projection.ValueStack[0] = Matrix4.CreatePerspectiveFieldOfView ((float)Fov, 1 / aspect, (float)NEAR, (float)FAR);
					break;
				case ProjectionType.Ortho:
					m_Projection.ValueStack[0] = Matrix4.CreateOrthographic (projw, projw * aspect, (float)NEAR, (float)FAR);
					;
					break;
				default:
					break;
				}
			}
			
			if (m_Manip != null)
			{
				m_Manip.HandleInput (window);
			}
		}

		public override void Dispose ()
		{
			m_SystemState.Dispose ();
			PositionBuffer.Dispose ();
			DimensionBuffer.Dispose ();
			ColorBuffer.Dispose ();
			Texture.Dispose();
		}
	}
}

