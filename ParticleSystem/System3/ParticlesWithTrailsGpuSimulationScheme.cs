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
using opentk.ShadingSetup;

namespace opentk.System3
{
	/// <summary>
	/// Particles with trails.
	/// </summary>
	public class ParticlesWithTrailsGpuSimulationScheme: ISimulationScheme
	{
		/// <summary>
		/// Map mode type.
		/// </summary>
		public enum MapModeType
		{
			D = 0x3,
			ForceField = 0x4
		}

		public enum IntegrationStepType
		{
			LimitDelta = 0x1,
			DoNotLimit = 0x0
		}

		public enum InterpolationType
		{
			Cubic,
			Linear
		}

		/// <summary>
		/// Compute metadata.
		/// </summary>
		public enum ComputeMetadata
		{
			Tangent,
			Speed,
		}

		public MapModeType MapMode {
			get;
			set;
		}

		public IntegrationStepType IntegrationStep {
			get;
			set;
		}

		public InterpolationType Interpolation {
			get;
			set;
		}

		public int TrailBundleSize {
			get;
			set;
		}

		public ComputeMetadata ComputeMetadataMode {
			get;
			set;
		}

		string ISimulationScheme.Name {
			get {
				return "ParticlesWithTrails (Gpu)";
			}
		}
		
		private State m_State;
		private BufferObject<float> m_Parameters;

		void ISimulationScheme.Simulate (System3 system, DateTime simulationTime, long simulationStep)
		{
			var trailBundleSize = Math.Max(TrailBundleSize, 1);
			
			if (m_State == null) {
				//create program from resources filtered by namespace and name
				var program = new Program ("SimulationScheme")
				{
					RenderPass.GetShaders ("System3", "ParticlesWithTrails"),
					RenderPass.GetShaders ("System3", "PWTSub")
				};
				m_Parameters = new BufferObjectCompact<float>(sizeof(float), 0) { Name = "map_parameters_buffer", Usage = BufferUsageHint.DynamicDraw };
				
				m_State = 
					new State (null)
					{
					  program,
					  new ShaderStorageSet
					  {
							{"Position", system.PositionBuffer},
						  {"Rotation", system.RotationBuffer},
						  {"MapParameters", m_Parameters},
						  {"Dimension", system.DimensionBuffer},
						  {"Attribute1", system.ColorBuffer},
						  {"Meta", system.MetaBuffer},
					  },
					  new UniformState
					  {
						  {"u_Dt", () => (float)system.DT },
						  {"u_TrailSize", () => system.TrailSize },
						  {"u_TrailBundleSize", () => TrailBundleSize },
						  {"u_StepsPerFrame", () => system.StepsPerFrame },
						  {"u_ParticleScale", () => (float)system.ParticleScaleFactor },
						  {"u_Map", (ShaderType)ShaderTypeExt.ComputeShader, () => system.ChaoticMap.Name },
					  }
					};
			}
			
			system.MetaBuffer.Publish();
			system.PositionBuffer.Publish();
			system.DimensionBuffer.Publish();
			system.RotationBuffer.Publish();
			system.ColorBuffer.Publish();
			system.RotationLocalBuffer.Publish();
			
			m_Parameters.Length = system.ChaoticMap.a.Length;
			m_Parameters.CopyFrom(system.ChaoticMap.a, 0);
			m_Parameters.Publish();
			
			m_State.Activate ();
			GLExtensions.DispatchCompute (system.PARTICLES_COUNT/(8 * trailBundleSize)  + 1, 1, 1);
			
			//system.MetaBuffer.Readout();
			//system.PositionBuffer.Readout();
			//system.RotationBuffer.Readout();
			//system.DimensionBuffer.Readout();
			//system.ColorBuffer.Readout();
		}
	}
}


