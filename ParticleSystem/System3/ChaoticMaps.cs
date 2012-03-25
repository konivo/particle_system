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
		protected long m_lastrun;
		protected long m_lastrunmask;
		protected float m_SearchAttemptsLeft;
		protected float[] m_BestNextCandidate;

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

		[Browsable(false)]
		public abstract int ParamCount
		{
			get;
		}

		public float[] a
		{
			get;
			private set;
		}

		public float[] target_state
		{
			get;
			private set;
		}

		public float[] mask_state
		{
			get;
			private set;
		}

		public float[] prev_state
		{
			get;
			private set;
		}

		public Tuple<double, Vector4> L
		{
			get;
			private set;
		}

		public int ChangePeriod
		{
			get;
			set;
		}

		public float TransitionRate
		{
			get;
			set;
		}

		public float SearchOrbitSeedExtent
		{
			get;
			set;
		}

		public float SearchAttemptsCount { get; set;}

		public float SearchOrbitDistanceLimit { get; set;}

		public float SearchDesiredMaxL { get; set;}

		public float SearchDesiredMinL { get; set;}

		public float SearchDt { get; set;}

		public int SearchLoopIterCount { get; set;}

		public ChaoticMap (string name)
		{
			Name = name;

			ChangePeriod = 1;
			SearchDesiredMaxL = 1f;
			SearchDesiredMinL = 0.1f;
			SearchAttemptsCount = 0.0f;
			SearchLoopIterCount = 500;
			SearchOrbitSeedExtent = 1;
			SearchDt = 0.01f;
			TransitionRate = 1f;

			a = new float[ParamCount];
			prev_state = target_state = MathHelper2.RandomVectorSet(ParamCount , Vector2d.One).Select(x => (float)x.X).ToArray();
			mask_state = Enumerable.Range(0, ParamCount).Select(x => 1f).ToArray();
		}

		private void SearchForParams()
		{
			var oldA = a;
			var oldState = target_state;
			var newL = L;
			m_SearchAttemptsLeft += SearchAttemptsCount;

			for(; 1 <= m_SearchAttemptsLeft; m_SearchAttemptsLeft--)
			{
				//a = target_state = MathHelper2.RandomVectorSet(ParamCount , Vector2d.One).Select(x => (float)x.X).ToArray();
				a = target_state = MathHelper2.RandomVectorSet(ParamCount , Vector2d.One).Select(x => (float)x.X)
				.Zip(target_state, (x, y) => x + 0.01f * y).ToArray();

				for (int i = 0; i < a.Length; i++)
				{
					target_state[i] *= mask_state[i];
				}

				newL = LyapunovExponent(SearchLoopIterCount, SearchDt, SearchOrbitSeedExtent, 10, SearchOrbitDistanceLimit);
				if(double.IsNaN(newL.Item1))
					continue;

				if(L == null)
				{
					m_BestNextCandidate = target_state;
					L = newL;
				}
				else if(
					!double.IsNaN(newL.Item1) &&
					newL.Item1 > SearchDesiredMinL && newL.Item1 < SearchDesiredMaxL && newL.Item2.Length < SearchOrbitDistanceLimit)
				{
					if(newL.Item1 > L.Item1 || L.Item1 > SearchDesiredMaxL)
					{
						m_BestNextCandidate = target_state;
						L = newL;
					}
				}
			}

			a = oldA;
			target_state = oldState;
		}

		private void InitializeParams()
		{
			prev_state = target_state;
			if(m_BestNextCandidate != null)
			{
				target_state = m_BestNextCandidate;
				m_BestNextCandidate = null;
				L = null;
			}
		}

		private void InitializeMask()
		{
//			mask_state = Enumerable.Range(0, ParamCount).Select(x => MathHelper2.GetThreadLocalRandom().NextDouble() > 0.5? 1f: 0f).ToArray();
//			for (int i = 0; i < a.Length; i++) {
//				target_state[i] *= mask_state[i];
//			}
		}

		public virtual void UpdateMap (DateTime simtime, long step)
		{
			SearchForParams();

			if(m_lastrun + ChangePeriod < step )
			{
				InitializeParams();
				m_lastrun = step;
			}
			else if(m_lastrunmask + 10 * ChangePeriod < step )
			{
				InitializeMask();
				m_lastrunmask = step;
			}
			else
			{
				var t = (step - m_lastrun) / (ChangePeriod * TransitionRate);
				if(t <= 1)
				{
					for (int i = 0; i < a.Length; i++) {
						a[i] = prev_state[i] * (1 - t) + target_state[i] * t;
					}
				}
			}
		}

		/// <summary>
		/// Lyapunovs the exponent.
		/// </summary>
		/// <returns>
		/// The exponent.
		/// </returns>
		/// <param name='stepsCount'>
		/// Steps count.
		/// </param>
		/// <param name='dt'>
		/// Dt.
		/// </param>
		/// <param name='seedExtent'>
		/// Seed extent.
		/// </param>
		/// <param name='seedCount'>
		/// Seed count.
		/// </param>
		/// <param name='orbitDistanceLimit'>
		/// Orbit distance limit.
		/// </param>
		public Tuple<double, Vector4> LyapunovExponent(int stepsCount = 100, float dt = 0.1f, float seedExtent = 5, int seedCount = 5, float orbitDistanceLimit = 1000)
		{
			var exponents =
				from i in Enumerable.Range(0, seedCount)
				let l = LyapunovExponent((Vector4)MathHelper2.RandomVector4(seedExtent), stepsCount / seedCount, dt, orbitDistanceLimit)
				where l.Item1 != double.PositiveInfinity
				select l;

			var sum = exponents.Aggregate(Tuple.Create(0.0, new Vector4()), (aggr, y) => Tuple.Create(aggr.Item1 + y.Item1, aggr.Item2 + y.Item2));
			return Tuple.Create(sum.Item1 / seedCount, sum.Item2 / seedCount);
		}

		/// <summary>
		/// Lyapunovs the exponent.
		/// </summary>
		/// <returns>
		/// The exponent.
		/// </returns>
		/// <param name='startingPoint'>
		/// Starting point.
		/// </param>
		/// <param name='stepsCount'>
		/// Steps count.
		/// </param>
		/// <param name='dt'>
		/// Dt.
		/// </param>
		/// <param name='orbitDistanceLimit'>
		/// Orbit distance limit.
		/// </param>
		public Tuple<double, Vector4> LyapunovExponent(Vector4 startingPoint, int stepsCount = 100, float dt = 0.1f, float orbitDistanceLimit = 1000)
		{
			var stateA = startingPoint;
			var stateB = stateA + (Vector4)MathHelper2.RandomVector4(0.001);
			var dB = stateB;
			var dA = stateA;

			var result = 0.0;
			var deltaOld = 0f;
			var deltaNew = 0f;
			int i = 1;

      for (; i <= stepsCount; i++)
      {
				Map(ref stateA, ref dA);
				Map(ref stateB, ref dB);

				deltaOld = deltaNew;
				stateA = stateA + dt * dA;
				stateB = stateB + dt * dB;

				var isNan = double.IsNaN(deltaNew) || double.IsNaN(deltaOld);
				deltaNew = (stateA - stateB).Length;

				if(!isNan && (deltaOld == 0 || deltaNew == 0))
					continue;
				else if(!isNan && orbitDistanceLimit > Math.Max(stateA.Length, stateB.Length))
				{
					stateB = stateA + (stateB - stateA) * deltaOld / deltaNew;
					result += Math.Log(deltaNew/deltaOld) / dt;
				}
				else
				{
					//restart the run
					//stepsCount -= i;
					//i = 1;
					//result = 0;

					return Tuple.Create(double.PositiveInfinity, stateA);
				}
			}

			return Tuple.Create(result/ i, stateA);
		}

	}

	/// <summary>
	///
	/// </summary>
	public class LorenzMap : ChaoticMap
	{
		#region implemented abstract members of opentk.System3.ChaoticMap
		public override int ParamCount
		{
			get
			{
				return 3;
			}
		}
		#endregion

		[DefaultValue(10.0)]
		public float Sigma
		{
			get{ return mask_state[0];}
			set{ mask_state[0] = value;}
		}
		[DefaultValue(28.0)]
		public float Rho
		{
			get{ return mask_state[1];}
			set{ mask_state[1] = value;}
		}
		[DefaultValue(2.6)]
		public float Beta
		{
			get{ return mask_state[2];}
			set{ mask_state[2] = value;}
		}

		public LorenzMap () : base("Lorenz")
		{
			Sigma = 10;
			Rho = 28;
			Beta = 2.5f;

			TransitionRate = 1;
			ChangePeriod = 1;

			target_state[0] = 1;
			target_state[1] = 1;
			target_state[2] = 1;

			Map = Implementation;
		}

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;
			
			var x_n = a[0] * (y_p - x_p);
			var y_n = x_p * (a[1] - z_p) - y_p;
			var z_n = x_p * y_p - z_p * a[2];

			output.X = x_n;
			output.Y = y_n;
			output.Z = z_n;
		}
	}

	/// <summary>
	///
	/// </summary>
	public class PickoverMap : ChaoticMap
	{
		#region implemented abstract members of opentk.System3.ChaoticMap
		public override int ParamCount
		{
			get
			{
				return 4;
			}
		}
		#endregion

		public float A
		{
			get{ return mask_state[0];}
			set{ mask_state[0] = value;}
		}
		public float B
		{
			get{ return mask_state[1];}
			set{ mask_state[1] = value;}
		}
		public float C
		{
			get{ return mask_state[2];}
			set{ mask_state[2] = value;}
		}
		public float D
		{
			get{ return mask_state[3];}
			set{ mask_state[3] = value;}
		}

		public PickoverMap () : base("Pickover")
		{
			A = 1.425f;
			B = 1.24354f;
			C = 1.02435342f;
			D = 1.473503f;

			TransitionRate = 1;
			ChangePeriod = 1;

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
		#region implemented abstract members of opentk.System3.ChaoticMap
		public override int ParamCount
		{
			get
			{
				return 3;
			}
		}
		#endregion

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
		}
	}

	/// <summary>
	///
	/// </summary>
	public class ChuaMap : ChaoticMap
	{
		#region implemented abstract members of opentk.System3.ChaoticMap
		public override int ParamCount
		{
			get
			{
				return 3;
			}
		}
		#endregion

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
		#region implemented abstract members of opentk.System3.ChaoticMap
		public override int ParamCount
		{
			get
			{
				return 3;
			}
		}
		#endregion

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
		}
	}

	/// <summary>
	///
	/// </summary>
	public class CustomOwenMareshMap : ChaoticMap
	{
		#region implemented abstract members of opentk.System3.ChaoticMap
		public override int ParamCount
		{
			get
			{
				return 3;
			}
		}
		#endregion

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
		}
	}

	/// <summary>
