using System;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace opentk.System2
{
	public partial class System2
	{
		//private static ParticleSystem m_Particles = new ParticleSystem(100000);
		private ArrayObject m_ParticleRenderingState;
		//
		private Program m_ParticleRenderingProgram;
		//
		private UniformState m_UniformState;
		//
		private MatrixStack m_TransformationStack;
		//
		private MatrixStack m_Projection;
		//private static BufferObject<Vector4> VelocityBuffer;
		private BufferObject<Vector4> PositionBuffer;
		//
		private BufferObject<Vector4> ColorAndSizeBuffer;
		//
		private State m_SystemState;

		unsafe void PrepareState ()
		{
			if (m_ParticleRenderingState != null)
			{
				Simulate (DateTime.Now);

				PositionBuffer.Publish ();
				ColorAndSizeBuffer.Publish ();
				m_SystemState.Activate ();
				return;
			}

			unsafe
			{
				//VelocityBuffer = new BufferObject<Vector4> (sizeof(Vector4), size) { Name = "velocity_buffer", Usage = BufferUsageHint.DynamicDraw };
				PositionBuffer = new BufferObject<Vector4> (sizeof(Vector4), PARTICLES_COUNT) { Name = "position_buffer", Usage = BufferUsageHint.DynamicDraw };
				ColorAndSizeBuffer = new BufferObject<Vector4> (sizeof(Vector4), PARTICLES_COUNT) { Name = "colorandsize_buffer", Usage = BufferUsageHint.DynamicDraw };
			}

			m_Projection = new MatrixStack ().Push (Matrix4.CreateOrthographic (14, 14, -1, 1));

			m_TransformationStack = new MatrixStack (m_Projection).Push (Matrix4.Identity).Push (Matrix4.Identity);

			m_UniformState = new UniformState ().Set ("color", new Vector4 (0, 0, 1, 1)).Set ("red", 1.0f).Set ("green", 0.0f).Set ("blue", 1.0f).Set ("colors", new float[] { 0, 1, 0, 1 }).Set ("colors2", new Vector4[] { new Vector4 (1, 0.1f, 0.1f, 0), new Vector4 (1, 0, 0, 0), new Vector4 (1, 1, 0.1f, 0) }).Set ("modelview_transform", m_TransformationStack);
			
			var sprite = new[] { new Vector3 (-1, -1, 0), new Vector3 (-1, 1, 0), new Vector3 (1, 1, 0), new Vector3 (1, -1, 0) };
			
			var vdata_buffer = new BufferObject<Vector3> (sizeof(Vector3)) { Name = "vdata_buffer", Usage = BufferUsageHint.DynamicDraw, Data = sprite };
			
			
			m_ParticleRenderingState = new ArrayObject (new VertexAttribute { AttributeName = "vertex_pos", Buffer = vdata_buffer, Size = 3, Type = VertexAttribPointerType.Float }, new VertexAttribute { AttributeName = "sprite_pos", Buffer = PositionBuffer, Divisor = 1, Size = 4, Stride = 0, Type = VertexAttribPointerType.Float }, new VertexAttribute { AttributeName = "sprite_colorandsize", Buffer = ColorAndSizeBuffer, Divisor = 1, Size = 4, Type = VertexAttribPointerType.Float });
			m_ParticleRenderingProgram = new Program ("main_program", GetShaders().ToArray ());

			m_SystemState = new State (null, m_ParticleRenderingState, m_ParticleRenderingProgram, m_UniformState);
			
			var hnd = PositionBuffer.Handle;
			hnd = ColorAndSizeBuffer.Handle;

			m_DebugView = new opentk.QnodeDebug.QuadTreeDebug(10000, m_TransformationStack);
			InitializeSystem();
			PrepareState ();
		}

		private void SetCamera (GameWindow window)
		{
			float aspect = window.Height / (float)window.Width;
			float projw = 14;
			GL.Viewport (0, 0, window.Width, window.Height);
			
			if (m_Projection != null)
				m_Projection.Stack[0] = Matrix4.CreateOrthographic (projw, projw * aspect, -1, 1);
		}
	}
}

