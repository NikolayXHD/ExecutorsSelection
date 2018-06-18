using ExecutorsSelection;
using NUnit.Framework;

namespace Test
{
	[TestFixture]
	public class OneStageProblemTests : TestsBase
	{
		[Test]
		public void When_max_cost_is_too_low_Then_solution_is_infeasible([Values(0, 50)] int maxCost)
		{
			var problem = createDefaultProblem();
			problem.MaxCost = maxCost;

			var solution = problem.Solve();
			Log(solution);

			Assert.That(solution.IsInfeasible);
		}

		[Test]
		public void When_min_quality_is_too_high_Then_solution_is_infeasible([Values(0.51, 0.6, 1)] double minQuality)
		{
			var problem = createDefaultProblem();
			problem.MinQuality = minQuality;

			var solution = problem.Solve();
			Log(solution);

			Assert.That(solution.IsInfeasible);
		}

		[TestCase(99, 1, ExpectedResult = true)]
		[TestCase(100, 1, ExpectedResult = false)]
		[TestCase(49, 2, ExpectedResult = true)]
		[TestCase(50, 2, ExpectedResult = false)]
		public bool When_not_enough_available_time_Then_solution_is_infeasible(double totalTime, double speed)
		{
			var problem = createDefaultProblem();
			problem.AvailableTimes = new[] { totalTime / 2, totalTime / 2 };
			problem.WorkSpeeds = new[] { speed, speed };

			var solution = problem.Solve();
			Log(solution);

			return solution.IsInfeasible;
		}

		[Test]
		public void Lower_payment_rate_Is_preferred([Values(0, 1)] int lowerRateIndex)
		{
			var problem = createDefaultProblem();
			problem.PaymentRates[lowerRateIndex] *= 0.5;

			Log("Payment rate per worker", problem.PaymentRates);

			var solution = problem.Solve();
			Log(solution);

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
			Log("Work quality per worker", problem.WorkQualities);

			var solution = problem.Solve();
			Log(solution);

			for (int i = 0; i < problem.ExecutorsCount; i++)
			{
				if (i == higherQualityIndex)
					Assert.That(solution.WorkDistribution[i], Is.EqualTo(problem.TotalWorkAmount));
				else
					Assert.That(solution.WorkDistribution[i], Is.EqualTo(0d));
			}
		}

		[Test]
		public void Minimum_quality_Is_observed([Values(0, 0.01, 0.75, 0.99, 1)] double minQuality)
		{
			var problem = createDefaultProblem();

			const int quality = 1;
			problem.WorkQualities = new double[] { 0, quality };
			problem.PaymentRates = new double[] { 0, 1 };
			problem.MinQuality = minQuality;

			// totally prefer low payment rate over quality
			problem.DeltaRate = 0;

			Log("Min quality", problem.MinQuality);
			Log("Payment rate per worker", problem.PaymentRates);
			Log("Work quality per worker", problem.WorkQualities);

			var solution = problem.Solve();
			Log(solution);

			Assert.That(solution.IsInfeasible, Is.False);
			Assert.That(solution.IsUnbound, Is.False);

			Assert.That(solution.WorkDistribution[0], Is.EqualTo((1 - minQuality) / quality * problem.TotalWorkAmount).Within(Epsilon));
			Assert.That(solution.WorkDistribution[1], Is.EqualTo(minQuality / quality * problem.TotalWorkAmount).Within(Epsilon));
		}

		[Test]
		public void Maximum_cost_Is_observed([Values(0, 1, 75, 99, 100)] double maxCost)
		{
			var problem = createDefaultProblem();

			problem.WorkQualities = new double[] { 0, 1 };

			const int paymentRate = 1;
			problem.PaymentRates = new double[] { 0, paymentRate };
			problem.MaxCost = maxCost;
			problem.MinQuality = 0;

			// Totally prefer quality over low payment rate
			problem.DeltaQuality = 0;

			Log("Max cost", problem.MaxCost);
			Log("Payment rate per worker", problem.PaymentRates);
			Log("Work quality per worker", problem.WorkQualities);

			var solution = problem.Solve();
			Log(solution);

			Assert.That(solution.IsInfeasible, Is.False);
			Assert.That(solution.IsUnbound, Is.False);

			Assert.That(solution.WorkDistribution[0], Is.EqualTo(problem.TotalWorkAmount - maxCost / paymentRate).Within(Epsilon));
			Assert.That(solution.WorkDistribution[1], Is.EqualTo(maxCost / paymentRate).Within(Epsilon));
		}

		[Test]
		public void Qualtiy_over_price_preference_ratio_Is_observed(
			[Values(0, 1)] int higherQualityIndex,
			[Values(0.01)] double deltaQuality,
			[Values(-0.5, 0.01, 0.09, 0.11, 1)] double deltaRate)
		{
			var problem = createDefaultProblem();
			problem.WorkQualities[higherQualityIndex] += deltaQuality;
			problem.PaymentRates[higherQualityIndex] += deltaRate;

			bool qualityIncrementIsTooExpensive = deltaRate / deltaQuality > problem.DeltaRate / problem.DeltaQuality;

			Log("Max delta Payment rate", problem.DeltaRate);
			Log("Per delta Work quality", problem.DeltaQuality);
			Log("Work quality per worker", problem.WorkQualities);
			Log("Payment rate per worker", problem.PaymentRates);

			if (qualityIncrementIsTooExpensive)
				Log("Quality increment is too expensive", true);

			var solution = problem.Solve();
			Log(solution);

			for (int i = 0; i < problem.ExecutorsCount; i++)
			{
				if ((i == higherQualityIndex) ^ qualityIncrementIsTooExpensive)
					Assert.That(solution.WorkDistribution[i], Is.EqualTo(problem.TotalWorkAmount));
				else
					Assert.That(solution.WorkDistribution[i], Is.EqualTo(0d));
			}
		}

		[Test]
		public void Available_time_Is_observed(
			[Values(0, 2, 10)] double availableTime,
			[Values(0, 1, 5)] double speed)
		{
			var problem = createDefaultProblem();
			problem.WorkQualities[0] += 0.01;

			problem.AvailableTimes[0] = availableTime;
			problem.WorkSpeeds[0] = speed;

			Log("Work quality per worker", problem.WorkQualities);
			Log("Available time per worker", problem.AvailableTimes);
			Log("Work speed per worker", problem.WorkSpeeds);

			var solution = problem.Solve();
			Log(solution);

			Assert.That(solution.WorkDistribution[0], Is.EqualTo(availableTime * speed).Within(Epsilon));
			Assert.That(solution.WorkDistribution[1], Is.EqualTo(problem.TotalWorkAmount - availableTime * speed).Within(Epsilon));
		}



		private static ExecutorsSelectionProblem createDefaultProblem()
		{
			var problem = new ExecutorsSelectionProblem
			{
				TotalWorkAmount = 100,

				PaymentRates = new[] { 1d, 1d },
				WorkQualities = new[] { 0.5, 0.5 },

				AvailableTimes = new[] { 1000d, 1000d },
				WorkSpeeds = new[] { 1d, 1d },

				DeltaRate = 0.1,
				DeltaQuality = 0.01,

				MaxCost = 5000,
				MinQuality = 0.5,

				WorkStages = new[] { 0, 0 }
			};

			return problem;
		}
	}
}