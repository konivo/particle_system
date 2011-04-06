using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Structure;
using OpenTK.Graphics.OpenGL;

namespace opentk.GridRenderPass
{
	public partial class Grid: RenderPass
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
		private State m_State;

		public Grid (MatrixStack trans)
		{
			m_TransformationStack = trans;
		}

		unsafe void PrepareState ()
		{
			if (m_AttributeState != null)
			{
				PositionBuffer.Publish ();
				m_State.Activate ();
				return;
			}

			unsafe
			{
				PositionBuffer = new BufferObject<Vector3> (sizeof(Vector3)) {
					Name = "position_buffer",
					Usage = BufferUsageHint.DynamicDraw,
					Data = new []{
						new Vector3(100, 0, 0), new Vector3(-100, 0, 0),
						new Vector3(0, 100, 0), new Vector3(0, -100, 0),
						new Vector3(0, 0, 100), new Vector3(0, 0, -100)}
					 };
			}

			m_UniformState = new UniformState ()
				.Set ("modelview_transform", m_TransformationStack);
			
			m_AttributeState = new ArrayObject (
			new VertexAttribute { AttributeName = "pos", Buffer = PositionBuffer, Size = 3, Type = VertexAttribPointerType.Float });

			m_Program = new Program ("coordinate_grid_program", GetShaders().ToArray ());
			m_State = new State (null, m_AttributeState, m_Program, m_UniformState);
			
			var hnd = PositionBuffer.Handle;

			PrepareState ();
		}

		public override void Render ()
		{
			PrepareState ();
			GL.DrawArrays(BeginMode.Lines, 0, 6);
			GLHelper.PrintError();
		}
	}
}