//	///
//	/// </summary>
	public class C4D336MMap : ChaoticMap
	{
		public override int ParamCount { get{ return 336;}}
//		private long m_lastrun;
//		private long m_lastrunmask;
//		private float m_SearchAttemptsLeft;
//		private float[] m_BestNextCandidate;
//
//		public float[] a {get; private set;}
//		public float[] target_state {get; private set;}
//		public float[] mask_state {get; private set;}
//		public float[] prev_state {get; private set;}
//
//		public Tuple<double, Vector4> L { get; private set;}
//
//		public int ChangePeriod { get; private set;}
//
//		public float TransitionRate { get; private set;}
//
//		public float SearchOrbitSeedExtent { get; private set;}
//
//		//public int SearchPeriod { get; private set;}
//
//		public float SearchAttemptsCount { get; private set;}
//
//		public float SearchOrbitDistanceLimit { get; private set;}
//
//		public float SearchDesiredMaxL { get; private set;}
//
//		public float SearchDesiredMinL { get; private set;}
//
//		public float SearchDt { get; private set;}
//
//		public int SearchLoopIterCount { get; private set;}

		public C4D336MMap  () : base("C4D336MMap")
		{
			ChangePeriod = 200;
			SearchDesiredMaxL = 1f;
			SearchDesiredMinL = 0.1f;
			SearchAttemptsCount = 0.2f;
			SearchLoopIterCount = 500;
			SearchOrbitSeedExtent = 1;
			SearchDt = 0.01f;
			TransitionRate = 0.1f;
//			a = new float[ParamCount ];
//
//			m_BestNextCandidate = target_state = MathHelper2.RandomVectorSet(ParamCount , Vector2d.One).Select(x => (float)x.X).ToArray();
//			mask_state = Enumerable.Range(0, ParamCount).Select(x => 1f).ToArray();
			Map = Implementation;

//			InitializeParams();
//			InitializeMask();
		}

//		private void SearchForParams()
//		{
//			var oldA = a;
//			var oldState = target_state;
//			var newL = L;
//			m_SearchAttemptsLeft += SearchAttemptsCount;
//
//			for(; 1 <= m_SearchAttemptsLeft; m_SearchAttemptsLeft--)
//			{
//				//a = target_state = MathHelper2.RandomVectorSet(ParamCount , Vector2d.One).Select(x => (float)x.X).ToArray();
//				a = target_state = MathHelper2.RandomVectorSet(ParamCount , Vector2d.One).Select(x => (float)x.X)
//				.Zip(target_state, (x, y) => x + 0.01f * y).ToArray();
//
//				for (int i = 0; i < a.Length; i++)
//				{
//					target_state[i] *= mask_state[i];
//				}
//
//				newL = LyapunovExponent(SearchLoopIterCount, SearchDt, SearchOrbitSeedExtent, 10, SearchOrbitDistanceLimit);
//				if(double.IsNaN(newL.Item1))
//					continue;
//
//				if(L == null)
//				{
//					m_BestNextCandidate = target_state;
//					L = newL;
//				}
//				else if(
//					!double.IsNaN(newL.Item1) &&
//					newL.Item1 > SearchDesiredMinL && newL.Item1 < SearchDesiredMaxL && newL.Item2.Length < SearchOrbitDistanceLimit)
//				{
//					if(newL.Item1 > L.Item1 || L.Item1 > SearchDesiredMaxL)
//					{
//						m_BestNextCandidate = target_state;
//						L = newL;
//					}
//				}
//			}
//
//			a = oldA;
//			target_state = oldState;
//		}
//
//		private void InitializeParams()
//		{
//			prev_state = target_state;
//			if(m_BestNextCandidate != null)
//			{
//				target_state = m_BestNextCandidate;
//				m_BestNextCandidate = null;
//				L = null;
//			}
//		}
//
//		private void InitializeMask()
//		{
////			mask_state = Enumerable.Range(0, ParamCount).Select(x => MathHelper2.GetThreadLocalRandom().NextDouble() > 0.5? 1f: 0f).ToArray();
////			for (int i = 0; i < a.Length; i++) {
////				target_state[i] *= mask_state[i];
////			}
//		}
//
//		public override void UpdateMap (DateTime simtime, long step)
//		{
//			SearchForParams();
//
//			if(m_lastrun + ChangePeriod < step )
//			{
//				InitializeParams();
//				m_lastrun = step;
//			}
//			else if(m_lastrunmask + 10 * ChangePeriod < step )
//			{
//				InitializeMask();
//				m_lastrunmask = step;
//			}
//			else
//			{
//				var t = (step - m_lastrun) / (ChangePeriod * TransitionRate);
//				if(t <= 1)
//				{
//					for (int i = 0; i < a.Length; i++) {
//						a[i] = prev_state[i] * (1 - t) + target_state[i] * t;
//					}
//				}
//			}
//		}

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
		}
	}

	/// <summary>
	///
	/// </summary>
	public class Q4D60MMap : ChaoticMap
	{
//		private int ParamCount = 61;
//		private long m_lastrun;
//
//		public float[] a {get; set;}
//		public float[] target_state {get; set;}
//		public float[] prev_state {get; set;}

		public float K1 {get; set;}
		public float K2 {get; set;}
		public float K0 {get; set;}

		#region implemented abstract members of opentk.System3.ChaoticMap
		public override int ParamCount
		{
			get
			{
				return 61;
			}
		}
		#endregion


		public Q4D60MMap  () : base("Q4D60MMap ")
		{
//			a = new float[ParamCount ];
//			target_state = MathHelper2.RandomVectorSet(ParamCount , Vector2d.One).Select(x => (float)x.X).ToArray();
//
//			InitializeParams();
			K1 = 0.1f;
			K2 = 0.01f;
			K0 = 0.01f;
			Map = Implementation;
		}

//		private void InitializeParams()
//		{
//			prev_state = target_state;
//			target_state = MathHelper2.RandomVectorSet(ParamCount , Vector2d.One).Select(x => (float)x.X).ToArray();
//		}
//
//		public override void UpdateMap (DateTime simtime, long step)
//		{
//			if(m_lastrun + 100 < step )
//			{
//				InitializeParams();
//				m_lastrun = step;
//			}
//			else
//			{
//				var t = (step - m_lastrun) / 100.0f ;
//				for (int i = 0; i < a.Length; i++) {
//					a[i] = prev_state[i] * (1 - t) + target_state[i] * t;
//				}
//			}
//		}

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
		#region implemented abstract members of opentk.System3.ChaoticMap
		public override int ParamCount
		{
			get
			{
				return 3;
			}
		}
		#endregion

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

	/// <summary>
	/// dx/dt = y + x*(R - v_l)/v_l, dy/dt = -x + y*(R - v_l)/v_l, dz/dt = 1
	/// </summary>
	public class TubularMap : ChaoticMap
	{
		#region implemented abstract members of opentk.System3.ChaoticMap
		public override int ParamCount
		{
			get
			{
				return 3;
			}
		}
		#endregion

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

