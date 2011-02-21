using System;
using OpenTK.Graphics.OpenGL;
namespace opentk
{
	public static class ResourcesHelper
	{
		public static string GetText(string resource, System.Text.Encoding encoding)
		{						
			using(var str = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
				using(var rdr = new System.IO.StreamReader(str, encoding))
					return rdr.ReadToEnd();
		}
	}
}

