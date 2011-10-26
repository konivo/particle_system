using System;
using OpenTK;
using OpenTK.Graphics;
using System.ComponentModel;
using System.ComponentModel.Composition;
using OpenTK.Graphics.OpenGL;
using opentk.PropertyGridCustom;
using opentk.Scene.ParticleSystem;

namespace opentk.System3
{
	public partial class System3 : ParticleSystemBase
	{
		public enum ColorSchemeType
		{
			Distance,
			Color
		}

		public enum SeedDistributionType
		{
			RegularGrid = 0x1,
			Random = 0x2
		}

		public enum MapModeType
		{
			SingleStep = 0x1,
			Iterated = 0x2,
			Timedomain = 0x3
		}

		public int TrailSize
		{
			get;
			set;
		}

		public int StepsPerFrame
		{
			get;
			set;
		}

		public MapModeType MapMode
		{
			get;
			set;
		}

		public bool SingleStepSimulation
		{
			get;
			set;
		}

		public SeedDistributionType SeedDistribution 
		{
			get;
			set;
		}

		public ColorSchemeType ColorScheme
		{
			get;
			set;
		}

		[Category("Map properties")]
		[TypeConverter(typeof(ChaoticMapConverter))]
		[DescriptionAttribute("Expand to see the parameters of the map.")]
		public ChaoticMap ChaoticMap
		{
			get { return m_ChaoticMap; }
			set { DoPropertyChange (ref m_ChaoticMap, value, "ChaoticMap"); }
		}

		protected override ParticleSystem GetInstanceInternal (GameWindow win)
		{
			var result = new System3 {
				PARTICLES_COUNT = 6000, VIEWPORT_WIDTH = 324, NEAR = 1, FAR = 10240, DT = 0.0001,
				Fov = 0.9,
				SeedDistribution = System3.SeedDistributionType.RegularGrid,
				ParticleScaleFactor = 6600, MapMode = System3.MapModeType.Timedomain,};
			return result;
		}
		
		protected override void PrepareStateCore ()
		{
			;
		}
	}
}

