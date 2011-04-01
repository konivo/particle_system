using System;

namespace OpenTK
{
	public static class MathHelper2
	{
		private static readonly Random m_Rnd = new Random();

		public static Vector4d RandomVector4 (double magnitude)
		{
			double dmag = 2 * magnitude;
			return new Vector4d (m_Rnd.NextDouble () * dmag, m_Rnd.NextDouble () * dmag, m_Rnd.NextDouble () * dmag, m_Rnd.NextDouble () * dmag) - new Vector4d (magnitude, magnitude, magnitude, magnitude);
		}

		public static Vector3d RandomVector3 (double magnitude)
		{
			double dmag = 2 * magnitude;
			return new Vector3d (m_Rnd.NextDouble () * dmag, m_Rnd.NextDouble () * dmag, m_Rnd.NextDouble () * dmag) - new Vector3d (magnitude, magnitude, magnitude);
		}

		public static Vector2d RandomVector2 (double magnitude)
		{
			double dmag = 2 * magnitude;
			return new Vector2d (m_Rnd.NextDouble () * dmag, m_Rnd.NextDouble () * dmag) - new Vector2d (magnitude, magnitude);
		}

		public static Vector4d RandomVector (Vector4d magnitude)
		{
			return Vector4d.Multiply(RandomVector4(1), magnitude);
		}

		public static Vector3d RandomVector (Vector3d magnitude)
		{
			return Vector3d.Multiply(RandomVector3(1), magnitude);
		}

		public static Vector2d RandomVector (Vector2d magnitude)
		{
			return Vector2d.Multiply(RandomVector2(1), magnitude);
		}
	}
}

