using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Structure;
using OpenTK.Graphics.OpenGL;

namespace opentk.QnodeDebug
{
	using QNode = QuadTree<int>;

	public partial class QuadTreeDebug: RenderPass
	{
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
		//
		private BufferObject<Vector3> DimensionBuffer;
		//
		private State m_State;

		private readonly int m_MaxSize = 1000;

		private int m_ActSize = 0;

		public QNode Tree {
			get;
			set;
		}

		public QuadTreeDebug (int maxSize, MatrixStack trans)
		{
			m_MaxSize = maxSize;
			m_TransformationStack = trans;
		}

		private void FillBuffers()
		{
			if(Tree == null)
				return;
				
			var q = new List<QNode>();
			q.Add(Tree);

			for (m_ActSize = 0; m_ActSize < m_MaxSize; m_ActSize++)
			{
				if(q.Count == 0)
					break;

				var top = q[0];
				q.RemoveAt(0);

				PositionBuffer.Data[m_ActSize] = new Vector3((top.Max + top.Min) * 0.5f);
				DimensionBuffer.Data[m_ActSize] = new Vector3((top.Max - top.Min));

				q.AddRange(top.Children);
			}
		}

		unsafe void PrepareState ()
		{
			if (m_AttributeState != null)
			{
				FillBuffers();

				PositionBuffer.Publish ();
				DimensionBuffer.Publish ();
				m_State.Activate ();
				return;
			}

			unsafe
			{
				PositionBuffer = new BufferObject<Vector3> (sizeof(Vector3), m_MaxSize) { Name = "position_buffer", Usage = BufferUsageHint.DynamicDraw };
				DimensionBuffer = new BufferObject<Vector3> (sizeof(Vector3), m_MaxSize) { Name = "dimension_buffer", Usage = BufferUsageHint.DynamicDraw };
			}

			m_UniformState = new UniformState ()
				.Set ("modelview_transform", m_TransformationStack);
			
			m_AttributeState = new ArrayObject (
			new VertexAttribute { AttributeName = "cube_pos", Buffer = PositionBuffer, Size = 3, Type = VertexAttribPointerType.Float },
			new VertexAttribute { AttributeName = "cube_dimensions", Buffer = DimensionBuffer, Size = 3, Type = VertexAttribPointerType.Float });

			m_Program = new Program ("debug_qnode_program", GetShaders().ToArray ());
			m_State = new State (null, m_AttributeState, m_Program, m_UniformState);
			
			var hnd = PositionBuffer.Handle;
			hnd = DimensionBuffer.Handle;

			PrepareState ();
		}

		public override void Render (GameWindow window)
		{
			PrepareState ();
			GL.DrawArrays(BeginMode.Points, 0, m_ActSize);
			GLHelper.PrintError();
		}
	}
}

