using System;
using System.Linq;
using OpenTK;

namespace opentk.Scene
{
	/// <summary>
	///
	/// </summary>
	public class LightImplementationParameters
	{
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

		public LightImplementationParameters (Light light)
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

