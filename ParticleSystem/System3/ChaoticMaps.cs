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
		public Func<Vector4d, Vector4d> Map
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
	}

	/// <summary>
	///
	/// </summary>
	public class LorenzMap : ChaoticMap
	{
		[DefaultValue(10.0)]
		public double Sigma
		{
			get;
			set;
		}
		[DefaultValue(28.0)]
		public double Rho
		{
			get;
			set;
		}
		[DefaultValue(2.6)]
		public double Beta
		{
			get;
			set;
		}

		public LorenzMap () : base("Lorenz")
		{
			Sigma = 10;
			Rho = 28;
			Beta = 2.5;
			Map = Implementation;
		}

		private Vector4d Implementation (Vector4d input)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;
			
			var x_n = Sigma * (y_p - x_p);
			var y_n = x_p * (Rho - z_p) - y_p;
			var z_n = x_p * y_p - z_p * Beta;
			
			return new Vector4d (x_n, y_n, z_n, 0);
		}
	}

	/// <summary>
	///
	/// </summary>
	public class PickoverMap : ChaoticMap
	{
		public double A
		{
			get;
			set;
		}
		public double B
		{
			get;
			set;
		}
		public double C
		{
			get;
			set;
		}
		public double D
		{
			get;
			set;
		}

		public PickoverMap () : base("Pickover")
		{
			A = 1.425;
			B = 1.24354;
			C = 1.02435342;
			D = 1.473503;
			Map = Implementation;
		}

		private Vector4d Implementation (Vector4d input)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;

			var z_n = Math.Sin (x_p);
			var x_n = Math.Sin (A * y_p) - z_p * Math.Cos (B * x_p);
			var y_n = z_p * Math.Sin (C * x_p) - Math.Cos (D * y_p);

			return new Vector4d (x_n, y_n, z_n, 0);
		}
	}

	/// <summary>
	///
	/// </summary>
	public class PolynomialMap : ChaoticMap
	{
		public double A
		{
			get;
			set;
		}
		public double B
		{
			get;
			set;
		}
		public double C
		{
			get;
			set;
		}

		public PolynomialMap () : base("Polynomial")
		{
			A = 1.425;
			B = 1.24354;
			C = 1.02435342;
			Map = Implementation;
		}

		private Vector4d Implementation (Vector4d input)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;

			var x_n = A + y_p - z_p * y_p;
			var y_n = B + z_p - z_p * x_p;
			var z_n = C + x_p - y_p * x_p;

			return new Vector4d (x_n, y_n, z_n, 0);
		}
	}

	/// <summary>
	///
	/// </summary>
	public class ChuaMap : ChaoticMap
	{
		public double A
		{
			get;
			set;
		}
		public double B
		{
			get;
			set;
		}
		public double C
		{
			get;
			set;
		}

		public ChuaMap () : base("Chua")
		{
			A = 1.425;
			B = 1.24354;
			C = 1.02435342;
			Map = Implementation;
		}

		private Vector4d Implementation (Vector4d input)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;

			var x_n = A * (y_p - PhiFunction (x_p));
			var y_n = x_p - y_p + z_p;
			var z_n = -B * y_p - C * z_p;

			return new Vector4d (x_n, y_n, z_n, 0);
		}

		private double PhiFunction (double x)
		{
			return 1 / 16.0 * Math.Pow (x, 3) - 1 / 6.0 * x;
		}
	}

	/// <summary>
	///
	/// </summary>
	public class OwenMareshMap : ChaoticMap
	{
		public double A
		{
			get;
			set;
		}
		public double B
		{
			get;
			set;
		}
		public double C
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

		private Vector4d Implementation (Vector4d input)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;

			var x_n = A * Math.Cos(z_p - y_p);
			var y_n = B * Math.Sin(x_p - z_p);
			var z_n = C * Math.Cos(y_p - x_p);

			return new Vector4d (x_n, y_n, z_n, 0);
		}

		private double PhiFunction (double x)
		{
			return 1 / 16.0 * Math.Pow (x, 3) - 1 / 6.0 * x;
		}
	}

	/// <summary>
	///
	/// </summary>
	public class CustomOwenMareshMap : ChaoticMap
	{
		public double A
		{
			get;
			set;
		}
		public double B
		{
			get;
			set;
		}
		public double C
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

		private Vector4d Implementation (Vector4d input)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;

			var x_n = A * Math.Cos(y_p + z_p);
			var y_n = B * Math.Sin(x_p + z_p);
			var z_n = C * Math.Cos(x_p + y_p);

			return new Vector4d (x_n, y_n, z_n, 0);
		}

		private double PhiFunction (double x)
		{
			return 1 / 16.0 * Math.Pow (x, 3) - 1 / 6.0 * x;
		}
	}

	/// <summary>
	///
	/// </summary>
	public class _4DSpringMap : ChaoticMap
	{
		public double a1 { get; set;}
		public double a2{ get; set;}
		public double a3{ get; set;}
		public double a4{ get; set;}
		public double a5{ get; set;}
		public double a6{ get; set;}
		public double a7{ get; set;}
		public double a8{ get; set;}
		public double a9{ get; set;}
		public double a10 { get; set;}
		public double a11 { get; set;}
		public double a12{ get; set;}
		public double a13{ get; set;}
		public double a14{ get; set;}
		public double a15{ get; set;}
		public double a16{ get; set;}
		public double a17{ get; set;}
		public double a18{ get; set;}
		public double a19{ get; set;}
		public double a20 { get; set;}
		public double a21 { get; set;}
		public double a22{ get; set;}
		public double a23{ get; set;}
		public double a24{ get; set;}
		public double a25{ get; set;}
		public double a26{ get; set;}
		public double a27{ get; set;}
		public double a28{ get; set;}
		public double a29{ get; set;}
		public double a30 { get; set;}
		public double a31 { get; set;}
		public double a32{ get; set;}
		public double a33{ get; set;}
		public double a34{ get; set;}
		public double a35{ get; set;}
		public double a36{ get; set;}
		public double a37{ get; set;}
		public double a38{ get; set;}
		public double a39{ get; set;}
		public double a40 { get; set;}
		public double a41 { get; set;}
		public double a42{ get; set;}
		public double a43{ get; set;}
		public double a44{ get; set;}
		public double a45{ get; set;}
		public double a46{ get; set;}
		public double a47{ get; set;}
		public double a48{ get; set;}
		public double a49{ get; set;}
		public double a50 { get; set;}
		public double a51 { get; set;}
		public double a52{ get; set;}
		public double a53{ get; set;}
		public double a54{ get; set;}
		public double a55{ get; set;}
		public double a56{ get; set;}
		public double a57{ get; set;}
		public double a58{ get; set;}
		public double a59{ get; set;}
		public double a60{ get; set;}
		public double K1 {get; set;}
		public double K2 {get; set;}
		public double K0 {get; set;}

		private long m_test;
		private long m_lastrun;


		public _4DSpringMap () : base("_4DSpringMap")
		{
			InitializeParams();
			K1 = 0.1;
			K2 = 0.01;
			K0 = 0.01;
			Map = Implementation;
		}

		private void InitializeParams()
		{
			a1 += K0 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a2 += K1 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a3 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a4 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a5 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a6 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a7 += K1 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a8 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a9 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a10 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a11 += K1 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a12 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a13 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a14 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a15 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a16 += K0 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a17 += K1 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a18 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a19 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a20 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a21 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a22 += K1 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a23 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a24 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a25 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a26 += K1 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a27 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a28 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a29 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a30 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a31 += K0 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a32 += K1 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a33 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a34 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a35 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a36 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a37 += K1 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a38 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a39 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a40 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a41 += K1 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a42 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a43 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a44 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a45 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a46 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a47 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a48 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
			a49 += K2 * (MathHelper2.GetThreadLocalRandom().NextDouble() - 0.5);
		}

		public override void UpdateMap (DateTime simtime, long step)
		{
			if(m_lastrun + 1 << 10 < step )
			{
				InitializeParams();
				m_lastrun = step;
			}
		}

		private Vector4d Implementation (Vector4d input)
		{
			var X = input.X;
			var Y = input.Y;
			var Z = input.Z;
			var W = input.W;

			var X2 = Math.Pow(X, 2);
			var Y2 = Math.Pow(Y, 2);
			var Z2 = Math.Pow(Z, 2);
			var W2 = Math.Pow(W, 2);
			/*
			X = X + 0.1a1Y
	    Y = Y + 0.1(a2X + a3X3 + a4X2Y + a5XY2 + a6Y + a7Y3 + a8sin Z
		  Z = [Z + 0.1(a9 + 1.3)] mod 2π
		  W = (N - 1000) / (NMAX - 1000)
		  */

		  var x_n = a1 + a2 * X + a3 * X2 + a4 * X * Y + a5 * X* Z + a6 * X* W + a7* Y + a8* Y2 + a9* Y* Z + a10 * Y* W + a11* Z + a12* Z2 +	a13* Z* W + a14* W + a15* W2;
			var y_n = a16 + a17* X + a18* X2 + a19* X* Y + a20* X* Z + a21* X* W + a22* Y + a23* Y2 + a24* Y* Z + a25* Y* W + a26* Z + a27* Z2 + a28* Z* W + a29* W + a30* W2;

			var z_n = a31 + a32* X + a33* X2 + a34* X* Y + a35* X* Z + a36* X* W + a37* Y + a38* Y2 + a39* Y* Z + a40* Y* W + a41* Z + a42* Z2 + a43* Z* W + a44* W + a45* W2;
			var w_n = a46 + a47* X + a48* X2 + a49* X* Y + a50* X* Z + a51* X* W + a52* Y + a53* Y2 + a54* Y* Z + a55* Y* W + a56* Z + a57* Z2 + a58* Z* W + a59* W + a60* W2;

			return new Vector4d (x_n, y_n, z_n, w_n);
		}

		private double PhiFunction (double x)
		{
			return 1 / 16.0 * Math.Pow (x, 3) - 1 / 6.0 * x;
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

		private Vector4d Implementation (Vector4d input)
		{
			var x_n = input.Y;
			var y_n = -input.X + input.Y* input.Z;
			var z_n = 1 - Math.Pow(input.Y, 2.0);

			return new Vector4d (x_n, y_n, z_n, 0);
		}
	}

	/// <summary>
	/// dx/dt = y, dy/dt = -x + yz, dz/dt = 1 - y2
	/// </summary>
	public class TestMap : ChaoticMap
	{
		public TestMap () : base("TestMap")
		{
			Map = Implementation;
		}

		double sphere_sdb(Vector4d sphere, Vector4d pos)
		{
			return (pos.Xyz - sphere.Xyz).Length - sphere.W;
		}

		//
		Vector3d sphere_sdb_grad(Vector4d sphere, Vector3d pos)
		{
			pos = pos - sphere.Xyz;
			pos.Normalize();
			return pos;
		}

		//
		double torus_sdb(double r1, double r2, Vector4d pos)
		{
			double d1 = (pos.Xy.Length - r1);
			d1 = Math.Sqrt(d1*d1 + pos.Z*pos.Z) - r2;

			return d1;
		}

		//
		Vector4d DomainMorphFunction(Vector4d pos)
		{
			var v1 = new Vector4d(
				torus_sdb(40,  30, pos),
				torus_sdb(40, 30, new Vector4d(pos.Y, pos.X, pos.Z, 0)),
				torus_sdb(50, 20, new Vector4d(pos.Z, pos.Y, pos.X, 0)), 0);

			var v2 = new Vector4d(
				torus_sdb(60,  40, v1),
				torus_sdb(60, 40, new Vector4d(v1.Z, v1.X, v1.Y, 0)),
				torus_sdb(70, 50, new Vector4d(v1.Z, v1.Y, v1.X, 0)), 0);

			var v3 = new Vector4d(
				torus_sdb(60,  60, v2 - new Vector4d(5, 10, 11, 0)),
				torus_sdb(60, 60, new Vector4d(v2.Z, v2.X, v2.Y, 0)  - new Vector4d(-5, 10, 0, 0)),
				torus_sdb(60, 60, new Vector4d(v2.Z, v2.Y, v2.X, 0) - new Vector4d(5, 0, -11, 0)), 0);

			return v3;
		}

		//
		double SDBValue(Vector4d pos)
		{
			var mpos = DomainMorphFunction(pos);
			//return torus_sdb(50, 10, mpos) * 0.2;
			return sphere_sdb(new Vector4d(0, 0, 0, 28), mpos) * 0.2;
		}

		//s
		private Vector4d Implementation (Vector4d input)
		{
			return DomainMorphFunction(input);
		}
	}

	/// <summary>
	/// dx/dt = y + x*(R - v_l)/v_l, dy/dt = -x + y*(R - v_l)/v_l, dz/dt = 1
	/// </summary>
	public class TubularMap : ChaoticMap
	{
		public double R
		{
			get;
			set;
		}

		public TubularMap () : base("TubularMap")
		{
			Map = Implementation;
		}

		private Vector4d Implementation (Vector4d input)
		{
			var v_l = input.Length;
			var K2 = (R - v_l);

			var x_n = input.Y + K2 * input.X;
			var y_n = -input.X + K2 * input.Y;
			var z_n = 0.3;

			return new Vector4d (x_n, y_n, z_n, 0);
		}
	}


}

