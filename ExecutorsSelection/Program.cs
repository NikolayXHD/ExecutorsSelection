using System;
using System.Diagnostics;

namespace ExecutorsSelection
{
	class Program
	{
		static void Main(string[] args)
		{
			double[] paymentRates = { 1, 1 };
			double[] workQualities = { 0.5, 1 };
			
			// total words to translate
			double workAmount = 100;
			double[] availableWorktimes = { 1000, 1000 };
			double[] workSpeeds = { 1, 1 };

			// deltaRate is such an increase of payment rate
			// that increasing rate by it when quality changes by deltaQuality
			// keeps utility function (whose maximum we are seeking) unchanged
			double deltaRate = 0.1;
			double deltaQuality = 0.01;

			double maxCost = 5000;
			double minQuality = 0.5;

			const int stagesCount = 1;

			int[] executorStages = new int[] { 0, 0 };
			int nExecutors = paymentRates.Length;

			double[] b = new double[nExecutors + 2 + stagesCount * 2];

			for (int i = 0; i < nExecutors; i++)
				b[i] = availableWorktimes[i] * workSpeeds[i];

			b[nExecutors] = maxCost;
			b[nExecutors + 1] = -workAmount * minQuality;

			for (int s = 0; s < stagesCount; s++)
			{
				int i = nExecutors + 2 + s * 2;
				b[i] = workAmount;
				b[i + 1] = -workAmount;
			}

			double[] c = new double[nExecutors];
			for (int i = 0; i < nExecutors; i++)
				c[i] = -paymentRates[i] + workQualities[i] * deltaRate / deltaQuality;

			double[,] a = new double[b.Length, c.Length];

			for (int i = 0; i < nExecutors; i++)
				a[i, i] = 1;

			for (int j = 0; j < nExecutors; j++)
			{
				a[nExecutors, j] = paymentRates[j];
				a[nExecutors + 1, j] = -workQualities[j];

				int index = nExecutors + 2 + executorStages[j] * 2;
				a[index, j] = 1;
				a[index + 1, j] = -1;
			}

			var simplex = new Simplex(b, c, a);
			var result = simplex.Maximize();

			Console.WriteLine($"Maximized value: {result.Value}");
			Console.WriteLine("Vector:");

			for (int i = 0; i < result.Vector.Length; i++)
			{
				double v = result.Vector[i];
				Console.Write(v.ToString("F2"));
				
				if (i < result.Vector.Length - 1)
					Console.Write(' ');
			}

			Console.ReadLine();
		}
	}
}
