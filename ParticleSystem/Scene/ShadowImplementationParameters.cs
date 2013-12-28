using System;
using System.Linq;
using OpenTK;
using System.ComponentModel;
using opentk.PropertyGridCustom;

namespace opentk.Scene
{
	/// <summary>
	/// Shadow implementation type.
	/// </summary>
	public enum ShadowImplementationType
	{
		NoFilter,
		Filter2x2,
		Filter4x4,
		Filter8x8,
		Filter16x16,
		Soft1,
		Soft2
	}
	/// <summary>
	/// Shadowmap type.
	/// </summary>
	public enum ShadowmapType
	{
		Default,
		Exponential,
		//Variance
	}
	/// <summary>
	///
	/// </summary>
	public class ShadowImplementationParameters
	{
		[Category("Light properties")]
		[TypeConverter(typeof(ParametersConverter<Light>))]
		public Light Light
		{
			get; private set;
		}

		public Vector3 SceneExtentMin
		{
			get; set;
		}

		public Vector3 SceneExtentMax
		{
			get; set;
		}

		public ModelViewProjectionParameters LightMvp
		{
			get; private set;
		}

		public IValueProvider<Matrix4> LightIlluminationTransformProvider
		{
			get; private set;
		}

		public ShadowImplementationType ImplementationType
		{
			get; set;
		}
		
		public ShadowmapType ShadowmapType
		{
			get; set;
		}

		public ShadowImplementationParameters (Light light)
		{
			Light = light;

			var LightSpaceModelviewProvider =
				ValueProvider.Create(() => Matrix4.LookAt(- Light.Direction * 300, Light.Direction * 300, Vector3.UnitZ));

			var LightSpaceProjectionProvider =
				ValueProvider.Create(
				() =>
				{
					if(Light.Type == LightType.Directional)
					{
						return Matrix4.CreateOrthographic(300, 300, 0, 600);
					}
					else
					{
						return  Matrix4.CreateTranslation(- Light.Position);
					}
				});

			LightMvp = new ModelViewProjectionParameters
			(
				 "",
				 LightSpaceModelviewProvider,
				 LightSpaceProjectionProvider
			);

			//
			LightIlluminationTransformProvider = ValueProvider.Create(
			() =>
			{
				if(Light.Type == LightType.Directional)
				{
					return Matrix4.CreateTranslation(Light.Direction);
				}
				else
				{
					return  Matrix4.CreateTranslation(- Light.Position);
				}
			});

		}
	}
}

