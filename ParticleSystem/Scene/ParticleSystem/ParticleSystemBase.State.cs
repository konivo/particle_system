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
						var publishSize = PublishSize;
						m_PublishCounter += 1;

						var start = m_PublishCounter * publishSize % PARTICLES_COUNT;
						var end = start + publishSize;
						end = end > PARTICLES_COUNT ? PARTICLES_COUNT : end;

						m_PositionBuffer.PublishPart (start, end - start);
						m_DimensionBuffer.PublishPart (start, end - start);
						m_ColorBuffer.PublishPart (start, end - start);
						m_RotationBuffer.PublishPart (start, end - start);
						m_RotationLocalBuffer.PublishPart (start, end - start);
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
					new VertexAttribute { AttributeName = "sprite_rot_local", Buffer = m_ColorBuffer, Size = 16, Stride = 64, Type = VertexAttribPointerType.Float },
					new VertexAttribute { AttributeName = "sprite_rot", Buffer = m_ColorBuffer, Size = 16, Stride = 64, Type = VertexAttribPointerType.Float }
				);

			//
			PublishSize = 100000;
			TransformationStack = new MatrixStack();
			ProjectionStack = new MatrixStack();
			CameraMvp = new ModelViewProjectionParameters("", TransformationStack, ProjectionStack);

			//
			m_Manip = new OrbitManipulator (ProjectionStack);
			m_Grid = new Grid (CameraMvp);
			TransformationStack.Push (m_Manip.RT);

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

			Shading.GetPass(this).Render(window);

			SetCamera(window);

			if(ShowGrid)
				m_Grid.Render(window);

			window.SwapBuffers ();
		}
		
		#endregion
	}
}

