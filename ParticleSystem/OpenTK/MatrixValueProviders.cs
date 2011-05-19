using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenTK
{
	/// <summary>
	/// defines stack of matrices. Matrices are ordered from right to left (first is the rightmost)
	/// </summary>
	public class MatrixInversion : IValueProvider<Matrix4>
	{
		private IValueProvider<Matrix4> m_BaseStack;
		private Matrix4 m_Value;
		private volatile bool m_Refreshed;

		public MatrixInversion (IValueProvider<Matrix4> basestack)
		{
			m_BaseStack = basestack;
			m_BaseStack.PropertyChanged += ChildValueChangedHandler;
		}

		private void ChildValueChangedHandler (object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			RaiseChanged ();
		}

		private void RaiseChanged ()
		{
			m_Refreshed = false;
			
			var hnd = PropertyChanged;
			if (hnd != null)
			{
				hnd (this, new System.ComponentModel.PropertyChangedEventArgs ("Value"));
			}
		}

		#region IValueProvider[Matrix4] implementation
		public Matrix4 Value
		{
			get
			{
				if (!m_Refreshed)
				{
					m_Value = m_BaseStack.Value;
					m_Value.Invert();

					m_Refreshed = true;
				}
				
				return m_Value;
			}
		}
		#endregion

		#region INotifyPropertyChanged implementation
		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
		#endregion
	}
}
