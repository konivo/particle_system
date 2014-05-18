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
	public class ParticlesWithTrailsGenerationScheme: IGenerationScheme
	{
		string IGenerationScheme.Name 
		{
			get {
				return "ParticlesWithTrails";
			}
		}

		void IGenerationScheme.Generate (System3 system, DateTime simulationTime, long simulationStep)
		{
			var trailSize = Math.Max (system.TrailSize, 1);
			var trailCount = (system.Position.Length + trailSize - 1) / trailSize;
			var trailBundleSize = Math.Max (system.SimulationScheme is ParticlesWithTrails ? ((ParticlesWithTrails)system.SimulationScheme).TrailBundleSize : 1, 1);
			var trailBundleCount = (trailCount + trailBundleSize - 1) / trailBundleSize;

			var fun = system.ChaoticMap.Map;
			var position = system.Position;
			var dimension = system.Dimension;
			var meta = system.Meta;
			var rotation = system.Rotation;
			var attribute1 = system.Color;

			var particleCount = position.Length;
			var particleScale = system.ParticleScaleFactor;
			var dt = (float)system.DT;

			for (int bundleIndex = 0; bundleIndex < trailBundleCount; bundleIndex++) 
			{
				var firsttrail = bundleIndex * trailBundleSize * trailSize;
				var lasttrail = Math.Min (firsttrail + trailBundleSize, particleCount);
				
				for (int j = 0; j < trailSize; j++) {
					for (int i = firsttrail; i < lasttrail; i++) {
						//i is the trail's first element						
						var ii = i + j * trailBundleSize;
						if (ii >= particleCount) {
							break;
						}
						
						if(j != 0 || trailSize == 1)
						{
							/*
							//
							var metaii = meta.MapReadWrite(ref ii);
							metaii[ii].Size = 0;
							
							meta.MapReadWrite(ref ii)[ii].Size = 0;
							meta[ref ii][ii].Size = 0;
							meta.ReadWrite[ref ii][ii]
							(meta > ref ii)[ii]
							(meta > ii)[ii]
							//
							meta[ii].Size = 0;
							//
							meta.Oper(ii, ptr => ptr->Size = 0);
							//
							var metaii = meta[ii];
							metaii.Size = 0;
							meta[ii] = metaii;
						*/
							var _ii = ii;
							meta.MapReadWrite(ref _ii)[_ii].Size = Math.Max (system.ParticleGenerator.UpdateSize (system, i, ii), float.Epsilon);
						}
						
						if (j == trailSize - 1) {
							var _i = i;
							var metai = meta.MapReadWrite(ref _i);
							if (metai[_i].LifeLen <= 0) {
								system.ParticleGenerator.NewBundle (system, i);
							} 
							else
							{
								metai[_i].LifeLen--;
							}
						}
					}
				}
			}
		}
	}

	/// <summary>
	/// OneTimeScheme scheme.
	/// </summary>
	public class OneTimeGenerationScheme: IGenerationScheme
	{
		private bool m_MapModeComputed;
		private long m_PrevSimulStep;

		#region IGenerationScheme implementation
		void IGenerationScheme.Generate (System3 system, DateTime simulationTime, long simulationStep)
		{
			m_MapModeComputed &= simulationStep == m_PrevSimulStep + 1;
			m_PrevSimulStep = simulationStep;

			var Position = system.Position;
			var Meta = system.Meta;
			var Color = system.Color;
			var dt = (float)system.DT;

			//prepare seed points
			if (!m_MapModeComputed) 
			{			
				for (int i = 0; i < Meta.Length; i++) {
					system.ParticleGenerator.MakeBubble (system, 0, i);
				}
				m_MapModeComputed = true;
			}
		}

		string IGenerationScheme.Name 
		{
			get {
				return "OneTimeScheme";
			}
		}
		#endregion
	}
}


