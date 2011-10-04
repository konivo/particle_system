using System;
using System.Linq;
using System.ComponentModel;
using System.Globalization;
using opentk.ShadingSetup;

namespace opentk.ShadingSetup
{
	public class RenderSetupConverter: ExpandableObjectConverter
	{
		private IShadingSetup[] m_Shadings = GlobalContext.Container.GetExportedValues<IShadingSetup>().ToArray();

		public override bool GetStandardValuesSupported (ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType)
		{
			return typeof(IShadingSetup).IsAssignableFrom(sourceType) ||
				sourceType == typeof(string);
		}

		public override object ConvertFrom (ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(typeof(IShadingSetup).IsAssignableFrom(value.GetType()))
				return value;

			else if(value is string)
			{
				return m_Shadings.First(x => x.GetType().FullName == (string)value);
			}
			else return base.ConvertFrom(context, culture, value);
		}

		public override bool CanConvertTo (ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType.IsAssignableFrom(typeof(IShadingSetup)) ||
			base.CanConvertTo(context, destinationType);
		}

		public override object ConvertTo (ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if(destinationType.IsAssignableFrom(typeof(IShadingSetup)))
				return value;

			else return base.ConvertTo(context, culture, value, destinationType);
		}

		public override TypeConverter.StandardValuesCollection GetStandardValues (ITypeDescriptorContext context)
		{
			return new TypeConverter.StandardValuesCollection(m_Shadings);
		}

		public override bool GetStandardValuesExclusive (ITypeDescriptorContext context)
		{
			return true;
		}
	}
}

