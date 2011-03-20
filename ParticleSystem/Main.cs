using System;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Resources;

namespace opentk
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var win =
				new OpenTK.GameWindow (200, 200, 
				                      GraphicsMode.Default, 
				                      "", 
				                      OpenTK.GameWindowFlags.Default
				                      );
			
			(new System3.System3()).GetInstance(win);
			win.Run ();
			
			Console.WriteLine ("Hello World!");
		}
	}
}

