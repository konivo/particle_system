using System;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.GridRenderPass;
using opentk.Manipulators;
using System.ComponentModel;
using System.Linq.Expressions;

namespace opentk.System3
{
	public delegate void MapFunc(ref Vector4 input, ref Vector4 output);

	/// <summary>
	///
	/// </summary>
	[InheritedExport]
	public abstract class ChaoticMap
	{
		[Browsable(false)]
		public string Name
		{
			get;
			private set;
		}

		[Browsable(false)]
		public MapFunc Map
		{
			get;
			protected set;
		}

		public virtual void UpdateMap(DateTime simtime, long step)
		{}

		public ChaoticMap (string name)
		{
			Name = name;
		}

		public Tuple<double, Vector4> LyapunovExponent(int stepsCount = 100)
		{
			return LyapunovExponent((Vector4)MathHelper2.RandomVector4(23));
		}

		public Tuple<double, Vector4> LyapunovExponent(Vector4 startingPoint, int stepsCount = 100, float dt = 0.001f)
		{
			var stateA = startingPoint;

			var dA = stateA;
			var stateB = stateA + (Vector4)MathHelper2.RandomVector4(0.001);
			var dB = stateB;
			var result = 0.0;
			var deltaOld = 0f;
			var deltaNew = 0f;

      for (int i = 0; i < stepsCount; i++)
      {
				Map(ref stateA, ref dA);
				Map(ref stateB, ref dB);

				deltaOld = deltaNew;
				stateA = stateA + 0.001f * dA;
				stateB = stateB + 0.001f * dB;

				deltaNew = (stateA - stateB).Length;

				if(deltaOld == 0 || deltaNew == 0)
					continue;

				var d = 0.001f / deltaNew;
				stateB = stateA + (stateB - stateA) * d;

				d = deltaNew/deltaOld;
				if(float.IsNaN(d))
					continue;

				result += Math.Log(deltaNew/deltaOld) * 1000;
			}

			return Tuple.Create(result/ stepsCount, stateA);
		}

	}

	/// <summary>
	///
	/// </summary>
	public class LorenzMap : ChaoticMap
	{
		[DefaultValue(10.0)]
		public float Sigma
		{
			get;
			set;
		}
		[DefaultValue(28.0)]
		public float Rho
		{
			get;
			set;
		}
		[DefaultValue(2.6)]
		public float Beta
		{
			get;
			set;
		}

		public LorenzMap () : base("Lorenz")
		{
			Sigma = 10;
			Rho = 28;
			Beta = 2.5f;
			Map = Implementation;
		}

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;
			
			var x_n = Sigma * (y_p - x_p);
			var y_n = x_p * (Rho - z_p) - y_p;
			var z_n = x_p * y_p - z_p * Beta;

			output.X = x_n;
			output.Y = y_n;
			output.Z = z_n;
			
