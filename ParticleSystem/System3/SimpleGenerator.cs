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
	/// Particles with trails.
	/// </summary>
	public class SimpleGenerator: IParticleGenerator
	{
		public string Name
		{
			get
			{
				return "SimpleGenerator";
			}
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
			get;
			set;
		}
	
		public float StableOrbitsLMax
		{
			get;
			set;
		}
	
		public int StableOrbitsCount
		{
			get;
			set;
		}
	
		public float StableOrbitsMaxDist
		{
			get;
			set;
		}
	
		public SimpleGenerator()
		{
			SizeRandomness = 0;
			SizeScalePower = 1;
			SizeScaleRatio = 0.001f;
			LifeLengthMax = 100;
			LifeLengthMin = 90;
			ScaleRatioMaxDifference = 0.01f;
			PreferStableOrbits = false;
			StableOrbitsLMax = 0;
			StableOrbitsLMin = 0;
			StableOrbitsCount = 20;
			StableOrbitsMaxDist = 2000;
		}
	
		public void NewBundle(System3 system, int bundleFirstItem)
		{
			var Meta = system.Meta;
		
			Meta [bundleFirstItem] = new MetaInformation {
			Size = (float)Math.Pow (MathHelper2.GetThreadLocalRandom().NextDouble (), SizeScalePower) * SizeScaleRatio,
			LifeLen = MathHelper2.GetThreadLocalRandom().Next (LifeLengthMin, LifeLengthMax),
			Leader = Meta[bundleFirstItem].Leader };
		
			MakeBubble(system, bundleFirstItem, Meta [bundleFirstItem].Leader + bundleFirstItem);
			return;
		}
			
		public void MakeBubble(System3 system, int bundleFirstItem, int i)
		{
			var Position = system.Position;
			var Dimension = system.Dimension;
			var Rotation = system.Rotation;
			var Color = system.Color;
		
			if (PreferStableOrbits)
			{
				var map = system.ChaoticMap;
				var lp = map.LyapunovExponent(Position [i], StableOrbitsCount);
			
				if (lp.Item1 > StableOrbitsLMin && lp.Item1 < StableOrbitsLMax)
					return;
			
				if (lp.Item2.Length < StableOrbitsMaxDist)
					return;
			}
		
			var size = UpdateSize(system, bundleFirstItem, i);
			var newpos = NewPosition(system, bundleFirstItem, i);
		
			Dimension [i] = new Vector4(size, size, size, size);
			Position [i] = new Vector4(newpos.X, newpos.Y, newpos.Z, 1);
			Color [i] = new Vector4(0, 1, 0, 1);
			Rotation [i] = Matrix4.Identity;
		}
	
		public float UpdateSize(System3 system, int bundleFirstItem, int i)
		{
			var Meta = system.Meta;
			var r = (float)MathHelper2.GetThreadLocalRandom().NextDouble();
			return Math.Max((1 - SizeRandomness * r) * Meta [bundleFirstItem].Size, ScaleRatioMaxDifference * SizeScaleRatio);
		}
		
		protected virtual Vector3 NewPosition(System3 system, int bundleFirstItem, int i)
		{
			return (Vector3)MathHelper2.RandomVector3(12);
		}
	}
}