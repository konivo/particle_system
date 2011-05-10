using System;
using System.Linq;
using System.Diagnostics;

namespace OpenTK.Graphics.OpenGL
{
	public static class GLHelper
	{
		[System.Diagnostics.Conditional("DEBUG")]
		public static void PrintError (int depth = 5)
		{
			var err = GL.GetError ();
			if (err != ErrorCode.NoError) {
				StackTrace str = new StackTrace ();
				var methods = string.Join(Environment.NewLine,
				                          str.GetFrames()
				                          .Take(depth)
				                          .Select(x => string.Format("at method {0}, type {1}, line {2}", x.GetMethod(), x.GetMethod().DeclaringType, x.GetFileLineNumber())));
				Console.WriteLine ("untreated error {0} {1}", err, methods);
			}
		}
	}
}