			//return new Vector4d (x_n, y_n, z_n, 0);
		}
	}

	/// <summary>
	///
	/// </summary>
	public class PickoverMap : ChaoticMap
	{
		public float A
		{
			get;
			set;
		}
		public float B
		{
			get;
			set;
		}
		public float C
		{
			get;
			set;
		}
		public float D
		{
			get;
			set;
		}

		public PickoverMap () : base("Pickover")
		{
			A = 1.425f;
			B = 1.24354f;
			C = 1.02435342f;
			D = 1.473503f;
			Map = Implementation;
		}

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;

			var z_n = (float)Math.Sin (x_p);
			var x_n = (float)Math.Sin (A * y_p) - z_p * (float)Math.Cos (B * x_p);
			var y_n = z_p * (float)Math.Sin (C * x_p) - (float)Math.Cos (D * y_p);

			output.X = x_n;
			output.Y = y_n;
			output.Z = z_n;
			
			//return new Vector4d (x_n, y_n, z_n, 0);
		}
	}

	/// <summary>
	///
	/// </summary>
	public class PolynomialMap : ChaoticMap
	{
		public float A
		{
			get;
			set;
		}
		public float B
		{
			get;
			set;
		}
		public float C
		{
			get;
			set;
		}

		public PolynomialMap () : base("Polynomial")
		{
			A = 1.425f;
			B = 1.24354f;
			C = 1.02435342f;
			Map = Implementation;
		}

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;

			var x_n = A + y_p - z_p * y_p;
			var y_n = B + z_p - z_p * x_p;
			var z_n = C + x_p - y_p * x_p;

			output.X = x_n;
			output.Y = y_n;
			output.Z = z_n;
			
			//return new Vector4d (x_n, y_n, z_n, 0);
		}
	}

	/// <summary>
	///
	/// </summary>
	public class ChuaMap : ChaoticMap
	{
		public float A
		{
			get;
			set;
		}
		public float B
		{
			get;
			set;
		}
		public float C
		{
			get;
			set;
		}

		public ChuaMap () : base("Chua")
		{
			A = 1.425f;
			B = 1.24354f;
			C = 1.02435342f;
			Map = Implementation;
		}

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;

			var x_n = A * (y_p - PhiFunction (x_p));
			var y_n = x_p - y_p + z_p;
			var z_n = -B * y_p - C * z_p;

			output.X = x_n;
			output.Y = y_n;
			output.Z = z_n;

			//return new Vector4d (x_n, y_n, z_n, 0);
		}

		private float PhiFunction (float x)
		{
			return 1 / 16.0f * (float)Math.Pow (x, 3) - 1 / 6.0f * x;
		}
	}

	/// <summary>
	///
	/// </summary>
	public class OwenMareshMap : ChaoticMap
	{
		public float A
		{
			get;
			set;
		}
		public float B
		{
			get;
			set;
		}
		public float C
		{
			get;
			set;
		}

		public OwenMareshMap () : base("OwenMaresh")
		{
			A = 2;
			B = 2;
			C = 7;
			Map = Implementation;
		}

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;

			var x_n = A * (float)Math.Cos(z_p - y_p);
			var y_n = B * (float)Math.Sin(x_p - z_p);
			var z_n = C * (float)Math.Cos(y_p - x_p);

			output.X = x_n;
			output.Y = y_n;
			output.Z = z_n;

			//return new Vector4d (x_n, y_n, z_n, 0);
		}
	}

	/// <summary>
	///
	/// </summary>
	public class CustomOwenMareshMap : ChaoticMap
	{
		public float A
		{
			get;
			set;
		}
		public float B
		{
			get;
			set;
		}
		public float C
		{
			get;
			set;
		}

		public CustomOwenMareshMap () : base("CustomOwenMaresh")
		{
			A = 2;
			B = 2;
			C = 2;
			Map = Implementation;
		}

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;

			var x_n = A * (float)Math.Cos(y_p + z_p);
			var y_n = B * (float)Math.Sin(x_p + z_p);
			var z_n = C * (float)Math.Cos(x_p + y_p);

			output.X = x_n;
			output.Y = y_n;
			output.Z = z_n;

			//return new Vector4d (x_n, y_n, z_n, 0);
		}
	}

	/// <summary>
