using System.Globalization;
using System.Linq;
using ExecutorsSelection;
using NLog;
using NUnit.Framework;

namespace Test
{
	[TestFixture]
	public class ExecutorSelectionProblemTests
	{
		[TearDown]
		public void Teardown() =>
			LogManager.Flush();

		[Test]
		public void When_max_cost_is_too_small_Then_solution_is_infeasible([Values(0, 50)] int maxCost)
		{
			var problem = createDefaultProblem();
			problem.MaxCost = maxCost;

			var solution = problem.Solve();

			log(solution);

			Assert.That(solution.IsInfeasible);
			Assert.That(solution.InfeasibleStages, Is.EquivalentTo(problem.ExecutorWorkStages.Distinct().ToArray()));
		}

		[Test]
		public void When_min_quality_is_too_high_Then_solution_is_infeasible([Values(0.51, 0.6, 1)] double minQuality)
		{
			var problem = createDefaultProblem();
			problem.MinQuality = minQuality;

			var solution = problem.Solve();

			log(solution);

			Assert.That(solution.IsInfeasible);
		}

		[Test]
		public void Lower_payment_rate_Is_preferred([Values(0, 1)] int lowerRateIndex)
		{
			var problem = createDefaultProblem();
			problem.PaymentRates[lowerRateIndex] *= 0.5;

			var solution = problem.Solve();

			log("Payment rate per worker", problem.PaymentRates);
			log(solution);

			Assert.That(solution.IsInfeasible, Is.False);
			Assert.That(solution.IsUnbound, Is.False);

			for (int i = 0; i < problem.ExecutorsCount; i++)
			{
				if (i == lowerRateIndex)
					Assert.That(solution.WorkDistribution[i], Is.EqualTo(problem.TotalWorkAmount));
				else
					Assert.That(solution.WorkDistribution[i], Is.EqualTo(0d));
			}
		}

		[Test]
		public void Higher_quality_Is_preferred([Values(0, 1)] int higherQualityIndex)
		{
			var problem = createDefaultProblem();
			problem.WorkQualities[higherQualityIndex] += 0.1;

			var solution = problem.Solve();

			log("Work quality per worker", problem.WorkQualities);
			log(solution);

			Assert.That(solution.IsInfeasible, Is.False);
			Assert.That(solution.IsUnbound, Is.False);

			for (int i = 0; i < problem.ExecutorsCount; i++)
			{
				if (i == higherQualityIndex)
					Assert.That(solution.WorkDistribution[i], Is.EqualTo(problem.TotalWorkAmount));
				else
					Assert.That(solution.WorkDistribution[i], Is.EqualTo(0d));
			}
		}

		private static ExecutorsSelectionProblem createDefaultProblem()
		{
			var problem = new ExecutorsSelectionProblem
			{
				TotalWorkAmount = 100,

				PaymentRates = new[] { 1d, 1d },
				WorkQualities = new[] { 0.5, 0.5 },

				AvailableWorktimes = new[] { 1000d, 1000d },
				WorkSpeeds = new[] { 1d, 1d },

				DeltaRate = 0.1,
				DeltaQuality = 0.01,

				MaxCost = 5000,
				MinQuality = 0.5,

				ExecutorWorkStages = new[] { 0, 0 }
			};

			return problem;
		}

		private static void log(string title, double[] values) =>
			_log.Debug($"{title}: " + string.Join(" ", values.Select(v => v.ToString("F1", _culture))));

		private static void log(string title, int[] values) =>
			_log.Debug($"{title}: " + string.Join(" ", values.Select(v => v.ToString("D", _culture))));
		
		private static void log(string title, double value) =>
			_log.Debug($"{title}: {value.ToString("F1", _culture)}");

		private static void log(ExecutorsSelectionProblem.Solution solution)
		{
			log("Optimal work distribution", solution.WorkDistribution);
			log("Total cost", solution.TotalCost);
			log("Average quality", solution.AverageQuality);
			log("Infeasible stages", solution.InfeasibleStages);
		}

		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		private static readonly CultureInfo _culture = CultureInfo.InvariantCulture;
	}
}