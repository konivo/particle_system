using System;
using System.Linq;
using System.ComponentModel;
using System.Globalization;

namespace opentk.Scene
{
	public class LightConverter: ExpandableObjectConverter
	{
		public override bool GetStandardValuesSupported (ITypeDescriptorContext context)
		{
			return false;
		}

		public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType)
		{
			return typeof(Light).IsAssignableFrom(sourceType) ||
				sourceType == typeof(string);
		}

		public override object ConvertFrom (ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(typeof(Light).IsAssignableFrom(value.GetType()))
				return value;

			else if(value is string)
			{
				return new Light();
			}
			else return base.ConvertFrom(context, culture, value);
		}

		public override bool CanConvertTo (ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType.IsAssignableFrom(typeof(Light)) ||
			base.CanConvertTo(context, destinationType);
		}

		public override object ConvertTo (ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if(destinationType.IsAssignableFrom(typeof(Light)))
				return value;

			else return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}

