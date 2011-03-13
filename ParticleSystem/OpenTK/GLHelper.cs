using System;
using System.Diagnostics;

namespace OpenTK.Graphics.OpenGL
{
	public static class GLHelper
	{
		[System.Diagnostics.Conditional("DEBUG")]
		public static void PrintError ()
		{
			var err = GL.GetError ();
			if (err != ErrorCode.NoError) {
				StackTrace str = new StackTrace ();
				Console.WriteLine ("untreated error {0} at method {1}, line {2}", err, str.GetFrame (1).GetMethod (), str.GetFrame(1).GetFileLineNumber() );
			}
		}
	}
}