//	///
//	/// </summary>
	public class C4D336MMap : ChaoticMap
	{
		private int ParamCount = 336;
		//private int Order = 3;
		private long m_lastrun;
		private long m_lastrunmask;

		public float[] a {get; private set;}
		public float[] target_state {get; private set;}
		public float[] mask_state {get; private set;}
		public float[] prev_state {get; private set;}

		public Tuple<double, Vector4> L {get; private set;}

		public C4D336MMap  () : base("C4D336MMap")
		{
			a = new float[ParamCount ];
			target_state = MathHelper2.RandomVectorSet(ParamCount , 0.3 * Vector2d.One).Select(x => (float)x.X).ToArray();

			Map = Implementation;
			InitializeParams();
			InitializeMask();
		}

		private void InitializeParams()
		{
			int tryNumber = 0;
			var a_prev = a;
			prev_state = target_state;

			do
			{
				a = target_state = MathHelper2.RandomVectorSet(ParamCount , Vector2d.One).Select(x => (float)x.X).ToArray();
			}
			while(Math.Abs((L = LyapunovExponent(500)).Item1) > 1.502 && tryNumber++ < 150);
			a = a_prev;
		}

		private void InitializeMask()
		{
			mask_state = Enumerable.Range(0, ParamCount).Select(x => MathHelper2.GetThreadLocalRandom().NextDouble() > 0.5? 1f: 0f).ToArray();
			for (int i = 0; i < a.Length; i++) {
				target_state[i] *= mask_state[i];
			}
		}

		public override void UpdateMap (DateTime simtime, long step)
		{
			if(m_lastrun + 100 < step )
			{
				InitializeParams();
				m_lastrun = step;
			}
			else if(m_lastrunmask + 400 < step )
			{
				InitializeMask();
				m_lastrunmask = step;
			}
			else
			{
				var t = (step - m_lastrun) / 100.0f ;
				for (int i = 0; i < a.Length; i++) {
					a[i] = prev_state[i] * (1 - t) + target_state[i] * t;
				}
			}
		}

		[ThreadStatic]
		private static float[] m_Koefs;

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			if(m_Koefs == null)
				m_Koefs = new float[84];

			m_Koefs[0] = input.X;
			m_Koefs[1] = input.Y;
			m_Koefs[2] = input.Z;
			m_Koefs[3] = input.W;

			for (int i = 0; i < 4; i++)
			  for (int j = 0; j < 4; j++)
					m_Koefs[i * 4 + j + 4] = m_Koefs[i] * m_Koefs[j];

			for (int i = 0; i < 4; i++)
			  for (int j = 0; j < 4; j++)
			    for (int k = 0; k < 4; k++)
						m_Koefs[i * 16 + j * 4 + k + 20] = m_Koefs[i] * m_Koefs[j] * m_Koefs[k];

			float x_n = 0;
			for (int i = 0; i < 84; i++)
				x_n += m_Koefs[i] * a[i + 0];

			float y_n = 0;
			for (int i = 0; i < 84; i++)
				y_n += m_Koefs[i] * a[i + 84];

			float z_n = 0;
			for (int i = 0; i < 84; i++)
				z_n += m_Koefs[i] * a[i + 168];

			float w_n = 0;
			for (int i = 0; i < 84; i++)
				w_n += m_Koefs[i] * a[i + 252];

			output.X = x_n;
			output.Y = y_n;
			output.Z = z_n;
			output.W = w_n;

			//return new Vector4d (x_n, y_n, z_n, 0);
		}
	}

	/// <summary>
	///
	/// </summary>
	public class Q4D60MMap : ChaoticMap
	{
		private int ParamCount = 61;
		private long m_lastrun;

		public float[] a {get; set;}
		public float[] target_state {get; set;}
		public float[] prev_state {get; set;}

		public float K1 {get; set;}
		public float K2 {get; set;}
		public float K0 {get; set;}


		public Q4D60MMap  () : base("Q4D60MMap ")
		{
			a = new float[ParamCount ];
			target_state = MathHelper2.RandomVectorSet(ParamCount , Vector2d.One).Select(x => (float)x.X).ToArray();

			InitializeParams();
			K1 = 0.1f;
			K2 = 0.01f;
			K0 = 0.01f;
			Map = Implementation;
		}

		private void InitializeParams()
		{
			prev_state = target_state;
			target_state = MathHelper2.RandomVectorSet(ParamCount , Vector2d.One).Select(x => (float)x.X).ToArray();
		}

		public override void UpdateMap (DateTime simtime, long step)
		{
			if(m_lastrun + 100 < step )
			{
				InitializeParams();
				m_lastrun = step;
			}
			else
			{
				var t = (step - m_lastrun) / 100.0f ;
				for (int i = 0; i < a.Length; i++) {
					a[i] = prev_state[i] * (1 - t) + target_state[i] * t;
				}
			}
		}

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			var X = input.X;
			var Y = input.Y;
			var Z = input.Z;
			var W = input.W;

			var X2 = (float)Math.Pow(X, 2);
			var Y2 = (float)Math.Pow(Y, 2);
			var Z2 = (float)Math.Pow(Z, 2);
			var W2 = (float)Math.Pow(W, 2);
			/*
			X = X + 0.1a1Y
	    Y = Y + 0.1(a2X + a3X3 + a4X2Y + a5XY2 + a6Y + a7Y3 + a8sin Z
		  Z = [Z + 0.1(a9 + 1.3)] mod 2π
		  W = (N - 1000) / (NMAX - 1000)
		  */
			var x_n = a[1] + a[2] * X + a[3] * X2 + a[4] * X * Y + a[5] * X* Z + a[6] * X* W + a[7]* Y + a[8]* Y2 + a[9]* Y* Z + a[10] * Y* W + a[11]* Z + a[12]* Z2 +	a[13]* Z* W + a[14]* W + a[15]* W2;
			var y_n = a[16] + a[17]* X + a[18]* X2 + a[19]* X* Y + a[20]* X* Z + a[21]* X* W + a[22]* Y + a[23]* Y2 + a[24]* Y* Z + a[25]* Y* W + a[26]* Z + a[27]* Z2 + a[28]* Z* W + a[29]* W + a[30]* W2;

			var z_n = a[31] + a[32]* X + a[33]* X2 + a[34]* X* Y + a[35]* X* Z + a[36]* X* W + a[37]* Y + a[38]* Y2 + a[39]* Y* Z + a[40]* Y* W + a[41]* Z + a[42]* Z2 + a[43]* Z* W + a[44]* W + a[45]* W2;
			var w_n = a[46] + a[47]* X + a[48]* X2 + a[49]* X* Y + a[50]* X* Z + a[51]* X* W + a[52]* Y + a[53]* Y2 + a[54]* Y* Z + a[55]* Y* W + a[56]* Z + a[57]* Z2 + a[58]* Z* W + a[59]* W + a[60]* W2;


			output.X = x_n;
			output.Y = y_n;
			output.Z = z_n;
			output.W = w_n;

			//return new Vector4d (x_n, y_n, z_n, 0);
		}
	}


	/// <summary>
	/// dx/dt = y, dy/dt = -x + yz, dz/dt = 1 - y2
	/// </summary>
	public class Sprotts1Map : ChaoticMap
	{
		public Sprotts1Map () : base("Sprotts1Map")
		{
			Map = Implementation;
		}

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			var x_n = input.Y;
			var y_n = -input.X + input.Y* input.Z;
			var z_n = 1 - (float)Math.Pow(input.Y, 2.0);

			output.X = x_n;
			output.Y = y_n;
			output.Z = z_n;

			//return new Vector4d (x_n, y_n, z_n, 0);
		}
	}

