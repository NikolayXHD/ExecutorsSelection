using System;
using System.Linq;

namespace ExecutorsSelection
{
	/// <summary>
	/// A problem to optimally distribute work between a given set of work executors
	/// given their available time, work quality and payment rate.
	/// </summary>
	public class ExecutorsSelectionProblem
	{
		public double[] PaymentRates { get; set; }
		public double[] WorkQualities { get; set; }

		public double TotalWorkAmount { get; set; }
		public double[] AvailableWorktimes { get; set; }
		public double[] WorkSpeeds { get; set; }

		/// <summary>
		/// <see cref="DeltaRate"/> is such an increase of payment rate
		/// that increasing rate by it when quality changes by <see cref="DeltaQuality"/>
		/// keeps utility function (whose maximum we are seeking) unchanged
		/// </summary>
		public double DeltaRate { get; set; }

		/// <summary>
		/// <see cref="DeltaRate"/> is such an increase of payment rate
		/// that increasing rate by it when quality changes by <see cref="DeltaQuality"/>
		/// keeps utility function (whose maximum we are seeking) unchanged
		/// </summary>
		public double DeltaQuality { get; set; }

		public double MaxCost { get; set; }
		public double MinQuality { get; set; }

		public int[] ExecutorWorkStages { get; set; }

		public int ExecutorsCount => PaymentRates.Length;

		public Solution Solve()
		{
			int stagesCount = ExecutorWorkStages.Max() + 1;
			int nExecutors = ExecutorsCount;

			double[] b = new double[nExecutors + 2 + stagesCount * 2];

			for (int i = 0; i < nExecutors; i++)
				b[i] = AvailableWorktimes[i] * WorkSpeeds[i];

			b[nExecutors] = MaxCost;
			b[nExecutors + 1] = -TotalWorkAmount * MinQuality;

			for (int s = 0; s < stagesCount; s++)
			{
				int i = nExecutors + 2 + s * 2;
				b[i] = TotalWorkAmount;
				b[i + 1] = -TotalWorkAmount;
			}

			double[] c = new double[nExecutors];
			for (int i = 0; i < nExecutors; i++)
				c[i] = -PaymentRates[i] + WorkQualities[i] * DeltaRate / DeltaQuality;

			double[,] a = new double[b.Length, c.Length];

			for (int i = 0; i < nExecutors; i++)
				a[i, i] = 1;

			for (int j = 0; j < nExecutors; j++)
			{
				a[nExecutors, j] = PaymentRates[j];
				a[nExecutors + 1, j] = -WorkQualities[j];

				int index = nExecutors + 2 + ExecutorWorkStages[j] * 2;
				a[index, j] = 1;
				a[index + 1, j] = -1;
			}

			var lpProblem = new LinearProgrammingProblem(b, c, a);
			var max = lpProblem.FindMaximum();

			if (double.IsPositiveInfinity(max.Value))
				return new Solution
				{
					IsUnbound = true
				};

			var workDistribution = max.Vector.Take(nExecutors).ToArray();

			const double epsilon = 1e-5;

			var infeasibleStages = Enumerable.Range(0, stagesCount)
				.Where(s => epsilon < Math.Abs(
					TotalWorkAmount -
					Enumerable.Range(0, nExecutors)
						.Where(i => ExecutorWorkStages[i] == s)
						.Sum(i => workDistribution[i])))
				.ToArray();

			double averageQuality =
				Enumerable.Range(0, nExecutors)
					.Sum(i => workDistribution[i] * WorkQualities[i]) /
				TotalWorkAmount;

			return new Solution
			{
				WorkDistribution = workDistribution,
				AverageQuality = averageQuality,
				TotalCost = Enumerable.Range(0, nExecutors).Sum(i => workDistribution[i] * PaymentRates[i]),
				InfeasibleStages = infeasibleStages,
				IsInfeasible = infeasibleStages.Length > 0 || averageQuality < MinQuality
			};
		}

		public class Solution
		{
			public double[] WorkDistribution { get; set; }

			public double AverageQuality { get; set; }
			public double TotalCost { get; set; }

			public int[] InfeasibleStages { get; set; }
			public bool IsInfeasible { get; set; }

			public bool IsUnbound { get; set; }
		}
	}
}