using System;
using System.Linq;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using opentk.GridRenderPass;
using opentk.Manipulators;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;

namespace opentk.System3
{
	/// <summary>
	///
	/// </summary>
	[InheritedExport]
	public interface IParticleGenerator
	{
		string Name	{	get; }

		void NewBundle(System3 system, int bundleFirstItem);

		void MakeBubble (System3 system, int bundleFirstItem, int i);

		float UpdateSize(System3 system, int bundleFirstItem, int i);
	}

	/// <summary>
	/// Particles with trails.
	/// </summary>
	public class SimpleGenerator2: SimpleGenerator
	{
	}

	/// <summary>
	/// Particles with trails.
	/// </summary>
	public class SimpleGenerator: IParticleGenerator
	{
		string IParticleGenerator.Name
		{
			get
			{
				return "SimpleGenerator";
			}
		}

		public SeedDistributionType SeedDistribution
		{
			get;
			set;
		}

		public int LifeLengthMin
		{
			get;
			set;
		}

		public int LifeLengthMax
		{
			get;
			set;
		}

		public float SizeScaleRatio
		{
			get;
			set;
		}

		public float ScaleRatioMaxDifference
		{
			get;
			set;
		}

		public float SizeScalePower
		{
			get;
			set;
		}

		public float SizeRandomness
		{
			get;
			set;
		}

		public bool PreferStableOrbits
		{
			get;
			set;
		}

		public float StableOrbitsLMin
		{
			get; set;
		}

		public float StableOrbitsLMax
		{
			get; set;
		}

		public int StableOrbitsCount
		{
			get; set;
		}

		public float StableOrbitsMaxDist
		{
			get; set;
		}

		public SimpleGenerator ()
		{
			SizeRandomness = 0;
			SizeScalePower = 1;
			SizeScaleRatio = 0.001f;
			LifeLengthMin = LifeLengthMax = 1000;
			ScaleRatioMaxDifference = 0.01f;
			PreferStableOrbits = true;
			StableOrbitsLMax = 0;
			StableOrbitsLMin = 0;
			StableOrbitsCount = 20;
			StableOrbitsMaxDist = 2000;
		}

		public void NewBundle(System3 system, int bundleFirstItem)
		{
			var Meta = system.Meta;

			Meta[bundleFirstItem] = new MetaInformation {
				Size = (float)Math.Pow (MathHelper2.GetThreadLocalRandom().NextDouble (), SizeScalePower) * SizeScaleRatio,
				LifeLen = MathHelper2.GetThreadLocalRandom().Next (LifeLengthMin, LifeLengthMax),
				Leader = Meta[bundleFirstItem].Leader };

			MakeBubble (system, bundleFirstItem, Meta[bundleFirstItem].Leader + bundleFirstItem);
			return;
		}

		public void MakeBubble (System3 system, int bundleFirstItem, int i)
		{
			var Position = system.Position;
			var Dimension = system.Dimension;
			var Rotation = system.Rotation;
			var Color = system.Color;

			if(PreferStableOrbits)
			{
				var map = system.ChaoticMap;
				var lp = map.LyapunovExponent(Position[i], StableOrbitsCount);

				if(lp.Item1 > StableOrbitsLMin && lp.Item1 < StableOrbitsLMax)
					return;

				if(lp.Item2.Length < StableOrbitsMaxDist)
					return;
			}

			var size = UpdateSize(system, bundleFirstItem, i);
			var newpos = (Vector3)MathHelper2.RandomVector3 (12);

			if(SeedDistribution == SeedDistributionType.RegularGrid)
			{
				var gridStepSize = 0.1f;
				var gridLongStepSize = 5 * 2.5f;
				var gridCount = (int)Math.Floor(Math.Pow(Position.Length, 1/3.0) * 25);
				var gridCount2 = (int)Math.Ceiling(Math.Pow(Position.Length, 1/3.0) / 5);
				var gridOrigin = - (gridCount * gridStepSize) / 2;

				var gc2 = gridCount * gridCount2;

				int ix = i / gc2;
				int iy = (i - gc2 * ix) / gridCount;
				int iz = i - gc2 * ix - iy * gridCount;

				newpos = Vector3.Multiply(new Vector3(ix, iy, iz), new Vector3(gridLongStepSize, gridLongStepSize, gridStepSize)) +
					new Vector3(gridOrigin, gridOrigin, gridOrigin);
			}

			Dimension[i] = new Vector4 (size, size, size, size);
			Position[i] = new Vector4 (newpos.X, newpos.Y, newpos.Z, 1);
			Color[i] = new Vector4 (0, 1, 0, 1);
			Rotation[i] = Matrix4.Identity;
		}

		public float UpdateSize(System3 system, int bundleFirstItem, int i)
		{
			var Meta = system.Meta;
			var r = (float)MathHelper2.GetThreadLocalRandom().NextDouble ();
			return Math.Max((1 - SizeRandomness * r)  * Meta[bundleFirstItem].Size, ScaleRatioMaxDifference * SizeScaleRatio);
		}
	}
}