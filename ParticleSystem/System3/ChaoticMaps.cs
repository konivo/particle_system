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

		public float[] bias_state
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

		public float SearchSpaceDiversityFactor { get; set;}

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
			mask_state = Enumerable.Range(0, ParamCount).Select(x => 5f).ToArray();
			bias_state = Enumerable.Range(0, ParamCount).Select(x => 0f).ToArray();
		}

		private void SearchForParams()
		{
			var oldA = a;
			var oldState = target_state;
			var newL = L;
			bool found = false;
			m_SearchAttemptsLeft += SearchAttemptsCount;

			for(; 1 <= m_SearchAttemptsLeft; m_SearchAttemptsLeft--)
			{
				//a = target_state = MathHelper2.RandomVectorSet(ParamCount , Vector2d.One).Select(x => (float)x.X).ToArray();
				target_state =
					MathHelper2
					.RandomVectorSet(ParamCount , Vector2d.One)
					.Select(
							(x,i) => (float)x.X/* * mask_state[i]*/)
					.Zip(target_state,
							(x, y) => (1 - SearchSpaceDiversityFactor) * y + SearchSpaceDiversityFactor * x)
					.Select(
							(x, i) => MathHelper2.Clamp(x, -1, 1))
					.ToArray();

				//
				a = target_state
					.Select(
						(x, i) => mask_state[i] * x + bias_state[i])
					.ToArray();

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
					found = true;
				}
			}

			UpdateSearchSpreadFactor(found);

			a = oldA;
			target_state = oldState;
		}

		private void UpdateSearchSpreadFactor(bool solutionFound)
		{
			if(solutionFound)
				SearchSpaceDiversityFactor = Math.Max(0.01f, SearchSpaceDiversityFactor * 0.9f);
			else
				SearchSpaceDiversityFactor = Math.Min(1.00f, SearchSpaceDiversityFactor * 1.1f);
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

		private void InitializeNewCandidate()
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
				SearchSpaceDiversityFactor = 1;
				m_lastrunmask = step;
			}
			else
			{
				var t = (step - m_lastrun) / (ChangePeriod * TransitionRate);
				if(t <= 1)
				{
					for (int i = 0; i < a.Length; i++) {
						a[i] = (prev_state[i] * (1 - t) + target_state[i] * t) * mask_state[i] + bias_state[i];
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
			var x_n = (float)Math.Sin (a[0] * y_p) - z_p * (float)Math.Cos (a[1] * x_p);
			var y_n = z_p * (float)Math.Sin (a[2] * x_p) - (float)Math.Cos (a[3] * y_p);

			output.X = x_n;
			output.Y = y_n;
			output.Z = z_n;
			
			//return new Vector4d (x_n, y_n, z_n, 0);
		}
	}

	/// <summary>
	///
	/// </summary>
	public class CustomPickoverMap : ChaoticMap
	{
		#region implemented abstract members of opentk.System3.ChaoticMap
		public override int ParamCount
		{
			get
			{
				return 9;
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
		public float E
		{
			get{ return mask_state[4];}
			set{ mask_state[4] = value;}
		}
		public float F
		{
			get{ return mask_state[5];}
			set{ mask_state[5] = value;}
		}

		public CustomPickoverMap () : base("CustomPickoverMap")
		{
			A = 1.425f;
			B = 1.24354f;
			C = 1.02435342f;
			mask_state[8] = mask_state[7] = mask_state[6] = E = F = D = 1.473503f;

			TransitionRate = 1;
			ChangePeriod = 1;

			Map = Implementation;
		}

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;

			var z_n = y_p * (float)Math.Sin (a[0] * x_p) - x_p * (float)Math.Cos (a[1] * y_p) - y_p * (float)Math.Cos (a[2] * z_p);
			var x_n = z_p * (float)Math.Sin (a[3] * x_p) - y_p * (float)Math.Cos (a[4] * x_p) - z_p * (float)Math.Cos (a[5] * x_p);
			var y_n = z_p * (float)Math.Sin (a[6] * x_p) - x_p * (float)Math.Cos (a[7] * z_p) - x_p * (float)Math.Cos (a[8] * y_p);

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

		public PolynomialMap () : base("Polynomial")
		{
			A = 1.425f;
			B = 1.24354f;
			C = 1.02435342f;

			TransitionRate = 1;
			ChangePeriod = 1;
			Map = Implementation;
		}

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;

			var x_n = a[0] + y_p - z_p * y_p;
			var y_n = a[1] + z_p - z_p * x_p;
			var z_n = a[2] + x_p - y_p * x_p;

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

		public ChuaMap () : base("Chua")
		{
			A = 1.425f;
			B = 1.24354f;
			C = 1.02435342f;

			TransitionRate = 1;
			ChangePeriod = 1;
			Map = Implementation;
		}

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;

			var x_n = a[0] * (y_p - PhiFunction (x_p));
			var y_n = x_p - y_p + z_p;
			var z_n = -a[1] * y_p - a[2] * z_p;

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

			var x_n = a[0] * (float)Math.Cos(z_p - y_p);
			var y_n = a[1] * (float)Math.Sin(x_p - z_p);
			var z_n = a[2] * (float)Math.Cos(y_p - x_p);

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
			Map = Implementation;
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
		}
	}

	/// <summary>
	///
	/// </summary>
	public class Q4D60MMap : ChaoticMap
	{
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
			K1 = 0.1f;
			K2 = 0.01f;
			K0 = 0.01f;
			Map = Implementation;
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

			var x_n = a[1] + a[2] * X + a[3] * X2 + a[4] * X * Y + a[5] * X* Z + a[6] * X* W + a[7]* Y + a[8]* Y2 + a[9]* Y* Z + a[10] * Y* W + a[11]* Z + a[12]* Z2 +	a[13]* Z* W + a[14]* W + a[15]* W2;
			var y_n = a[16] + a[17]* X + a[18]* X2 + a[19]* X* Y + a[20]* X* Z + a[21]* X* W + a[22]* Y + a[23]* Y2 + a[24]* Y* Z + a[25]* Y* W + a[26]* Z + a[27]* Z2 + a[28]* Z* W + a[29]* W + a[30]* W2;

			var z_n = a[31] + a[32]* X + a[33]* X2 + a[34]* X* Y + a[35]* X* Z + a[36]* X* W + a[37]* Y + a[38]* Y2 + a[39]* Y* Z + a[40]* Y* W + a[41]* Z + a[42]* Z2 + a[43]* Z* W + a[44]* W + a[45]* W2;
			var w_n = a[46] + a[47]* X + a[48]* X2 + a[49]* X* Y + a[50]* X* Z + a[51]* X* W + a[52]* Y + a[53]* Y2 + a[54]* Y* Z + a[55]* Y* W + a[56]* Z + a[57]* Z2 + a[58]* Z* W + a[59]* W + a[60]* W2;


			output.X = x_n;
			output.Y = y_n;
			output.Z = z_n;
			output.W = w_n;
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

	/// <summary>
	///
	/// </summary>
	public class SpiralMap : ChaoticMap
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

		public SpiralMap () : base("SpiralMap")
		{
			Map = Implementation;
		}

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			/*
			 df/dt =  flow map ...

			 single path:

			 phi <=> t
			 x = R * sin(w * phi + k),
			 y = R * cos(w * phi + k),
			 R = Rmax * sin(d * phi + k)
			 z = Rmax * cos(d* phi + k)

			 Rmax = sqrt(x^2 + y ^2 + z^2)

			 dx/dphi = Rmax * cos (w * phi + k)* sin(d * phi + k) +  Rmax * cos (d * phi + k)* sin(w * phi + k)
			 dy/dphi = - Rmax * sin(w * phi + k) * sin(d * phi + k) + Rmax * cos(w * phi + k) * sin(d * phi + k)
			 dz/dphi = -Rmax * sin(phi + k)
			*/

			if( output.W == 0)
			{
				output = Vector4.Zero;
			}

			var Rmax = input.Xyz.Length;

			var x_n = input.Y + input.Z * input.X / (float)(Rmax * Rmax - Math.Pow(input.Z, 2));
			var y_n = - input.X + input.Z * input.Y / (float)(Rmax * Rmax - Math.Pow(input.Z, 2));
			var z_n = (float)Math.Sqrt(Math.Pow(input.X, 2) + Math.Pow(input.Y, 2));

			var d = new Vector3(x_n, y_n, z_n);
			//d.Normalize();

			if( output.W ++ < 3)
			{
				output.X -= d.Y ;
				output.Y -= d.Z;
				output.Z -= d.X;

				//var tmp = output;
				var tmp = new Vector4(input.Z, input.X, input.Y, 0);
				Implementation(ref tmp, ref output);
			}
			else
			{
				output.X -= d.Y ;
				output.Y -= d.Z;
				output.Z -= d.X;
				output.W = 0;
			}
		}
	}

	/// <summary>
	///
	/// </summary>
	public class SpiralBMap : ChaoticMap
	{
		private int m_CenterCount = 30;
		private int m_CenterParamCount = 5;

		#region implemented abstract members of opentk.System3.ChaoticMap
		public override int ParamCount
		{
			get
			{
				return m_CenterCount * m_CenterParamCount;
			}
		}
		#endregion

		public SpiralBMap () : base("SpiralBMap")
		{
			Map = Implementation;

			for(int i = 0; i < m_CenterCount * m_CenterParamCount; i+= m_CenterParamCount)
			{
				mask_state[i] = mask_state[i + 1] = mask_state[i + 2] = 50;
				mask_state[i + 3] = 1;
				mask_state[i + 4] = 1;
			}
		}

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			output = Vector4.Zero;
			for(int i = 0; i < m_CenterCount * m_CenterParamCount; i+= m_CenterParamCount)
			{
				var center = new Vector4{ X = a[i], Y = a[i + 1], Z = a[i + 2]};
				Spiral(a[i + 3], a[i + 4], ref center, ref input, ref output);
			}

			output /= m_CenterCount * 3;
		}

		private void Spiral(float k, float acc, ref Vector4 center, ref Vector4 input, ref Vector4 output)
		{
			var tmp = input - center;
			var dist = tmp.Length;

			for(int i = 0; i < 3; i++)
			{
				if(float.IsNaN(tmp.X) || float.IsNaN(tmp.Y))
					break;

				var x_n = k * tmp.Y;// + tmp.X;
				var y_n = -k * tmp.X;// + tmp.Y;
				var z_n = 1;

				var d = new Vector4(x_n, y_n, z_n, 0);
				d.Normalize();

				d *= 1f/Math.Max((float)Math.Sqrt(dist), 0.1f);
				output += acc * d;
				tmp = new Vector4(tmp.Z, tmp.X, tmp.Y,0);
				output = new Vector4(output .Z, output .X, output .Y,0);
			}
		}
	}

	/// <summary>
	/// dx/dt = y + x*(R - v_l)/v_l, dy/dt = -x + y*(R - v_l)/v_l, dz/dt = 1
	/// </summary>
	public class AngularMomentumMap : ChaoticMap
	{
		#region implemented abstract members of opentk.System3.ChaoticMap
		public override int ParamCount
		{
			get
			{
				return 24;
			}
		}
		#endregion

		public AngularMomentumMap () : base("AngularMomentumMap")
		{
			Map = Implementation;

			for(int i = 0; i < 6; i++)
			{
				mask_state[4*i] = mask_state[4*i + 1] = mask_state[4*i + 2] = 50;
				mask_state[4* i + 3] = 1;
			}
		}

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			output = Vector4.Zero;
			for(int i = 0; i < 6; i++)
			{
				var center = new Vector4{ X = a[4*i], Y = a[4*i + 1], Z = a[4* i + 2]};
				Moment(a[4* i + 3], ref center, ref input, ref output);
			}

			output /= 18;
		}

		private void Moment(float k, ref Vector4 center, ref Vector4 input, ref Vector4 output)
		{
			var tmp = center - input;
			var dist = tmp.Length;

			for(int i = 0; i < 3; i++)
			{
				var d = new Vector4(tmp.Y, - tmp.X, 0, 0) / tmp.LengthSquared ;
				output += d;
				tmp = new Vector4(tmp.Z, tmp.X, tmp.Y,0);
				output = new Vector4(output .Z, output .X, output .Y,0);
			}
		}
	}
	
	/// <summary>
	///
	/// </summary>
	public class DomainMorphMap : ChaoticMap
	{
		#region implemented abstract members of opentk.System3.ChaoticMap
		public override int ParamCount
		{
			get
			{
				return 0;
			}
		}
		#endregion

		public DomainMorphMap () : base("DomainMorphMap")
		{
			Map = Implementation;
		}

		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			//spiralovy posun sintrans
			Vector3 center = Vector3.UnitZ;
			var v = input.Xyz;//.zxy;
			
			for(int i = 1; i <= 3; i++)
			{
				var temp = v;
				//v = SinTrans(3, 0.25f, v, center, normalize(morph_rotate(v)));
				//v = SinTrans(3, 0.25f, v, morph_rotate(center), normalize(v));
				//v = SinTrans(0, 1, v, morph_rotate(center), normalize(morph_rotate(v)));
				//v = SinTrans(3, 0.25f, v, morph_rotate(center), normalize(morph_rotate(v)));
				//center = temp;
		
				//v = SinTrans(0, 1, v, center, normalize(v));
				//v = SinTrans(0, 1, v, center, normalize(morph_rotate(v))) -	morph_rotate(center) * 0.1f;
				//v = SinTrans(0, 1, v, center, normalize(v)) -	morph_rotate(v* 0.1f) ;
				//center = temp;
		
				//v = SinTrans(0, 1, v, center, normalize(v));
				//center = morph_rotate(temp);
				
				//v = morph_rotate(v, normalize(center));
				//v = morph_rotate(v, normalize(-center));
				v = SinTrans(3.0f/i, i, v, center, normalize(v));
				v = morph_rotate(v, normalize(center));
				center = temp;
			}
			
			//rovinna discretizace objektu
//			var center = new Vector3(1, 0, 0);
//			var v = input.Xyz;
//		
//			for(int i = 0; i < 3; i++)
//			{
//				v = SinTrans(1, 1, v, morph_rotate(center, new Vector4(0, 0, 1, 1)), normalize(morph_rotate(center)));
//			}
	
			output = new Vector4(v, 0);
		}
		
		private float sin(float f)
		{
			return (float)Math.Sin (f);
		}
		
		private float cos(float f)
		{
			return (float)Math.Cos(f);
		}
		
		private Vector3 normalize(Vector3 f)
		{
			f.Normalize();
			return f;
		}
		
		Vector3 morph_rotate(Vector3 pos)
		{
			float phi = pos.Y / Math.Max(new Vector2(pos.X, pos.Z).Length, 1);
	
			//matrices are specified in row-major order
	
			Matrix4 rotmatrix = 
			 new Matrix4(
					cos(phi), 0, -sin(phi), 0, 
					0,1, 0, 0,
					sin(phi), 0, cos(phi), 0,
					0, 0, 0, 0);
	
			return Vector4.Transform(new Vector4(pos, 0), rotmatrix).Xyz;
		}
		
		Vector3 morph_rotate(Vector3 pos, Vector3 axis)
		{
			//float phi = pos.Y / Math.Max(new Vector2(pos.X, pos.Z).Length, 1);
			float phi = Vector3.Dot(pos, axis)/100;// pos.Y / Math.Max(new Vector2(pos.X, pos.Z).Length, 1);
			return Vector3.Transform(pos, new Quaternion(axis, phi));
		}
		
		Vector3 morph_rotate(Vector3 pos, Vector4 axisangle)
		{
			return Vector3.Transform(pos, new Quaternion(axisangle.Xyz, axisangle.W));
		}
		
		Vector3 SinTrans(float amp, float octave, Vector3 v1, Vector3 center, Vector3 plane)
		{
			var k = amp * (float)Math.Sin(Vector3.Dot(v1 - center, plane) * octave)/octave * plane;
			return v1 + k;
		}
	}

	/// <summary>
	/// Swirl2 D map.
	/// </summary>
	public class Swirl2DMap : ChaoticMap
	{
		private int m_CenterCount = 30;
		private int m_CenterParamCount = 6;
		float m_MaxAcc = 100;
		float m_MaxAccBias = 1500;
		float m_MaxDistX = 50;
		float m_MaxDistY = 50;
		float m_MaxDistZ = 50;
		float m_Attenuation = 0.1f;
		float m_AttenuationBias = 1.7f;
		
		public float MaxAcc
		{
			get{ return m_MaxAcc;}
			set{ m_MaxAcc = value; UpdateMask(); }
		}
		
		public float Attenuation
		{
			get{ return m_Attenuation;}
			set{ m_Attenuation = value; UpdateMask(); }
		}
		
		public float AttenuationBias
		{
			get{ return m_AttenuationBias;}
			set{ m_AttenuationBias = value; UpdateMask(); }
		}
		
		public float MaxDistX
		{
			get{ return m_MaxDistX;}
			set{ m_MaxDistX = value; UpdateMask(); }
		}
		
		public float MaxDistY
		{
			get{ return m_MaxDistY;}
			set{ m_MaxDistY = value; UpdateMask(); }
		}
		
		public float MaxDistZ
		{
			get{ return m_MaxDistZ;}
			set{ m_MaxDistZ = value; UpdateMask(); }
		}
		
		public float MaxAccBias
		{
			get{ return m_MaxAccBias;}
			set{ m_MaxAccBias = value; UpdateMask(); }
		}
		
		#region implemented abstract members of opentk.System3.ChaoticMap
		public override int ParamCount
		{
			get
			{
				return m_CenterCount * m_CenterParamCount;
			}
		}
		#endregion
		
		public Swirl2DMap () : base("Swirl2DMap")
		{
			Map = Implementation;
			UpdateMask();			
		}
		
		private void UpdateMask()
		{
			for(int i = 0; i < m_CenterCount * m_CenterParamCount; i+= m_CenterParamCount)
			{
				mask_state[i] = MaxDistX;
				mask_state[i + 1] = MaxDistY;
				mask_state[i + 2] = MaxDistZ;
				
				mask_state[i + 3] = Attenuation;
				mask_state[i + 4] = MaxAcc;
				mask_state[i + 5] = 1;
				
				bias_state[i + 3] = AttenuationBias;
				bias_state[i + 4] = MaxAccBias;
			}
		}
		
		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			output = Vector4.Zero;
			for(int i = 0; i < m_CenterCount * m_CenterParamCount; i+= m_CenterParamCount)
			{
				var center = new Vector4{ X = a[i], Y = a[i + 1], Z = a[i + 2]};
				Spiral(a[i + 3], a[i + 4] * a[i + 5], ref center, ref input, ref output);
			}
			
			output /= m_CenterCount * 3;
		}
		
		private void Spiral(float k, float acc, ref Vector4 center, ref Vector4 input, ref Vector4 output)
		{
			var tmp = input - center;
			var dist = tmp.Xy.Length;
			
			if(dist < 0.1)
				return;
			
			var d = new Vector4(
				tmp.Y, 
				-tmp.X, 
				0, 
				0);
			d.Normalize();
			
			d *= 1f/(float)Math.Max(Math.Pow(dist, k), 0.1);
			output += acc * d;
		}
		
		public override void UpdateMap (DateTime simtime, long step)
		{
			for (int i = 0; i < a.Length; i++) {
				a[i] = target_state[i] * mask_state[i] + bias_state[i];
			}
			
			for(int i = 0; i < m_CenterCount * m_CenterParamCount; i += m_CenterParamCount)
			{
				var input = new Vector4 { X = a[i], Y = a[i + 1], Z = a[i + 2], W = 0 };
				var output = Vector4.Zero;
				Map(ref input, ref output);
				
				a[i] += output.X;
				a[i + 1] += output.Y;
				a[i + 2] += output.Z;
			}
			
			for (int i = 0; i < a.Length; i++) {
				target_state[i] = (a[i] - bias_state[i])/mask_state[i];
			}
		}
	}
	
	/// <summary>
	/// Swirl2 D map.
	/// </summary>
	public class Swirl3DMap : ChaoticMap
	{
		private int m_CenterCount = 10;
		private int m_CenterParamCount = 9;
		float m_MaxAcc = 50;
		float m_MaxAccBias = 150;
		float m_MaxDistX = 50;
		float m_MaxDistY = 50;
		float m_MaxDistZ = 50;
		float m_Attenuation = 0.1f;
		float m_AttenuationBias = 1.7f;
		
		public float MaxAcc
		{
			get{ return m_MaxAcc;}
			set{ m_MaxAcc = value; UpdateMask(); }
		}
		
		public float Attenuation
		{
			get{ return m_Attenuation;}
			set{ m_Attenuation = value; UpdateMask(); }
		}
		
		public float AttenuationBias
		{
			get{ return m_AttenuationBias;}
			set{ m_AttenuationBias = value; UpdateMask(); }
		}
		
		public float MaxDistX
		{
			get{ return m_MaxDistX;}
			set{ m_MaxDistX = value; UpdateMask(); }
		}
		
		public float MaxDistY
		{
			get{ return m_MaxDistY;}
			set{ m_MaxDistY = value; UpdateMask(); }
		}
		
		public float MaxDistZ
		{
			get{ return m_MaxDistZ;}
			set{ m_MaxDistZ = value; UpdateMask(); }
		}
		
		public float MaxAccBias
		{
			get{ return m_MaxAccBias;}
			set{ m_MaxAccBias = value; UpdateMask(); }
		}
		
		#region implemented abstract members of opentk.System3.ChaoticMap
		public override int ParamCount
		{
			get
			{
				return m_CenterCount * m_CenterParamCount;
			}
		}
		#endregion
		
		public Swirl3DMap () : base("Swirl3DMap")
		{
			Map = Implementation;
			UpdateMask();			
		}
		
		private void UpdateMask()
		{
			for(int i = 0; i < m_CenterCount * m_CenterParamCount; i += m_CenterParamCount)
			{
				mask_state[i] = MaxDistX;
				mask_state[i + 1] = MaxDistY;
				mask_state[i + 2] = MaxDistZ;
				
				mask_state[i + 3] = 1;
				mask_state[i + 4] = 1;
				mask_state[i + 5] = 1;
				
				mask_state[i + 6] = Attenuation;
				mask_state[i + 7] = MaxAcc;
				mask_state[i + 8] = 1;
				
				bias_state[i + 6] = AttenuationBias;
				bias_state[i + 7] = MaxAccBias;
			}
		}
		
		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			output = Vector4.Zero;
			for(int i = 0; i < m_CenterCount * m_CenterParamCount; i += m_CenterParamCount)
			{
				var center = new Vector3 { X = a[i], Y = a[i + 1], Z = a[i + 2] };
				var n = new Vector3 { X = a[i + 3], Y = a[i + 4], Z = a[i + 5] };					
				Spiral(a[i + 6], a[i + 7] * a[i + 8], ref center, ref n, ref input, ref output);
			}
			
			output /= m_CenterCount * 3;
		}
		
		private void Spiral(float k, float acc, ref Vector3 center, ref Vector3 n, ref Vector4 input, ref Vector4 output)
		{
			var tmp = input.Xyz - center;
			var dist = tmp.Length;
			
			if(dist < 0.1)
				return;
						
			var d = Vector3.Cross (tmp, n);
			d.Normalize ();
			
			d *= acc/(float)Math.Max(Math.Pow(dist, k), 0.1);
			output += new Vector4(d, 0);
		}
		
		public override void UpdateMap (DateTime simtime, long step)
		{
			for (int i = 0; i < a.Length; i++) {
				a[i] = target_state[i] * mask_state[i] + bias_state[i];
			}
			
			for(int i = 0; i < m_CenterCount * m_CenterParamCount; i += m_CenterParamCount)
			{
				var input = new Vector4 { X = a[i], Y = a[i + 1], Z = a[i + 2], W = 0 };
				var output = Vector4.Zero;
				Map(ref input, ref output);
				
				a[i] += output.X;
				a[i + 1] += output.Y;
				a[i + 2] += output.Z;
			}
			
			for (int i = 0; i < a.Length; i++) {
				target_state[i] = (a[i] - bias_state[i])/mask_state[i];
			}
		}		
	}
	
	/// <summary>
	/// 
	/// </summary>
	public class HopfMap : ChaoticMap
	{
		#region implemented abstract members of opentk.System3.ChaoticMap
		public override int ParamCount
		{
			get
			{
				return 2;
			}
		}
		#endregion
		
		public float A
		{
			get{ return mask_state[0];}
			set{ mask_state[0] = value;}
		}
		
		public float Abias
		{
			get{ return bias_state[0];}
			set{ bias_state[0] = value;}
		}
		
		public new float a
		{
			get{ return mask_state[1];}
			set{ mask_state[1] = value;}
		}
		
		public float abias
		{
			get{ return bias_state[1];}
			set{ bias_state[1] = value;}
		}
		
		public HopfMap () : base("HopfMap")
		{
			Map = Implementation;
		}
		
		private void Implementation (ref Vector4 input, ref Vector4 output)
		{
			var A = base.a[0];
			var a = base.a[1];
			var x = input.X;
			var y = input.Y;
			var z = input.Z;
			var k = A * (float)Math.Pow(1/input.LengthSquared, 2);
			
			output.X = k * 2 * (-a * y + x *z);
			output.Y = k * 2 * (a *x + y * z);
			output.Z = k * (a * a - x * x - y * y + z * z);
		}
	}
}

