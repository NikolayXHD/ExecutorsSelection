using System;
using System.Globalization;

namespace ExecutorsSelection
{
	public static class NumericFormats
	{
		private static readonly CultureInfo _culture = CultureInfo.InvariantCulture;
		private const string Double = "0.########";

		public static string Format(this double value) =>
			value.ToString(Double, _culture);

		public static string Format(this bool value) =>
			value.ToString().ToUpper(_culture);
	}
}