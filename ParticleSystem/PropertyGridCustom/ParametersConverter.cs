using System;
using System.Linq;
using System.ComponentModel;
using System.Globalization;

namespace opentk.PropertyGridCustom
{
	public class ParametersConverter<T>: ExpandableObjectConverter where T:class
	{
		public override bool GetStandardValuesSupported (ITypeDescriptorContext context)
		{
			return false;
		}

		public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType)
		{
			return typeof(T).IsAssignableFrom(sourceType) ||
				sourceType == typeof(string);
		}

		public override object ConvertFrom (ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(typeof(T).IsAssignableFrom(value.GetType()))
				return value;

			else if(value is string)
			{
				return default(T);
			}
			else return base.ConvertFrom(context, culture, value);
		}

		public override bool CanConvertTo (ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType.IsAssignableFrom(typeof(T)) ||
			base.CanConvertTo(context, destinationType);
		}

		public override object ConvertTo (ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if(destinationType.IsAssignableFrom(typeof(T)))
				return value;

			else return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}

