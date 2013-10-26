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
	/// Generates particles randomly in a box of specified dimensions
	/// </summary>
	public class SphereGenerator: SimpleGenerator, IParticleGenerator
	{
		public float SphereInnerSize
		{
			get; 
			set;
		}
		
		public float SphereOuterSize
		{
			get; 
			set;
		}
		
		public new string Name
		{
			get
			{
				return "SphereGenerator";
			}
		}
		
		public SphereGenerator()
		{
			SphereInnerSize = 50;
			SphereOuterSize = 75;
		}
		
		protected override Vector3 NewPosition(System3 system, int bundleFirstItem, int i)
		{
			var dir = (Vector3) MathHelper2.RandomVector3(1);
			var l = dir.Length;
			var k = (float) MathHelper2.GetThreadLocalRandom().NextDouble();			
			return (dir * SphereOuterSize * k + dir * SphereInnerSize * (1 - k)) / l;
		}
	}
}

