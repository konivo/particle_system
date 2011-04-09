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
	public class MatrixStack : IValueProvider<Matrix4>
	{
		/// <summary>
		///
		/// </summary>
		private class MMStack : IList<Matrix4>, ICollection<Matrix4>, IEnumerable<Matrix4>
		{
			private readonly MatrixStack m_Parent;

			public MMStack (MatrixStack parent)
			{
				m_Parent = parent;
			}

			#region IList[Matrix4] implementation
			public int IndexOf (Matrix4 item)
			{
				throw new NotImplementedException ();
			}

			public void Insert (int index, Matrix4 item)
			{
				m_Parent.Stack.Insert (index, new MatrixStack (item));
			}

			public void RemoveAt (int index)
			{
				m_Parent.Stack.RemoveAt (index);
			}

			public Matrix4 this[int index]
			{
				get { return m_Parent.Stack[index].Value; }
				set { m_Parent.Stack[index] = new MatrixStack (value); }
			}
			#endregion

			#region ICollection[Matrix4] implementation
			public void Add (Matrix4 item)
			{
				m_Parent.Stack.Add(new MatrixStack(item));
			}

			public void Clear ()
			{
				throw new NotImplementedException ();
			}

			public bool Contains (Matrix4 item)
			{
				throw new NotImplementedException ();
			}

			public void CopyTo (Matrix4[] array, int arrayIndex)
			{
				throw new NotImplementedException ();
			}

			public bool Remove (Matrix4 item)
			{
				throw new NotImplementedException ();
			}

			public int Count
			{
				get
				{
					throw new NotImplementedException ();
				}
			}

			public bool IsReadOnly
			{
				get
				{
					throw new NotImplementedException ();
				}
			}
			#endregion

			#region IEnumerable[Matrix4] implementation
			public IEnumerator<Matrix4> GetEnumerator ()
			{
				throw new NotImplementedException ();
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
			{
				throw new NotImplementedException ();
			}
			#endregion
		}

		/// <summary>
		///
		/// </summary>
		private class MStack : System.Collections.ObjectModel.ObservableCollection<IValueProvider<Matrix4>>
		{
			private readonly MatrixStack m_Parent;

			public MStack (MatrixStack parent)
			{
				m_Parent = parent;
			}

			protected override void SetItem (int index, IValueProvider<Matrix4> item)
			{
				this[index].PropertyChanged -= m_Parent.ChildValueChangedHandler;
				base.SetItem (index, item);
				item.PropertyChanged += m_Parent.ChildValueChangedHandler;
				m_Parent.RaiseChanged ();
			}

			protected override void ClearItems ()
			{
				foreach (IValueProvider<Matrix4> item in this)
				{
					item.PropertyChanged -= m_Parent.ChildValueChangedHandler;
				}
				
				base.ClearItems ();
				m_Parent.RaiseChanged ();
			}

			protected override void InsertItem (int index, IValueProvider<Matrix4> item)
			{
				base.InsertItem (index, item);
				item.PropertyChanged += m_Parent.ChildValueChangedHandler;
				m_Parent.RaiseChanged ();
			}

			protected override void RemoveItem (int index)
			{
				this[index].PropertyChanged -= m_Parent.ChildValueChangedHandler;
				base.RemoveItem (index);
				m_Parent.RaiseChanged ();
			}
		}

		private MatrixStack m_BaseStack;
		private Matrix4 m_Value;
		private Matrix4 m_LocalStack;
		private volatile bool m_Refreshed;

		public MatrixStack (MatrixStack basestack) : this()
		{
			m_BaseStack = basestack;
			m_BaseStack.PropertyChanged += ChildValueChangedHandler;
		}

		public MatrixStack (Matrix4 mat) : this()
		{
			m_LocalStack = mat;
			m_Value = mat;
		}

		public MatrixStack ()
		{
			m_LocalStack = Matrix4.Identity;
			Stack = new MStack (this);
			ValueStack = new MMStack (this);
		}

		public IList<Matrix4> ValueStack
		{
			get;
			private set;
		}

		public IList<IValueProvider<Matrix4>> Stack
		{
			get;
			private set;
		}

		public MatrixStack Push (Matrix4 mat)
		{
			ValueStack.Add (mat);
			return this;
		}

		public MatrixStack Push (IValueProvider<Matrix4> another)
		{
			Stack.Add (another);
			return this;
		}

		public MatrixStack Pop ()
		{
			Stack.RemoveAt (Stack.Count - 1);
			return this;
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
					var bmat = m_BaseStack == null ? m_LocalStack : m_BaseStack.Value;
					bmat = Stack.Aggregate (bmat, (res, x) => x.Value * res);
					m_Value = bmat;

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
