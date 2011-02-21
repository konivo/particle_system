using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace OpenTK
{
	public interface IValueProvider<T>: INotifyPropertyChanged
	{
		T Value {get;}
	}
}

