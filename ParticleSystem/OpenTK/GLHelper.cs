using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace OpenTK.Graphics.OpenGL
{
	public static class GLHelper
	{
		private static readonly ISet<string> m_Errors = new HashSet<string>();
		private static readonly List<string> m_CurrentErrors = new List<string>();
	
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

				var errorString = string.Format ("untreated error {0} {1}", err, methods);
				m_CurrentErrors.Add(errorString);
				if(m_Errors.Add (errorString))
				{
					Console.WriteLine (errorString);
				}
			}
		}
		
		[System.Diagnostics.Conditional("DEBUG")]
		public static void ResetError()
		{
			if(m_CurrentErrors.Count == 0)
			  m_Errors.Clear ();
			  	
			m_CurrentErrors.Clear ();
		}
	}
}

