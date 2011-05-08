using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Extensions;
using OpenTK.Structure;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace opentk.Manipulators
{
	public partial class OrbitManipulator : Manipulator
	{
		public Vector3 Position = new Vector3 (100, 100, 100);
		public Vector3 Target;
		public Vector3 UpDirection = new Vector3(0, 0, 1);

		private Vector2 m_OldMousePos;
		private bool m_Initialized = false;

		public OrbitManipulator (IValueProvider<Matrix4> projview) : base(projview)
		{
			RTstack.Push (Matrix4.LookAt (Position, Target, new Vector3 (0, 0, 1)));
		}

		#region implemented abstract members of opentk.Manipulator
		public override void Render ()
		{
		}


		public override bool HandleInput (GameWindow window)
		{
			m_Initialized = window.Mouse[MouseButton.Left];
			
			var mousepos = 2 * new Vector2 (window.Mouse.X, window.Mouse.Y);
			mousepos.Y = -mousepos.Y;

			if (!m_Initialized)
			{
				m_OldMousePos = mousepos;
				return false;
			}
			
			var deltaPos = mousepos - m_OldMousePos;
			var deltaAngles = deltaPos / 400;

			var temp = Position;
			temp.Normalize();
			temp = Vector3.Cross(temp, Vector3.UnitZ);
			temp.Normalize();

			var rotationH = Quaternion.FromAxisAngle (temp, deltaAngles.Y);
			var rotationV = Quaternion.FromAxisAngle (Vector3.UnitZ, deltaAngles.X);
			Position = Vector3.Transform(Position, Matrix4.Rotate(rotationH) * Matrix4.Rotate(rotationV));

			RTstack.ValueStack[0] = Matrix4.LookAt (Position, Target, UpDirection);
			
			m_OldMousePos = mousepos;
			return true;
		}
		
		#endregion
	}
}

