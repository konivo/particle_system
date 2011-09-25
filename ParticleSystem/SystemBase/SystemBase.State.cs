using System;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.GridRenderPass;
using opentk.Manipulators;
using opentk.Scene;

namespace opentk.SystemBase
{
	public abstract partial class SystemBase
	{
		//
		//private ArrayObject m_ParticleRenderingState;
		//
		//private UniformState m_UniformState;
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
		//
		private int m_PublishCounter;
		//
		private RenderPass[] m_Passes;

		public Vector4[] Position
		{
			get; private set;
		}

		public Vector4[] Dimension
		{
			get; private set;
		}

		public Vector4[] Color
		{
			get; private set;
		}

		public ArrayObject ParticleRenderingState
		{
			get; private set;
		}

		public int PublishSize
		{
			get; private set;
		}

		public Vector2 Viewport
		{
			get; private set;
		}

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
		///
		/// </summary>
		/// <returns>
		/// A <see cref="RenderPass[]"/>
		/// </returns>
		protected abstract RenderPass[] GetPasses();

		/// <summary>
		/// s
		/// </summary>
		protected abstract void PrepareStateCore();

		//
		private void PrepareState ()
		{
			if (m_Passes != null)
			{
				if (PARTICLES_COUNT != PositionBuffer.Data.Length)
				{
					Position = PositionBuffer.Data = new Vector4[PARTICLES_COUNT];
					Dimension = DimensionBuffer.Data = new Vector4[PARTICLES_COUNT];
					Color = ColorBuffer.Data = new Vector4[PARTICLES_COUNT];

					InitializeSystem ();
				}

				Simulate (DateTime.Now);
				
				if (false)
				{
					var publishSize = PublishSize;
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

				m_Passes = GetPasses();
				return;
			}

			PublishSize = 100000;
			PrepareStateCore();
			
			unsafe
			{
				PositionBuffer = new BufferObject<Vector4> (sizeof(Vector4), 0) { Name = "position_buffer", Usage = BufferUsageHint.DynamicDraw };
				DimensionBuffer = new BufferObject<Vector4> (sizeof(Vector4), 0) { Name = "dimension_buffer", Usage = BufferUsageHint.DynamicDraw };
				ColorBuffer = new BufferObject<Vector4> (sizeof(Vector4), 0) { Name = "color_buffer", Usage = BufferUsageHint.DynamicDraw };
			}

			ParticleRenderingState =
				new ArrayObject (
					new VertexAttribute { AttributeName = "sprite_pos", Buffer = PositionBuffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float },
					new VertexAttribute { AttributeName = "sprite_color", Buffer = ColorBuffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float },
					new VertexAttribute { AttributeName = "sprite_dimensions", Buffer = DimensionBuffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float }
				);

			m_Passes = GetPasses();
			PrepareState ();
		}

		private void SetViewport(int x, int y, int width, int height)
		{
			Viewport = new Vector2(width, height);
			GL.Viewport (x, y, width, height);
		}

		private void SetCamera (GameWindow window)
		{
			float aspect = window.Height / (float)window.Width;
			float projw = VIEWPORT_WIDTH;

			SetViewport(0, 0, window.Width, window.Height);
			
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
		}

		public override void Dispose ()
		{
			PositionBuffer.Dispose ();
			DimensionBuffer.Dispose ();
			ColorBuffer.Dispose ();
		}
	}
}

