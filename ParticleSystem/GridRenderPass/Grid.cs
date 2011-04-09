using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Structure;
using OpenTK.Graphics.OpenGL;

namespace opentk.GridRenderPass
{
	public partial class Grid : RenderPass
	{
		private int m_GridDensity = 20;

		private int m_GridDiameter = 200;

		private int m_Count;
		//
		private ArrayObject m_AttributeState;
		//
		private Program m_Program;
		//
		private UniformState m_UniformState;
		//
		private MatrixStack m_TransformationStack;
		//
		private BufferObject<Vector3> PositionBuffer;
		private BufferObject<float> ParameterBuffer;
		//
		private State m_State;

		public Grid (MatrixStack trans)
		{
			m_TransformationStack = trans;
		}

		private void UpdateGrid ()
		{
			var vbuf = PositionBuffer.Data;
			var pbuf = ParameterBuffer.Data;

			Vector3 diagpoint = new Vector3 (-m_GridDiameter / 2.0f, -m_GridDiameter / 2.0f, 0);
			Vector3 min = diagpoint;
			Vector3 max = -diagpoint;
			Vector3 d = new Vector3 (m_GridDiameter / (float)(m_Count - 1), m_GridDiameter / (float)(m_Count - 1), 0);

			//xy grid lines
			for (int index = 0; index < m_Count; index++)
			{
				int i = 4 * index;
				vbuf[i] = new Vector3 (diagpoint.X, min.Y, 0);
				vbuf[i + 1] = new Vector3 (diagpoint.X, max.Y, 0);

				vbuf[i + 2] = new Vector3 (min.X, diagpoint.Y, 0);
				vbuf[i + 3] = new Vector3 (max.X, diagpoint.Y, 0);

				pbuf[i] = 0;
				pbuf[i + 1] = 1;

				pbuf[i + 2] = 0;
				pbuf[i + 3] = 1;

				diagpoint = diagpoint + d;
			}

			//z axis
			vbuf[4 * m_Count] = new Vector3 (0, 0, -100);
			vbuf[4 * m_Count + 1] = new Vector3 (0, 0, 100);
			pbuf[4 * m_Count] = 0;
			pbuf[4 * m_Count + 1] = 1;
		}

		unsafe void PrepareState ()
		{
			if (m_AttributeState != null)
			{
				UpdateGrid ();
				PositionBuffer.Publish ();
				ParameterBuffer.Publish ();
				m_State.Activate ();
				return;
			}

			m_Count = m_GridDiameter / m_GridDensity + 1;

			unsafe
			{
				PositionBuffer = new BufferObject<Vector3> (sizeof(Vector3), 4 * m_Count + 2) { Name = "position_buffer", Usage = BufferUsageHint.DynamicDraw };
				ParameterBuffer = new BufferObject<float> (sizeof(float), 4 * m_Count + 2) { Name = "parameter_buffer", Usage = BufferUsageHint.DynamicDraw };
			}

			m_UniformState = new UniformState ().Set ("modelview_transform", m_TransformationStack);
			m_AttributeState = new ArrayObject (new VertexAttribute { AttributeName = "pos", Buffer = PositionBuffer, Size = 3, Type = VertexAttribPointerType.Float }, new VertexAttribute { AttributeName = "param", Buffer = ParameterBuffer, Size = 1, Type = VertexAttribPointerType.Float });

			m_Program = new Program ("coordinate_grid_program", GetShaders ().ToArray ());
			m_State = new State (null, m_AttributeState, m_Program, m_UniformState);
			
			var hnd = PositionBuffer.Handle;
			var hnd1 = ParameterBuffer.Handle;
			
			PrepareState ();
		}

		public override void Render ()
		{
			PrepareState ();
			GL.DrawArrays (BeginMode.Lines, 0, m_Count * 4 + 2);
			GLHelper.PrintError ();
		}
	}
}

