using System.Linq;
using ExecutorsSelection;
using NUnit.Framework;

namespace Test
{
	[TestFixture]
	public class TwoStageProblemTests : TestsBase
	{
		[Test]
		public void Default_problem_Has_two_stages()
		{
			var problem = createDefaultProblem();
			Assert.That(problem.StagesCount, Is.EqualTo(2));
		}

		[Test]
		public void TotalWorkAmount_is_observed_in_both_stages()
		{
			var problem = createDefaultProblem();

			var solution = problem.Solve();
			Log(solution);

			for (int s = 0; s < problem.StagesCount; s++)
			{
				var totalWorkOnStage = Enumerable.Range(0, problem.ExecutorsCount)
					.Where(i => problem.WorkStages[i] == s)
					.Sum(i => solution.WorkDistribution[i]);

				Assert.That(totalWorkOnStage, Is.EqualTo(problem.TotalWorkAmount));
			}

			Assert.That(solution.WorkDistribution[0] + solution.WorkDistribution[1], Is.EqualTo(problem.TotalWorkAmount).Within(Epsilon));
			Assert.That(solution.WorkDistribution[0] + solution.WorkDistribution[1], Is.EqualTo(problem.TotalWorkAmount).Within(Epsilon));
		}

		private static ExecutorsSelectionProblem createDefaultProblem()
		{
			var problem = new ExecutorsSelectionProblem
			{
				TotalWorkAmount = 100,

				PaymentRates = new[] { 1d, 1d, 1d, 1d },
				WorkQualities = new[] { 0, 0, 0.5, 0.5 },

				AvailableTimes = new[] { 1000d, 1000d, 1000d, 1000d },
				WorkSpeeds = new[] { 1d, 1d, 1d, 1d },

				DeltaRate = 0.1,
				DeltaQuality = 0.01,

				MaxCost = 5000,
				MinQuality = 0.5,

				WorkStages = new[] { 0, 0, 1, 1 }
			};

			return problem;
		}
	}
}