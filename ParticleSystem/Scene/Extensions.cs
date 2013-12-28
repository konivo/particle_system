using System;
using System.Linq;
using OpenTK;

namespace opentk.Scene
{
	public static class Extensions
	{
		/// <summary>
		/// Sets the mvp.
		/// </summary>
		/// <param name="state">State.</param>
		/// <param name="prefix">Prefix.</param>
		/// <param name="mvp">Mvp.</param>
		public static void SetMvp(this UniformState state, string prefix, ModelViewProjectionParameters mvp)
		{
			mvp.SetUniforms(prefix, state);
		}
		/// <summary>
		/// Add the specified state, prefix and mvp.
		/// </summary>
		/// <param name="state">State.</param>
		/// <param name="prefix">Prefix.</param>
		/// <param name="mvp">Mvp.</param>
		public static void Add (this UniformState state, ModelViewProjectionParameters mvp, string prefix = "")
		{
			mvp.SetUniforms(prefix, state);
		}
	}
}