//	/// <summary>
//	/// dx/dt = y, dy/dt = -x + yz, dz/dt = 1 - y2
//	/// </summary>
//	public class TestMap : ChaoticMap
//	{
//		public TestMap () : base("TestMap")
//		{
//			Map = Implementation;
//		}
//
//		float sphere_sdb(Vector4d sphere, Vector4d pos)
//		{
//			return (pos.Xyz - sphere.Xyz).Length - sphere.W;
//		}
//
//		//
//		Vector3d sphere_sdb_grad(Vector4d sphere, Vector3d pos)
//		{
//			pos = pos - sphere.Xyz;
//			pos.Normalize();
//			return pos;
//		}
//
//		//
//		float torus_sdb(float r1, float r2, Vector4d pos)
//		{
//			float d1 = (pos.Xy.Length - r1);
//			d1 = Math.Sqrt(d1*d1 + pos.Z*pos.Z) - r2;
//
//			return d1;
//		}
//
//		//
//		Vector4d DomainMorphFunction(Vector4d pos)
//		{
//			var v1 = new Vector4d(
//				torus_sdb(40,  30, pos),
//				torus_sdb(40, 30, new Vector4d(pos.Y, pos.X, pos.Z, 0)),
//				torus_sdb(50, 20, new Vector4d(pos.Z, pos.Y, pos.X, 0)), 0);
//
//			var v2 = new Vector4d(
//				torus_sdb(60,  40, v1),
//				torus_sdb(60, 40, new Vector4d(v1.Z, v1.X, v1.Y, 0)),
//				torus_sdb(70, 50, new Vector4d(v1.Z, v1.Y, v1.X, 0)), 0);
//
//			var v3 = new Vector4d(
//				torus_sdb(60,  60, v2 - new Vector4d(5, 10, 11, 0)),
//				torus_sdb(60, 60, new Vector4d(v2.Z, v2.X, v2.Y, 0)  - new Vector4d(-5, 10, 0, 0)),
//				torus_sdb(60, 60, new Vector4d(v2.Z, v2.Y, v2.X, 0) - new Vector4d(5, 0, -11, 0)), 0);
//
//			return v3;
//		}
//
//		//
//		float SDBValue(Vector4d pos)
//		{
//			var mpos = DomainMorphFunction(pos);
//			//return torus_sdb(50, 10, mpos) * 0.2;
//			return sphere_sdb(new Vector4d(0, 0, 0, 28), mpos) * 0.2;
//		}
//
//		//s
//		private void Implementation (ref Vector4 input, ref Vector4 output)
//		{
//			return DomainMorphFunction(input);
//		}
//	}

	/// <summary>
	/// dx/dt = y + x*(R - v_l)/v_l, dy/dt = -x + y*(R - v_l)/v_l, dz/dt = 1
	/// </summary>
	public class TubularMap : ChaoticMap
	{
		public float R
		{
			get;
			set;
		}

		public TubularMap () : base("TubularMap")
		{
			Map = Implementation;
		}

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			var v_l = input.Length;
			var K2 = (R - v_l);

			var x_n = input.Y + K2 * input.X;
			var y_n = -input.X + K2 * input.Y;
			var z_n = 0.3f;

			output.X = x_n;
			output.Y = y_n;
			output.Z = z_n;

			//return new Vector4d (x_n, y_n, z_n, 0);
		}
	}


}

