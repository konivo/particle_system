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
using System.Collections.Generic;

namespace opentk.System3
{
	/// <summary>
	/// Particles with trails.
	/// </summary>
	public class ParticlesWithTrailsGenerationScheme: IGenerationScheme
	{
		private IEnumerator<long> m_GIndices;
		private int m_Length;
	
		string IGenerationScheme.Name 
		{
			get {
				return "ParticlesWithTrails";
			}
		}
		
		IEnumerable<long> GenerationIndices(System3 system)
		{
			var trailSize = Math.Max (system.TrailSize, 1);
			var trailCount = (system.Position.Length + trailSize - 1) / trailSize;
			var trailBundleSize = Math.Max (system.SimulationScheme is ParticlesWithTrails ? ((ParticlesWithTrails)system.SimulationScheme).TrailBundleSize : 1, 1);
			var trailBundleCount = (trailCount + trailBundleSize - 1) / trailBundleSize;
			
			var position = system.Position;			
			for (int bundleIndex = 0; bundleIndex < trailBundleCount; bundleIndex++) 
			{
				var firsttrail = bundleIndex * trailBundleSize * trailSize;
				var lasttrail = Math.Min (firsttrail + trailBundleSize, system.Position.Length);
				
				for (int j = 0; j < trailSize; j++) {
					for (int i = firsttrail; i < lasttrail; i++) {
						//i is the trail's first element						
						var ii = i + j * trailBundleSize;
						if (ii >= system.Position.Length) {
							yield break;
						}
						
						yield return (long)i << 32 | (uint)j;
					}
				}
			}
		}

		void IGenerationScheme.Generate (System3 system, DateTime simulationTime, long simulationStep)
		{
			var trailBundleSize = Math.Max (system.SimulationScheme is ParticlesWithTrails ? ((ParticlesWithTrails)system.SimulationScheme).TrailBundleSize : 1, 1);
			var dt = (float)system.DT;
			
			m_GIndices = m_GIndices ?? GenerationIndices(system).GetEnumerator();
			var sw = new System.Diagnostics.Stopwatch();
			
			sw.Start();
			while(m_GIndices.MoveNext())
			{
				var pack = m_GIndices.Current;
				var i = (int)(pack >> 32);
				var j = (int)(pack);
				
				var ii = i + j * trailBundleSize;
				if (ii >= system.Position.Length) {
					break;
				}
				
				var _i = i;
				var metai = system.Meta.MapReadWrite(ref _i);						
				if(j == 0)
				{
					if (m_Length != system.Position.Length)
					{
						system.ParticleGenerator.NewBundle (system, i);
					}
					else if (metai[_i].LifeLen <= 0) {
						system.ParticleGenerator.NewBundle (system, i);
					}
					else
					{
						metai[_i].LifeLen--;
						metai[_i].Size = Math.Max (system.ParticleGenerator.UpdateSize (system, i, i), float.Epsilon);
					}
				}
				else
				{
					var _ii = ii;
					system.Meta.MapReadWrite(ref _ii)[_ii].Size = Math.Max (system.ParticleGenerator.UpdateSize (system, i, ii), float.Epsilon);
				}
				
				if(m_Length == system.Position.Length)
					if(sw.Elapsed > TimeSpan.FromMilliseconds(5))
						return;
			}
			
			m_GIndices.Dispose ();
			m_GIndices = null;
			m_Length = system.Position.Length;
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


