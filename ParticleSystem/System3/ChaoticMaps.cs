using System;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.GridRenderPass;
using opentk.Manipulators;
using System.ComponentModel;

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
		public Func<Vector3d, Vector3d> Map
		{
			get;
			protected set;
		}

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

		private Vector3d Implementation (Vector3d input)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;
			
			var x_n = Sigma * (y_p - x_p);
			var y_n = x_p * (Rho - z_p) - y_p;
			var z_n = x_p * y_p - z_p * Beta;
			
			return new Vector3d (x_n, y_n, z_n);
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

		private Vector3d Implementation (Vector3d input)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;

			var z_n = Math.Sin (x_p);
			var x_n = Math.Sin (A * y_p) - z_p * Math.Cos (B * x_p);
			var y_n = z_p * Math.Sin (C * x_p) - Math.Cos (D * y_p);

			return new Vector3d (x_n, y_n, z_n);
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

		private Vector3d Implementation (Vector3d input)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;

			var x_n = A + y_p - z_p * y_p;
			var y_n = B + z_p - z_p * x_p;
			var z_n = C + x_p - y_p * x_p;

			return new Vector3d (x_n, y_n, z_n);
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

		private Vector3d Implementation (Vector3d input)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;

			var x_n = A * (y_p - PhiFunction (x_p));
			var y_n = x_p - y_p + z_p;
			var z_n = -B * y_p - C * z_p;

			return new Vector3d (x_n, y_n, z_n);
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

		private Vector3d Implementation (Vector3d input)
		{
			var x_p = input.X;
			var y_p = input.Y;
			var z_p = input.Z;

			var x_n = A * Math.Cos(z_p - y_p);
			var y_n = B * Math.Sin(x_p - z_p);
			var z_n = C * Math.Cos(y_p - x_p);

			return new Vector3d (x_n, y_n, z_n);
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

		private Vector3d Implementation (Vector3d input)
		{
			var x_n = input.Y;
			var y_n = -input.X + input.Y* input.Z;
			var z_n = 1 - Math.Pow(input.Y, 2.0);

			return new Vector3d (x_n, y_n, z_n);
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

		double sphere_sdb(Vector4d sphere, Vector3d pos)
		{
			return (pos - sphere.Xyz).Length - sphere.W;
		}

		//
		Vector3d sphere_sdb_grad(Vector4d sphere, Vector3d pos)
		{
			pos = pos - sphere.Xyz;
			pos.Normalize();
			return pos;
		}

		//
		double torus_sdb(double r1, double r2, Vector3d pos)
		{
			double d1 = (pos.Xy.Length - r1);
			d1 = Math.Sqrt(d1*d1 + pos.Z*pos.Z) - r2;

			return d1;
		}

		//
		Vector3d DomainMorphFunction(Vector3d pos)
		{
			var v1 = new Vector3d(
				torus_sdb(40,  30, pos),
				torus_sdb(40, 30, new Vector3d(pos.Y, pos.X, pos.Z)),
				torus_sdb(50, 20, new Vector3d(pos.Z, pos.Y, pos.X)));

			var v2 = new Vector3d(
				torus_sdb(60,  40, v1),
				torus_sdb(60, 40, new Vector3d(v1.Z, v1.X, v1.Y)),
				torus_sdb(70, 50, new Vector3d(v1.Z, v1.Y, v1.X)));

			var v3 = new Vector3d(
				torus_sdb(60,  60, v2 - new Vector3d(5, 10, 11)),
				torus_sdb(60, 60, new Vector3d(v2.Z, v2.X, v2.Y)  - new Vector3d(-5, 10, 0)),
				torus_sdb(60, 60, new Vector3d(v2.Z, v2.Y, v2.X) - new Vector3d(5, 0, -11)));

			return v3;
		}

		//
		double SDBValue(Vector3d pos)
		{
			Vector3d mpos = DomainMorphFunction(pos);
			//return torus_sdb(50, 10, mpos) * 0.2;
			return sphere_sdb(new Vector4d(0, 0, 0, 28), mpos) * 0.2;
		}

		//s
		private Vector3d Implementation (Vector3d input)
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

		private Vector3d Implementation (Vector3d input)
		{
			var v_l = input.Length;
			var k = (R - v_l);

			var x_n = input.Y + k * input.X;
			var y_n = -input.X + k * input.Y;
			var z_n = 0.3;

			return new Vector3d (x_n, y_n, z_n);
		}
	}


}

