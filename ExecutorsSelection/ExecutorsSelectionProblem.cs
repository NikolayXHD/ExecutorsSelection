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
			validate();

			string problem = formulateLpProblem();
			var lines = calculateSolutionFor(problem);

			if (lines[0] == "This problem is infeasible")
				return new Solution
				{
					IsInfeasible = true
				};

			var workDistribution = parseSolution(lines);

			if (workDistribution == null)
				return new Solution
				{
					IsUnbound = true
				};

			return createSolution(workDistribution);
		}

		private void validate()
		{
			void validateWorkerProperties<T>(string name, T[] array)
				where T : IComparable<T>
			{
				if (array == null)
					throw new InvalidOperationException($"{name} is null");

				if (array.Length != ExecutorsCount)
					throw new InvalidOperationException($"Invalid {name} length {array.Length}. Expected {ExecutorsCount}");

				for (int i = 0; i < array.Length; i++)
					if (array[i].CompareTo(default(T)) < 0)
						throw new InvalidOperationException($"negative value {array[i]} in {name} at position {i}");
			}

			void validatePositiveParameter(string name, double value)
			{
				if (value < 0)
					throw new InvalidOperationException($"negative value {value} of parameter {name}");
			}

			void validateWorkStages()
			{
				validateWorkerProperties(nameof(WorkStages), WorkStages);

				if (WorkStages[0] != 0)
					throw new InvalidOperationException($"{nameof(WorkStages)}[0] must be 0. Actual value {WorkStages[0]}");

				for (int i = 1; i < ExecutorsCount; i++)
				{
					int delta = WorkStages[i] - WorkStages[i - 1];

					if (delta < 0 || delta > 1)
						throw new InvalidOperationException($"{nameof(WorkStages)} values increment must be 1 or 0. Actual increment: [{i - 1}]={WorkStages[i - 1]}, [{i}]={WorkStages[i]} => increment = {delta}");
				}
			}

			validateWorkerProperties(nameof(PaymentRates), PaymentRates);
			validateWorkerProperties(nameof(WorkQualities), WorkQualities);
			validateWorkerProperties(nameof(AvailableTimes), AvailableTimes);
			validateWorkerProperties(nameof(WorkSpeeds), WorkSpeeds);
			validateWorkStages();

			validatePositiveParameter(nameof(TotalWorkAmount), TotalWorkAmount);
			validatePositiveParameter(nameof(DeltaCost), DeltaCost);
			validatePositiveParameter(nameof(DeltaQuality), DeltaQuality);
		}



		private string formulateLpProblem()
		{
			// http://lpsolve.sourceforge.net/5.1/lp-format.htm

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

				string multiplierStr = multiplier.ToString(format, CultureInfo.InvariantCulture);

				if (multiplierStr != "1")
				{
					lp.Append(multiplierStr);
					lp.Append(' ');
				}

				lp.Append('x');
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
			for (int i = 0; i < ExecutorsCount; i++)
				variable(i, multiplier: WorkQualities[i] * DeltaCost - PaymentRates[i] * DeltaQuality);

			endline();

			for (int i = 0; i < ExecutorsCount; i++)
			{
				variable(i, multiplier: 1);
				lte(AvailableTimes[i] * WorkSpeeds[i]);
				endline();
			}

			for (int i = 0; i < ExecutorsCount; i++)
				variable(i, multiplier: PaymentRates[i]);

			lte(MaxCost);
			endline();

			for (int i = 0; i < ExecutorsCount; i++)
				variable(i, multiplier: WorkQualities[i]);

			gte(TotalWorkAmount * MinQuality);
			endline();

			for (int s = 0; s < StagesCount; s++)
			{
				for (int i = 0; i < ExecutorsCount; i++)
					if (WorkStages[i] == s)
						variable(i, multiplier: 1);

				eq(TotalWorkAmount);
				endline();
			}

			string problem = lp.ToString();
			return problem;
		}

		private static string[] calculateSolutionFor(string problem)
		{
			// http://lpsolve.sourceforge.net/5.1/lp_solve.htm

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

		private double[] parseSolution(string[] lines)
		{
			var varListIndex = Array.IndexOf(lines, "Actual values of the variables:");

			if (varListIndex < 0)
				return null;

			var workDistribution = new double[ExecutorsCount];

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
		public double[] AvailableTimes { get; set; }
		public double[] WorkSpeeds { get; set; }

		/// <summary>
		/// <see cref="DeltaCost"/> is such an increase of project cost that
		/// when quality changes by <see cref="DeltaQuality"/> 
		/// it keeps utility function (whose maximum we are seeking) unchanged
		/// </summary>
		public double DeltaCost { get; set; }

		/// <summary>
		/// <see cref="DeltaQuality"/> is such an increase of quality that
		/// when project cost changes by <see cref="DeltaCost"/> 
		/// it keeps utility function (whose maximum we are seeking) unchanged
		/// </summary>
		public double DeltaQuality { get; set; }

		public double MaxCost { get; set; }
		public double MinQuality { get; set; }

		public int[] WorkStages { get; set; }

		public int ExecutorsCount => PaymentRates.Length;
		public int StagesCount => WorkStages[WorkStages.Length - 1] + 1;

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