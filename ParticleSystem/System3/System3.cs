using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using opentk.PropertyGridCustom;
using opentk.Scene.ParticleSystem;

namespace opentk.System3
{
	/// <summary>
	/// System3.
	/// </summary>
	public partial class System3 : ParticleSystemBase
	{
		private ChaoticMap m_ChaoticMap;
		private ISimulationScheme m_SimulationScheme;
		private IGenerationScheme m_GenerationScheme;
		private IParticleGenerator m_ParticleGenerator;
		private long m_Step;
		
		public BufferObjectSegmented<MetaInformation> MetaBuffer
		{
			get;
			private set;
		}

		public BufferObjectSegmented<MetaInformation> Meta
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
		[TypeConverter(typeof(IGenerationSchemeConverter))]
		[DescriptionAttribute("Expand to see the parameters of the GenerationScheme scheme.")]
		public IGenerationScheme GenerationScheme
		{
			get { return m_GenerationScheme; }
			set { DoPropertyChange (ref m_GenerationScheme, value, "GenerationScheme"); }
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
			var result = new System3 
			{
				PARTICLES_COUNT = 26000, 
				VIEWPORT_WIDTH = 324, 
				NEAR = 1, 
				FAR = 10240, 
				DT = 0.01,
				Fov = 0.9, 
				PublishMethod = PublishMethod.AllAtOnce,
				ParticleScaleFactor = 2200, 
				SimulationScheme = new ParticlesWithTrailsGpuSimulationScheme(),
				GenerationScheme = new ParticlesWithTrailsGenerationScheme(),
				ParticleGenerator = new GridGenerator { AmountX = 1, AmountY = 1, AmountZ = 0.001f, StepX = 0.1f, StepY = 0.1f },
				ChaoticMap = new LorenzMap()//new Swirl2DMap()
			};
			return result;
		}
		
		protected override void InitializeSystem ()
		{
			ChaoticMap = ChaoticMap ?? new LorenzMap ();
			SimulationScheme = SimulationScheme ?? new ParticlesWithTrails ();
			GenerationScheme = GenerationScheme ?? new ParticlesWithTrailsGenerationScheme ();
			ParticleGenerator = ParticleGenerator ?? new SimpleGenerator ();
			TrailSize = Math.Max (TrailSize, 1);
			
			MetaBuffer.Length = Position.Length;
			Meta = MetaBuffer;
			//Meta = MetaBuffer.Data = new MetaInformation[Position.Length];
			for (int i = 0; i < Position.Length; i++) {
				ParticleGenerator.MakeBubble (this, i, i);
			}
		}
		
		protected override void Publish (int start, int count)
		{
			if(count == PARTICLES_COUNT)
				MetaBuffer.Publish ();
			else
				MetaBuffer.PublishPart(start, count);
		}
		
		protected override void PrepareStateCore ()
		{
			unsafe
			{
				MetaBuffer = new BufferObjectSegmented<MetaInformation> (sizeof(MetaInformation), 0) { Name = "metadata_buffer", Usage = BufferUsageHint.DynamicDraw };
			}
		}
		
		protected override void Simulate (DateTime simulationTime)
		{
			ChaoticMap.UpdateMap (simulationTime, m_Step);
			GenerationScheme.Generate (this, simulationTime, m_Step);
			SimulationScheme.Simulate (this, simulationTime, m_Step);
			m_Step++;
		}
	}
}

