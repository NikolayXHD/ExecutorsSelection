using System.Diagnostics;
using System.IO;
using System.Text;
using ExecutorsSelection;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Test
{
	[TestFixture]
	public class MarketTests: TestsBase
	{
		[Test, Order(0)]
		public void Create_market_template()
		{
			var market = new Market();
			saveMarketToFile(market, MarketTemplateFileName);
		}

		[Test, Order(1)]
		public void Generate_executor_profiles()
		{
			var market = loadMarketFromFile(MarketTemplateFileName);
			market.GenerateExecutorProfiles();
			saveMarketToFile(market, MarketWithExecutorsFileName);
		}

		[Test, Order(2)]
		public void Simulate_market_history()
		{
			var market = loadMarketFromFile(MarketWithExecutorsFileName);

			int savePeriod = 8;
			int periodsCount = 50;
			
			market.GenerateProjects(savePeriod * periodsCount);

			for (int i = 0; i < periodsCount; i++)
			{
				var sw = new Stopwatch();
				sw.Start();

				market.Run(savePeriod);

				sw.Stop();
				Logger.Debug($"{savePeriod} hours modeled in {sw.ElapsedMilliseconds} ms");

				saveMarketToFile(market, $"market_history_{savePeriod * i}.json");
			}

			saveMarketToFile(market, MarketFileName);
		}

		[Test, Order(3)]
		public void Create_executor_profiles_csv_From_simulated_market_history()
		{
			var market = loadMarketFromFile(MarketFileName);
			
			const char tab = '	';
			var builder = new StringBuilder();

			builder.Append("Name	Stage	Quality %	Speed Pages/Hour	Business rate %	Scheduled hours");
			for (int h = 0; h < market.HoursElapsed; h++)
				if (h % market.ExecutorsEconomicBehaviour.PaymentRateUpdatingIntervalHours == 0)
					builder.Append(tab).Append("Rate_hr_").Append(h.Format());

			builder.AppendLine();

			foreach (var executors in market.ExecutorsByStage)
				foreach (var profile in executors)
				{
					double scheduledTime = profile.Schedule.GetScheduledTime(market.HoursElapsed);
					double busyTime = profile.Schedule.GetBusyTimeInPast(market.HoursElapsed, market.HoursElapsed);
					double businessRate = busyTime / market.HoursElapsed;

					builder
						.Append(profile.Name).Append(tab)
						.Append(WorkStageNames.Names[profile.WorkStage]).Append(tab)
						.Append((profile.Quality * 100).Format()).Append(tab)
						.Append(profile.PagesPerHour.Format()).Append(tab)
						.Append((businessRate * 100).Format()).Append(tab)
						.Append(scheduledTime.Format());

					var rateIndex = -1;
					var rateHistory = profile.PaymentRateHistory;

					for (int h = 0; h < market.HoursElapsed; h++)
					{
						if (rateIndex < rateHistory.Count - 1 && h == rateHistory[rateIndex + 1].Hour)
							rateIndex++;

						if (h % market.ExecutorsEconomicBehaviour.PaymentRateUpdatingIntervalHours == 0)
						{
							double rate = rateHistory[rateIndex].Value;
							builder.Append(tab).Append(rate.Format());
						}
					}

					builder.AppendLine();
				}

			saveProfilesToFile(builder);
		}

		[Test, Order(3)]
		public void Suggest_plan_for_a_project_With_large_volume_And_no_max_cost_And_tight_deadline(
			[Values(1000, 2000, 4000)] double totalPages,
			[Values(4, 8, 12)] double hoursToDeadline)
		{
			var market = loadMarketFromFile(MarketFileName);
			var projectFactory = market.ProjectFactory;
			var executorFactory = market.ExecutorProfileFactory;

			var project = new Project
			{
				Id = 1,
				TotalWorkPages = totalPages,
				MaxTime = hoursToDeadline,

				MaxCost = totalPages * projectFactory.AverageMaxCostPerPage * 1000,

				DeltaQuality = projectFactory.DeltaQuality,
				DeltaCost = projectFactory.AverageDeltaCost,
				MinQuality = executorFactory.MinQualityPerStage[1] // translator
			};

			var planSelectionProblem = market.CreatePlanSelectionProblem(project);
			var plans = planSelectionProblem.SuggestPlans();

			var fileName = $"plans_{totalPages}_pg_{hoursToDeadline}_hrs.json";
			savePlansToFile(plans, fileName);
		}

		private static void savePlansToFile(Plan[] plans, string file)
		{
			string serialized = JsonConvert.SerializeObject(plans, Formatting.Indented);

			var path = Path.GetFullPath(file);
			Logger.Debug($"Saving plans to: {path}");
			File.WriteAllText(path, serialized);
		}

		private static void saveMarketToFile(Market market, string file)
		{
			string serialized = JsonConvert.SerializeObject(market, Formatting.Indented);

			var path = Path.GetFullPath(file);
			Logger.Debug($"Saving market to: {path}");
			File.WriteAllText(path, serialized);
		}

		private static void saveProfilesToFile(StringBuilder builder)
		{
			var path = Path.GetFullPath(ProfilesFileName);
			Logger.Debug($"Saving profiles to: {path}");

			File.WriteAllText(path, builder.ToString());
		}

		private static Market loadMarketFromFile(string file)
		{
			var text = File.ReadAllText(file);
			var market = JsonConvert.DeserializeObject<Market>(text);
			return market;
		}



		private const string MarketTemplateFileName = "market_template.json";
		private const string MarketWithExecutorsFileName = "market_template_with_executors.json";

		private const string MarketFileName = "market_history.json";
		private const string ProfilesFileName = "profiles_history.csv";
	}
}