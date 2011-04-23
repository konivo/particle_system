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

}

