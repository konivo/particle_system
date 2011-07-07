using System;
using System.Linq;
using System.Collections.Generic;

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

		public static IEnumerable<string> GetTexts (string filter1, string filter2, System.Text.Encoding encoding)
		{
			var resources = from res in System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceNames ()
				where res.Contains (filter1) && res.Contains (filter2)
				select ResourcesHelper.GetText (res, encoding);

			return resources.ToArray();
		}

		private static IEnumerable<string> GetTexts (System.Text.Encoding encoding, params string[] filters)
		{
			var resources = from res in System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceNames ()
				where filters.All(f => res.Contains (f))
				select ResourcesHelper.GetText (res, encoding);

			return resources.ToArray();
		}
	}
}


