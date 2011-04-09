using System;

namespace OpenTK.Extensions
{
	public static class MathExtensions
	{
		public static Vector3 NormalizedCoord(this Vector4 v){
			return v.Xyz / v.W;
		}

		public static Vector3d NormalizedCoord(this Vector4d v){
			return v.Xyz / v.W;
		}
	}
}

