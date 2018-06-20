using System;
using System.Linq;
using ExecutorsSelection;
using NLog;
using NUnit.Framework;

namespace Test
{
	public class TestsBase
	{
		[SetUp]
		public void Setup() =>
			Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;

		[TearDown]
		public void Teardown() =>
			LogManager.Flush();

		protected static void Log(string title, double[] values) =>
			Logger.Debug($"{title}: " + string.Join(" ",
				values.Select(NumericFormats.Format)));

		protected static void Log(string title, double value) =>
			Logger.Debug($"{title}: {value.Format()}");

		protected static void Log(string title, bool value) =>
			Logger.Debug($"{title}: {value.Format()}");

		protected static void Log(ExecutorsSelectionProblem.Solution solution)
		{
			if (solution.IsUnbound)
			{
				Log("Is UNbound", solution.IsUnbound);
				return;
			}

			if (solution.IsInfeasible)
			{
				Log("Is INfeasible", solution.IsInfeasible);
				return;
			}

			Log("Optimal work distribution", solution.WorkDistribution);
			Log("Total cost", solution.TotalCost);
			Log("Average quality", solution.AverageQuality);
		}

		protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		protected const double Epsilon = 1e-5;
	}
}