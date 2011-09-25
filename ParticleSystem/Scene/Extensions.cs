using System;
using System.Linq;
using OpenTK;

namespace opentk.Scene
{
	public static class Extensions
	{
/// <summary>
		///
		/// </summary>
		/// <param name="state">
		/// A <see cref="UniformState"/>
		/// </param>
		/// <param name="prefix">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="mvp">
		/// A <see cref="ModelViewProjectionParameters"/>
		/// </param>
		public static void SetMvp(this UniformState state, string prefix, ModelViewProjectionParameters mvp)
		{
			mvp.SetUniforms(prefix, state);
		}
	}
}

