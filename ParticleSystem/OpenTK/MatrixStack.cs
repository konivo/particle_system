using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenTK
{
	public class MatrixStack: IValueProvider<Matrix4>
	{
		private class MStack: System.Collections.ObjectModel.ObservableCollection<Matrix4>
		{
			private readonly MatrixStack m_Parent;
		
			public MStack (MatrixStack parent)
			{
				m_Parent = parent;
			}
			
			protected override void SetItem (int index, Matrix4 item)
			{
				base.SetItem (index, item);
				m_Parent.RaiseChanged();
			}
			
			protected override void ClearItems ()
			{
				base.ClearItems ();
				m_Parent.RaiseChanged ();
			}
			
			protected override void InsertItem (int index, Matrix4 item)
			{
				base.InsertItem (index, item);
				m_Parent.RaiseChanged ();
			} 
			
			protected override void RemoveItem (int index)
			{
				base.RemoveItem (index);
				m_Parent.RaiseChanged ();
			}
		}
	
		private MatrixStack m_BaseStack;
	
		public MatrixStack (MatrixStack basestack)
		:this()
		{
			m_BaseStack = basestack;
			
			//todo: remove strong reference from base to this instance if possible			
			m_BaseStack.PropertyChanged += (sender, args) => RaiseChanged();
		}
		
		public MatrixStack ()
		{
			Stack = new MStack(this);
		}
		
		public IList<Matrix4> Stack{get; private set;}
	
		public MatrixStack Push(Matrix4 mat)
		{
			Stack.Add(mat);
			return this;
		}
		
		public MatrixStack Pop ()
		{
			Stack.RemoveAt(Stack.Count - 1);
			return this;
		}
				
		private void RaiseChanged()
		{
			var hnd = PropertyChanged;
			if(hnd != null)
			{
				hnd(this, new System.ComponentModel.PropertyChangedEventArgs("Value"));
			}
		}
	
		#region IValueProvider[Matrix4] implementation
		public Matrix4 Value {
			get {
				var bmat = m_BaseStack == null? Matrix4.Identity: m_BaseStack.Value;
				bmat = Stack.Aggregate(bmat, (res, x) => res * x);
				
				return bmat;
			}
		}
		#endregion

		#region INotifyPropertyChanged implementation
		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
		#endregion	
	}
}