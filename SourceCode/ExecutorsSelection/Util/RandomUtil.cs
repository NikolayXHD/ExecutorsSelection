using System;
using MathNet.Numerics.Distributions;

namespace ExecutorsSelection
{
	public static class RandomUtil
	{
		public static double NextDouble(double min, double max) =>
			min + (max - min) * _rand.NextDouble();

		public static double NextDouble() =>
			_rand.NextDouble();

		public static T RandomElement<T>(this T[] values) =>
			values[_rand.Next(values.Length)];

		public static double NextDoubleGamma05(double average) =>
			_gamma05.Sample() * average;

		public static Gamma CreateGamma(double average, double variance) =>
			new Gamma(average * average / variance, average / variance);

		private static readonly Gamma _gamma05 = CreateGamma(1, 0.5);
		private static readonly Random _rand = new Random();
	}
}