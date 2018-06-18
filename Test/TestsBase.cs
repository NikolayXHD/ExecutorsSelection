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
			_log.Debug($"{title}: " + string.Join(" ",
				values.Select(NumericFormats.Format)));

		protected static void Log(string title, double value) =>
			_log.Debug($"{title}: {value.Format()}");

		protected static void Log(string title, bool value) =>
			_log.Debug($"{title}: {value.Format()}");

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

		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		protected const double Epsilon = 1e-5;
	}
}