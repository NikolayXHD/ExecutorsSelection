using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ExecutorsSelection
{
	/// <summary>
	/// A problem to optimally distribute work between a given set of work executors
	/// given their available time, work quality and payment rate.
	/// </summary>
	public class ExecutorsSelectionProblem
	{
		public Solution Solve()
		{
			int stagesCount = ExecutorWorkStages.Max() + 1;
			int nExecutors = ExecutorsCount;

			string problem = formulateLpProblem(nExecutors, stagesCount);
			var lines = readSolutionFor(problem);

			if (lines[0] == "This problem is infeasible")
				return new Solution
				{
					IsInfeasible = true
				};

			var workDistribution = parseSolution(lines, nExecutors);
			return createSolution(workDistribution);
		}

		private string formulateLpProblem(int nExecutors, int stagesCount)
		{
			const string format = "0.########";
			bool first = true;

			var lp = new StringBuilder();

			void maximum() => lp.Append("max: ");
			// void minimum() => lp.Append("min: ");

			void variable(int index, double multiplier)
			{
				if (multiplier < 0)
				{
					lp.Append('-');
					if (!first)
						lp.Append(' ');
				}
				else if (!first)
					lp.Append("+ ");

				multiplier = Math.Abs(multiplier);
				
				if (multiplier != 1d)
				{
					lp.Append(multiplier.ToString(format, CultureInfo.InvariantCulture));
					lp.Append(' ');
				}

				lp.Append("x");
				lp.Append(index.ToString(CultureInfo.InvariantCulture));
				lp.Append(' ');

				first = false;
			}

			void lte(double val)
			{
				lp.Append("<= ");
				lp.Append(val.ToString(format, CultureInfo.InvariantCulture));
			}

			void gte(double val)
			{
				lp.Append(">= ");
				lp.Append(val.ToString(format, CultureInfo.InvariantCulture));
			}

			void eq(double val)
			{
				lp.Append("= ");
				lp.Append(val.ToString(format, CultureInfo.InvariantCulture));
			}

			void endline()
			{
				first = true;
				lp.AppendLine(";");
			}

			maximum();
			for (int i = 0; i < nExecutors; i++)
				variable(i, multiplier: WorkQualities[i] * DeltaRate - PaymentRates[i] * DeltaQuality);

			endline();

			for (int i = 0; i < nExecutors; i++)
			{
				variable(i, multiplier: 1);
				lte(AvailableWorktimes[i] * WorkSpeeds[i]);
				endline();
			}

			for (int i = 0; i < nExecutors; i++)
				variable(i, multiplier: PaymentRates[i]);

			lte(MaxCost);
			endline();

			for (int i = 0; i < nExecutors; i++)
				variable(i, multiplier: WorkQualities[i]);

			gte(TotalWorkAmount * MinQuality);
			endline();

			for (int s = 0; s < stagesCount; s++)
			{
				for (int i = 0; i < nExecutors; i++)
					if (ExecutorWorkStages[i] == s)
						variable(i, multiplier: 1);

				eq(TotalWorkAmount);
				endline();
			}

			string problem = lp.ToString();
			return problem;
		}

		private static string[] readSolutionFor(string problem)
		{
			string fullSubdir = Path.GetFullPath("lpsolve");
			string path = Path.Combine(fullSubdir, "lp_solve.exe");

			var process = Process.Start(
				new ProcessStartInfo(path)
				{
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardError = true,
					RedirectStandardOutput = true,
					RedirectStandardInput = true
				});

			if (process == null)
				throw new ApplicationException("Failed to start lp_solve.exe");

			process.StandardInput.WriteLine(problem);
			process.StandardInput.Close();

			process.WaitForExit();

			string error = process.StandardError.ReadToEnd();

			if (!string.IsNullOrEmpty(error))
				throw new ApplicationException("lp_solve.exe error: " + error);

			string ouptut = process.StandardOutput.ReadToEnd();
			var lines = ouptut.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			return lines;
		}

		private static double[] parseSolution(string[] lines, int nExecutors)
		{
			var varListIndex = Array.IndexOf(lines, "Actual values of the variables:");

			if (varListIndex < 0)
				return null;

			var workDistribution = new double[nExecutors];

			for (int i = varListIndex + 1; i < lines.Length; i++)
			{
				var line = lines[i];

				if (line == string.Empty)
					continue;

				if (line[0] != 'x')
					throw new ArgumentException("unexpected line in lp solution: " + line);

				var nameDelimiterIndex = line.IndexOf(' ');
				int variableIndex = int.Parse(line.Substring(1, nameDelimiterIndex - 1), CultureInfo.InvariantCulture);
				var valueDelimiterIndex = line.LastIndexOf(' ');
				double value = double.Parse(line.Substring(valueDelimiterIndex + 1), CultureInfo.InvariantCulture);

				workDistribution[variableIndex] = value;
			}

			return workDistribution;
		}

		private Solution createSolution(double[] workDistribution)
		{
			if (workDistribution == null)
				return new Solution
				{
					IsUnbound = true
				};

			int nExecutors = workDistribution.Length;

			double averageQuality =
				Enumerable.Range(0, nExecutors)
					.Sum(i => workDistribution[i] * WorkQualities[i]) /
				TotalWorkAmount;

			double totalCost =
				Enumerable.Range(0, nExecutors)
					.Sum(i => workDistribution[i] * PaymentRates[i]);

			return new Solution
			{
				WorkDistribution = workDistribution,
				AverageQuality = averageQuality,
				TotalCost = totalCost
			};
		}



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

		public class Solution
		{
			public double[] WorkDistribution { get; set; }

			public double AverageQuality { get; set; }
			public double TotalCost { get; set; }

			public bool IsInfeasible { get; set; }
			public bool IsUnbound { get; set; }
		}
	}
}