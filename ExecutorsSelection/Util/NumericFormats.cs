using System.Globalization;

namespace ExecutorsSelection
{
	public static class NumericFormats
	{
		private static readonly CultureInfo _culture = CultureInfo.InvariantCulture;
		private const string Double = "0.########";

		public static string Format(this double value) =>
			value.ToString(Double, _culture);

		public static string Format(this int value) =>
			value.ToString(_culture);

		public static string Format2(this double value) =>
			value.ToString("0.##", _culture);

		public static string Format(this bool value) =>
			value.ToString().ToUpper(_culture);
	}
}