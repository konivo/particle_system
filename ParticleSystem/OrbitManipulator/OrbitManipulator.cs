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
			
			var projview = ProjectionView.Value;
			projview.Invert ();
			
			var mousepos = 2 * Vector2.Divide (new Vector2 (window.Mouse.X, window.Mouse.Y), new Vector2 (window.Width, window.Height)) - new Vector2 (1, 1);
			mousepos.Y = -mousepos.Y;
//
			if (!m_Initialized)
			{
				m_OldMousePos = mousepos;
				return false;
			}
			
			var deltaPos = mousepos - m_OldMousePos;
			var deltaAngles = deltaPos;
			
			var rotationH = Quaternion.FromAxisAngle (Vector3.UnitX, deltaAngles.Y);
			var rotationV = Quaternion.FromAxisAngle (Vector3.UnitY, deltaAngles.X);
			
			var rot = Quaternion.Multiply (rotationH, rotationV);
			Position = Vector3.Transform (Position, rot);

			RTstack.ValueStack[0] = Matrix4.LookAt (Position, Target, new Vector3 (0, 0, 1));
			
			m_OldMousePos = mousepos;
			return true;
		}
		
		#endregion
	}
}

