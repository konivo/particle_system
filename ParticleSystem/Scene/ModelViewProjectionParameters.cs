using System;
using System.Linq;
using OpenTK;

namespace opentk.Scene
{
	/// <summary>
	///
	/// </summary>
	public class ModelViewProjectionParameters
	{
		private MatrixStack m_Transformation;
		private MatrixStack m_TransformationInv;

		public string Prefix
		{
			get; private set;
		}

		public IValueProvider<Matrix4> ModelView
		{
			get; private set;
		}

		public IValueProvider<Matrix4> ModelViewProjection
		{
			get; private set;
		}

		public IValueProvider<Matrix4> Projection
		{
			get; private set;
		}

		public IValueProvider<Matrix4> ModelViewInv
		{
			get; private set;
		}

		public IValueProvider<Matrix4> ModelViewProjectionInv
		{
			get; private set;
		}

		public IValueProvider<Matrix4> ProjectionInv
		{
			get; private set;
		}

		public ModelViewProjectionParameters
		(
			 string prefix,
			 IValueProvider<Matrix4> modelview,
			 IValueProvider<Matrix4> projection
		)
		{
			m_Transformation = new MatrixStack();
			m_TransformationInv = new MatrixStack();

			Prefix = prefix;
			ModelView = modelview;
			ModelViewInv = new MatrixInversion(modelview);
			Projection = projection;
			ProjectionInv = new MatrixInversion(projection);
			ModelViewProjection = m_Transformation;
			ModelViewProjectionInv = m_TransformationInv;

			m_Transformation.Push(Projection);
			m_Transformation.Push(ModelView);

			m_TransformationInv.Push(ModelViewInv);
			m_TransformationInv.Push(ProjectionInv);
		}

		public void SetUniforms(string prefix, UniformState state)
		{
			string pp = String.IsNullOrEmpty(prefix)?
				String.IsNullOrEmpty(Prefix) ?
					string.Empty:
					Prefix + "_":
				prefix + "_";

			state.Set (pp + "modelview_transform", ModelView);
			state.Set (pp + "modelviewprojection_transform", ModelViewProjection);
			state.Set (pp + "projection_transform", Projection);
			state.Set (pp + "projection_inv_transform", ProjectionInv);
			state.Set (pp + "modelview_inv_transform", ModelViewInv);
			state.Set (pp + "modelviewprojection_inv_transform", ModelViewProjectionInv);
		}
	}
}