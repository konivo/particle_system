using System;
using System.Threading;
using System.Linq;

namespace OpenTK
{
	public static class MathHelper2
	{
		private static ThreadLocal<Random> m_Rnd = new ThreadLocal<Random>(() => new Random());
		private static ThreadLocal<Random> m_Rnd1 = new ThreadLocal<Random>(() => new MersenneTwister(154352));
		private static ThreadLocal<Random> m_Rnd2 = new ThreadLocal<Random>(() => new MersenneTwister(245346666));
		private static ThreadLocal<Random> m_Rnd3 = new ThreadLocal<Random>(() => new MersenneTwister(342));
		private static ThreadLocal<Random> m_Rnd4 = new ThreadLocal<Random>(() => new MersenneTwister(464353546));

//		private static ThreadLocal<Random> m_Rnd1 = new ThreadLocal<Random>(() => new Random(154352));
//		private static ThreadLocal<Random> m_Rnd2 = new ThreadLocal<Random>(() => new Random(245346666));
//		private static ThreadLocal<Random> m_Rnd3 = new ThreadLocal<Random>(() => new Random(342));
//		private static ThreadLocal<Random> m_Rnd4 = new ThreadLocal<Random>(() => new Random(464353546));

		public static Random GetThreadLocalRandom ()
		{
			return m_Rnd.Value;
		}

		public static Vector4d RandomVector4 (double magnitude)
		{
			double dmag = 2 * magnitude;
			return new Vector4d (m_Rnd1.Value.NextDouble () * dmag, m_Rnd2.Value.NextDouble () * dmag, m_Rnd3.Value.NextDouble () * dmag, m_Rnd4.Value.NextDouble () * dmag) - new Vector4d (magnitude, magnitude, magnitude, magnitude);
		}

		public static Vector3d RandomVector3 (double magnitude)
		{
			double dmag = 2 * magnitude;
			return new Vector3d (m_Rnd1.Value.NextDouble () * dmag, m_Rnd2.Value.NextDouble () * dmag, m_Rnd3.Value.NextDouble () * dmag) - new Vector3d (magnitude, magnitude, magnitude);
		}

		public static Vector2d RandomVector2 (double magnitude)
		{
			double dmag = 2 * magnitude;
			return new Vector2d (m_Rnd1.Value.NextDouble () * dmag, m_Rnd2.Value.NextDouble () * dmag) - new Vector2d (magnitude, magnitude);
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

		public static Vector2d Modulo (Vector2d val, Vector2d mod)
		{
			val = new Vector2d(val.X % mod.X, val.Y % mod.Y) + mod;
			return new Vector2d(val.X % mod.X, val.Y % mod.Y);
		}

		//todo: do it better, it has huge impact on ssao
		public static Vector2d[] RandomVectorSet (int w, Vector2d magnitude)
		{
			var result = new Vector2d[w];

			var magnitudeDelta = Vector2d.Multiply(magnitude, 1.0 / w);
			for (int i = 0; i < w; i++)
			{
				var highnoise = MathHelper2.RandomVector(magnitudeDelta);
				var basedir = MathHelper2.RandomVector(magnitude);
				var rand = highnoise + Vector2d.Multiply(basedir, magnitude * i / (w * basedir.Length));
				result[i] = rand;
			}

			//var str = string.Join(Environment.NewLine, result);

			return result;
		}

		//todo: do it better, it has huge impact on ssao
		public static Vector2d[] RegularVectorSet (int w, Vector2d magnitude)
		{
			var result = new Vector2d[w];
			var sqr = (int)Math.Sqrt(w);
			if(sqr <= 1)
				return result;

			for (int i = 0; i < sqr; i++)
			{
				for(int j = 0; j < sqr; j++)
				{
					var delta = 2.0 / (sqr - 1);
					var index = i * sqr + j;

					if(index >= w)
					 break;

					result[index] = new Vector2d(magnitude.X * ( -1 + delta * i), magnitude.Y * ( -1 + delta * j));
				}
			}

			//var str = string.Join(Environment.NewLine, result);

			return result;
		}

		//todo: do it better, it has huge impact on ssao
		public static Vector2[] RegularVectorSet (int w, Vector2 magnitude)
		{
			var result = RegularVectorSet(w, (Vector2d) magnitude);
			return result.Select(x => (Vector2)x).ToArray();
		}

		//todo: do it better, it has huge impact on ssao
		public static Vector2[] RandomVectorSet (int w, Vector2 magnitude)
		{
			var result = RandomVectorSet(w, (Vector2d) magnitude);
			return result.Select(x => (Vector2)x).ToArray();
		}

		public static Vector4d PlaneFrom(Vector3d o, Vector3d a, Vector3d b, double planeoffset)
		{
			var c = Vector3d.Cross(a - o, b - o);
			c.Normalize();
			double prod;
			Vector3d.Dot(ref o, ref c, out prod);

			return new Vector4d(c, -prod - planeoffset);
		}

		public static Vector4 PlaneFrom(Vector3 o, Vector3 a, Vector3 b, float planeoffset)
		{
			var c = Vector3.Cross(a - o, b - o);
			c.Normalize();
			float prod;
			Vector3.Dot(ref o, ref c, out prod);

			return new Vector4(c, -prod - planeoffset);
		}

		public static T Clamp<T> (T val, T min, T max) where T: IComparable<T>
		{
			return val.CompareTo(min) < 0? min : val.CompareTo(max) > 0? max : val;
		}
	}
}

