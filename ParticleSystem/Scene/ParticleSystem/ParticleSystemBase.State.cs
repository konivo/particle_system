using System;
using System.ComponentModel.Composition;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.GridRenderPass;
using opentk.Manipulators;
using opentk.Scene;
using OpenTK.Graphics;
using opentk.ShadingSetup;

namespace opentk.Scene.ParticleSystem
{
	public enum PublishMethod
	{
		Incremental, AllAtOnce
	}

	/// <summary>
	///
	/// </summary>
	public abstract partial class ParticleSystemBase
	{
		/// <summary>
		///
		/// </summary>
		protected abstract void InitializeSystem();

		/// <summary>
		///
		/// </summary>
		/// <param name="simulationTime">
		/// A <see cref="DateTime"/>
		/// </param>
		protected abstract void Simulate (DateTime simulationTime);

		/// <summary>
		/// s
		/// </summary>
		protected abstract void PrepareStateCore();

		//
		private void PrepareState ()
		{
			if (m_Initialized)
			{
				if (PARTICLES_COUNT != m_PositionBuffer.Data.Length)
				{
					Position = m_PositionBuffer.Data = new Vector4[PARTICLES_COUNT];
					Dimension = m_DimensionBuffer.Data = new Vector4[PARTICLES_COUNT];
					Color = m_ColorBuffer.Data = new Vector4[PARTICLES_COUNT];
					Rotation = m_RotationBuffer.Data = new Matrix4[PARTICLES_COUNT];
					RotationLocal = m_RotationLocalBuffer.Data = new Matrix4[PARTICLES_COUNT];

					InitializeSystem ();
				}

				Simulate (DateTime.Now);

				switch (PublishMethod){
				case PublishMethod.AllAtOnce:

					m_PositionBuffer.Publish ();
					m_DimensionBuffer.Publish ();
					m_ColorBuffer.Publish ();
					m_RotationBuffer.Publish();
					m_RotationLocalBuffer.Publish();

					break;
				case PublishMethod.Incremental:
					{
					  m_PublishCounter %= PARTICLES_COUNT;

						var start = m_PublishCounter;
						var end = Math.Min(start + PublishSize, PARTICLES_COUNT);
						var cnt = end - start;

						m_PositionBuffer.PublishPart (start, cnt);
						m_DimensionBuffer.PublishPart (start, cnt);
						m_ColorBuffer.PublishPart (start, cnt);
						m_RotationBuffer.PublishPart (start, cnt);
						m_RotationLocalBuffer.PublishPart (start, cnt);

						m_PublishCounter = end;
					}
					break;
				default:
					break;
				}

				return;
			}
			
			unsafe
			{
				m_PositionBuffer = new BufferObject<Vector4> (sizeof(Vector4), 0) { Name = "position_buffer", Usage = BufferUsageHint.DynamicDraw };
				m_DimensionBuffer = new BufferObject<Vector4> (sizeof(Vector4), 0) { Name = "dimension_buffer", Usage = BufferUsageHint.DynamicDraw };
				m_ColorBuffer = new BufferObject<Vector4> (sizeof(Vector4), 0) { Name = "color_buffer", Usage = BufferUsageHint.DynamicDraw };
				m_RotationBuffer = new BufferObject<Matrix4> (sizeof(Matrix4), 0) { Name = "rotation_buffer", Usage = BufferUsageHint.DynamicDraw };
				m_RotationLocalBuffer = new BufferObject<Matrix4> (sizeof(Matrix4), 0) { Name = "rotation_local_buffer", Usage = BufferUsageHint.DynamicDraw };
			}

			ParticleStateArrayObject =
				new ArrayObject (
					new VertexAttribute { AttributeName = "sprite_pos", Buffer = m_PositionBuffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float },
					new VertexAttribute { AttributeName = "sprite_color", Buffer = m_ColorBuffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float },
					new VertexAttribute { AttributeName = "sprite_dimensions", Buffer = m_DimensionBuffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float },
					new VertexAttribute { AttributeName = "sprite_rotation_local", Buffer = m_RotationLocalBuffer, Size = 16, Stride = 64, Type = VertexAttribPointerType.Float },
					new VertexAttribute { AttributeName = "sprite_rotation", Buffer = m_RotationBuffer, Size = 16, Stride = 64, Type = VertexAttribPointerType.Float }
				);

			//
			PublishSize = 100000;
			TransformationStack = new MatrixStack();
			ProjectionStack = new MatrixStack();
			CameraMvp = new ModelViewProjectionParameters("", TransformationStack, ProjectionStack);

			//
			ModelScaleFactor = 1;
			m_Manip = new OrbitManipulator (ProjectionStack);
			m_Grid = new Grid (CameraMvp);
			TransformationStack.Push (m_Manip.RT);
			TransformationStack.Push( Matrix4.Scale(ModelScaleFactor));

			//
			Uniforms = new UniformState("");
			var particle_scale_factor = ValueProvider.Create (() => this.ParticleScaleFactor);
			Uniforms.Set ("particle_scale_factor", particle_scale_factor);
			Uniforms.SetMvp ("", CameraMvp);

			//
			Shading = GlobalContext.Container.GetExportedValues<IShadingSetup>().FirstOrDefault();
			PrepareStateCore();

			m_Initialized = true;
			PrepareState ();
		}

		public void SetViewport(int x, int y, int width, int height)
		{
			Viewport = new Vector2(width, height);
			GL.Viewport (x, y, width, height);
		}

		public void SetViewport(GameWindow window)
		{
			SetViewport(0, 0, window.Width, window.Height);
		}

		private void UpdateModelTransformation()
		{
			ModelScaleFactor = MathHelper2.Clamp(ModelScaleFactor, 0.1f, 100);

			if (TransformationStack != null)
			{
				TransformationStack.ValueStack[1] = Matrix4.Scale(ModelScaleFactor);
			}
		}

		public void SetCamera (GameWindow window)
		{
			float aspect = window.Height / (float)window.Width;
			float projw = VIEWPORT_WIDTH;

			if (ProjectionStack != null)
			{
				switch (ProjectionType)
				{
				case ProjectionType.Frustum:
					ProjectionStack.ValueStack[0] = Matrix4.CreatePerspectiveFieldOfView ((float)Fov, 1 / aspect, (float)NEAR, (float)FAR);
					break;
				case ProjectionType.Ortho:
					ProjectionStack.ValueStack[0] = Matrix4.CreateOrthographic (projw, projw * aspect, (float)NEAR, (float)FAR);
					;
					break;
				default:
					break;
				}
			}
		}

		public override void Dispose ()
		{
			m_PositionBuffer.Dispose ();
			m_DimensionBuffer.Dispose ();
			m_ColorBuffer.Dispose ();
		}

		#region implemented abstract members of opentk._ParticleSystem
		protected override void HandleFrame (GameWindow window)
		{
			GL.ClearColor (Color4.Black);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			PrepareState ();

			if (m_Manip != null)
				m_Manip.HandleInput (window);

			UpdateModelTransformation();
			SetCamera(window);

			//
			Shading.GetPass(this).Render(window);
			if(ShowGrid)
				m_Grid.Render(window);

			window.SwapBuffers ();
		}
		
		#endregion
	}
}

