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
	public enum ColorSchemeType
	{
		Distance,
		Color
	}

	public enum SeedDistributionType
	{
		RegularGrid = 0x1,
		Random = 0x2,
		RandomInBox = 0x3
	}

	public partial class System3 : ParticleSystemBase
	{
		private ChaoticMap m_ChaoticMap;

		private ISimulationScheme m_SimulationScheme;

		private IParticleGenerator m_ParticleGenerator;

		public MetaInformation[] Meta
		{
			get;
			private set;
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

		public bool SingleStepSimulation
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

		[Category("Map properties")]
		[TypeConverter(typeof(ISimulationSchemeConverter))]
		[DescriptionAttribute("Expand to see the parameters of the simulation scheme.")]
		public ISimulationScheme SimulationScheme
		{
			get { return m_SimulationScheme; }
			set { DoPropertyChange (ref m_SimulationScheme, value, "SimulationScheme"); }
		}

		[Category("Map properties")]
		[TypeConverter(typeof(IParticleGeneratorConverter))]
		[DescriptionAttribute("Expand to see the parameters of the ParticleGenerator.")]
		public IParticleGenerator ParticleGenerator
		{
			get { return m_ParticleGenerator; }
			set { DoPropertyChange (ref m_ParticleGenerator, value, "ParticleGenerator"); }
		}

		protected override ParticleSystem GetInstanceInternal (GameWindow win)
		{
			var result = new System3 {
				PARTICLES_COUNT = 60000, VIEWPORT_WIDTH = 324, NEAR = 1, FAR = 10240, DT = 0.0051,
				Fov = 0.9, PublishMethod = PublishMethod.AllAtOnce,
				ParticleScaleFactor = 600, 
				SimulationScheme = new SingleStepScheme(), ChaoticMap = new DomainMorphMap(), ParticleGenerator = new SimpleGenerator{ SeedDistribution = SeedDistributionType.RegularGrid} };
			return result;
		}
		
		protected override void PrepareStateCore ()
		{
			;
		}
	}
}

