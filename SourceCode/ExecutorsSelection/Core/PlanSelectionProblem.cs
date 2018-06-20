using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using NLog;

namespace ExecutorsSelection
{
	public class PlanSelectionProblem
	{
		public Project Project { get; set; }
		public bool OnePlanPerTimeLimit { get; set; }

		public HashSet<ExecutorProfile>[] ExecutorsByStage { get; set; }
		public double[] ExpectedPagesPerHourByStage { get; set; }
		public double Now { get; set; }

		public Plan[] SuggestPlans(out List<Plan> rejectedPlans)
		{
			var plans = new List<Plan>();
			rejectedPlans = new List<Plan>();

			double[] possibleTimeLimits;
			if (Project.MaxTime.HasValue)
				possibleTimeLimits = new[] { Project.MaxTime.Value };
			else
			{
				double p = Project.TotalWorkPages;
				var tMax = p * _longestSequence.Sum(s => 1 / ExpectedPagesPerHourByStage[s]);
				var tMin = 0.5 * p / Math.Log(p) * _shortestSequence.Sum(s => 1 / ExpectedPagesPerHourByStage[s]);
				var tMid = 0.5 * (tMax + tMin);

				possibleTimeLimits = new[] { tMin, tMid, tMax };
			}

			foreach (double timeLimit in possibleTimeLimits)
			{
				var timeLimitPlans = new List<Plan>();

				for (int p = 0; p < _possibleStageSequences.Length; p++)
				{
					var stageSequence = _possibleStageSequences[p];
					var durationProportions = _stageDurationProportions[p];
					
					var plan = getPlan(stageSequence, durationProportions, timeLimit);

					if (plan != null)
						timeLimitPlans.Add(plan);
				}

				if (timeLimitPlans.Count == 0)
					continue;

				if (OnePlanPerTimeLimit)
				{
					var bestPlan = timeLimitPlans.OrderByDescending(p => p.Score).First();
					plans.Add(bestPlan);

					rejectedPlans.AddRange(timeLimitPlans.Where(p => p != bestPlan));
				}
				else
				{
					plans.AddRange(timeLimitPlans);
				}
			}

			var result = plans
				.OrderByDescending(p => p.Score)
				.ToArray();

			rejectedPlans = rejectedPlans
				.OrderByDescending(p => p.Score)
				.ToList();

			return result;
		}

		private Plan getPlan(int[] stageSequence, double[] durationProportions, double maxTime)
		{
			var (problem, executors) = formulateExecutorsSelectionProblem(stageSequence, durationProportions, maxTime);

			if (problem == null)
				return null;

			var sw = new Stopwatch();
			sw.Start();

			var solution = problem.Solve();

			sw.Stop();
			_log.Debug($"problem with {stageSequence.Length} stages solved in {sw.ElapsedMilliseconds} ms");

			if (solution.IsUnbound)
			{
				string serializedProblem = JsonConvert.SerializeObject(problem, Formatting.Indented);
				throw new Exception("Unbound solution for problem:" + Environment.NewLine + serializedProblem);
			}

			if (solution.IsInfeasible)
				return null;

			var projectAssignments = Enumerable.Range(0, executors.Count)
				.Where(i => solution.WorkDistribution[i] > 0)
				.Select(i => executors[i].CreateAssignment(solution.WorkDistribution[i]))
				.ToArray();

			var stageSubtotals = stageSequence.Select(s =>
			{
				var stageAssignments = projectAssignments
					.Where(e => e.WorkStage == s)
					.ToArray();

				return new PlanSubtotalsByStage
				{
					Stage = WorkStageNames.Names[s],
					ExecutorsCount = projectAssignments.Count(e => e.WorkStage == s),
					Time = stageAssignments.Max(e => e.Time),
					CostPerPage = 
						stageAssignments.Sum(e => e.Pages * e.PaymentRatePerPage) /
						problem.TotalWorkAmount
				};
			}).ToArray();

			return new Plan
			{
				Time = stageSubtotals.Sum(s => s.Time),
				Cost = solution.TotalCost,
				Quality = solution.AverageQuality,
				Executors = projectAssignments,
				StageSubtotals = stageSubtotals,

				Score = solution.Score
			};
		}

		private (ExecutorsSelectionProblem Problem, List<ExecutorProfile> Executors) formulateExecutorsSelectionProblem(
			int[] stagesSequence,
			double[] durationProportions,
			double maxTime)
		{
			var executors = new List<ExecutorProfile>();
			var availableTimes = new List<double>();
			var problemStages = new List<int>();
			var problemQualities = new List<double>();

			var now = Now;

			for (var s = 0; s < stagesSequence.Length; s++)
			{
				var stage = stagesSequence[s];
				var stageTime = durationProportions[s] * maxTime;

				var stageExecutors = ExecutorsByStage[stage];

				bool stageExecutorsAreAvailable = false;

				foreach (var executor in stageExecutors)
				{
					var availableTime = executor.Schedule.GetAvailableTime(now, duration: stageTime);

					if (availableTime > 0)
					{
						stageExecutorsAreAvailable = true;

						executors.Add(executor);
						availableTimes.Add(availableTime);
						problemStages.Add(s);

						var quality = s == stagesSequence.Length - 1
							? executor.Quality
							: 0;

						problemQualities.Add(quality);
					}
				}

				if (!stageExecutorsAreAvailable)
					return (null, executors);

				now += stageTime;
			}

			return (new ExecutorsSelectionProblem
			{
				TotalWorkAmount = Project.TotalWorkPages,
				MinQuality = Project.MinQuality,
				MaxCost = Project.MaxCost,
				DeltaCost = Project.DeltaCost,
				DeltaQuality = Project.DeltaQuality,

				AvailableTimes = availableTimes.ToArray(),
				WorkStages = problemStages.ToArray(),
				WorkQualities = problemQualities.ToArray(),
				PaymentRates = executors.Select(e => e.PaymentRatePerPage).ToArray(),
				WorkSpeeds = executors.Select(e => e.PagesPerHour).ToArray()
			}, executors);
		}

		private static readonly int[][] _possibleStageSequences =
		{
			new[] { 0 },
			new[] { 1 },
			new[] { 1, 2 },
			new[] { 1, 2, 3 }
		};

		private static readonly int[] _shortestSequence = { 1 };
		private static readonly int[] _longestSequence = { 1, 2, 3 };


		private static readonly double[][] _stageDurationProportions =
		{
			new[] { 1d },
			new[] { 1d },
			new[] { 2d / 3d, 1d / 3d },
			new[] { 6d / 9d, 2d / 9d, 1d / 9d }
		};

		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
	}
}