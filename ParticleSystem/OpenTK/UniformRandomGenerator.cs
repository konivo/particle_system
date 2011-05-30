using System;

namespace OpenTK
{
	public class UniformRandomGenerator : Random
	{
		uint x = 123456789;
		uint y = 362436069;
		uint z = 521288629;
		uint w = 88675123;

		public UniformRandomGenerator (int seed)
		{
		}

		protected override double Sample ()
		{
			uint t;

			t = x ^ (x << 11);
			x = y;
			y = z;
			z = w;
			return (w = w ^ (w >> 19) ^ (t ^ (t >> 8)))/ (double) uint.MaxValue;
			
		}
	}
}

