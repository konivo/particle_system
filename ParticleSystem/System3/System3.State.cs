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
		private State m_SystemState;
		//
		private Grid m_Grid;

		private OrbitManipulator m_Manip;

		unsafe void PrepareState ()
		{
			if (m_ParticleRenderingState != null)
			{
				Simulate (DateTime.Now);

				PositionBuffer.Publish ();
				DimensionBuffer.Publish ();
				m_SystemState.Activate ();
				return;
			}

			unsafe
			{
				PositionBuffer = new BufferObject<Vector4> (sizeof(Vector4), PARTICLES_COUNT) { Name = "position_buffer", Usage = BufferUsageHint.DynamicDraw };
				DimensionBuffer = new BufferObject<Vector4> (sizeof(Vector4), PARTICLES_COUNT) { Name = "dimension_buffer", Usage = BufferUsageHint.DynamicDraw };
			}

			var ortho = Matrix4.CreateOrthographic (1,1, NEAR, FAR);

			m_Projection = new MatrixStack ().Push (ortho);
			m_TransformationStack = new MatrixStack ().Push(m_Projection);

			m_UniformState = new UniformState ()
			.Set ("color", new Vector4 (0, 0, 1, 1))
			.Set ("red", 1.0f)
			.Set ("green", 0.0f)
			.Set ("blue", 1.0f)
			.Set ("colors", new float[] { 0, 1, 0, 1 })
			.Set ("colors2", new Vector4[] { new Vector4 (1, 0.1f, 0.1f, 0), new Vector4 (1, 0, 0, 0), new Vector4 (1, 1, 0.1f, 0) });
			
			m_ParticleRenderingState = new ArrayObject (
			                                            new VertexAttribute { AttributeName = "sprite_pos", Buffer = PositionBuffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float },
			                                            new VertexAttribute { AttributeName = "sprite_dimensions", Buffer = DimensionBuffer, Size = 3, Stride = 16, Type = VertexAttribPointerType.Float });
			m_ParticleRenderingProgram = new Program ("main_program", GetShaders().ToArray ());

			m_SystemState = new State (null, m_ParticleRenderingState, m_ParticleRenderingProgram, m_UniformState);
			
			var hnd = PositionBuffer.Handle;
			hnd = DimensionBuffer.Handle;

			m_Manip = new OrbitManipulator(m_TransformationStack);
			m_Grid = new Grid(m_TransformationStack);

			m_TransformationStack.Push (m_Manip.RT);
			m_UniformState.Set("modelview_transform", m_Manip.RT);
			m_UniformState.Set("projection_transform", m_Projection);


			InitializeSystem();
			PrepareState ();
		}

		private void SetCamera (GameWindow window)
		{
			float aspect = window.Height / (float)window.Width;
			float projw = VIEWPORT_WIDTH;
			GL.Viewport (0, 0, window.Width, window.Height);
			
			if (m_Projection != null)
			{
				var ortho = Matrix4.CreateOrthographic (projw, projw * aspect, NEAR, FAR);
				m_Projection.ValueStack[0] = ortho;
			}

			if(m_Manip != null)
			{
				m_Manip.HandleInput(window);
			}
		}
	}
}

