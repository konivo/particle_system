using System;
using System.Linq;
using System.ComponentModel;
using System.Globalization;
namespace opentk
{
	public class AocParametersConverter: ExpandableObjectConverter
	{
		public override bool GetStandardValuesSupported (ITypeDescriptorContext context)
		{
			return false;
		}

		public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType)
		{
			return typeof(AocParameters).IsAssignableFrom(sourceType) ||
				sourceType == typeof(string);
		}

		public override object ConvertFrom (ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(typeof(AocParameters).IsAssignableFrom(value.GetType()))
				return value;

			else if(value is string)
			{
				return new AocParameters();
			}
			else return base.ConvertFrom(context, culture, value);
		}

		public override bool CanConvertTo (ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType.IsAssignableFrom(typeof(AocParameters)) ||
			base.CanConvertTo(context, destinationType);
		}

		public override object ConvertTo (ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if(destinationType.IsAssignableFrom(typeof(AocParameters)))
				return value;

			else return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}

