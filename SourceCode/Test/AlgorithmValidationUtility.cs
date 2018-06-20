using System.Diagnostics;
using System.IO;
using System.Text;
using ExecutorsSelection;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Test
{
	[TestFixture]
	public class AlgorithmValidationUtility : TestsBase
	{
		[SetUp]
		public void SetupDirectory()
		{
			Directory.CreateDirectory(OutputDirectory);
		}

		[Test, Order(0)]
		public void Step_0_Create_market_template()
		{
			var market = new Market();
			saveMarketToFile(market, MarketTemplateFileName);
		}

		[Test, Order(1)]
		public void Step_1_Generate_executor_profiles()
		{
			var market = loadMarketFromFile(MarketTemplateFileName);
			market.GenerateExecutorProfiles();
			saveMarketToFile(market, MarketWithExecutorsTemplateFileName);
		}

		[Test, Order(2)]
		public void Step_3_Start_market_history_simulation()
		{
			simulateHistoryFrom(
				MarketWithExecutorsTemplateFileName,
				saveInterval: 8,
				saveIntervalsCount: 50);
		}

		[Test, Order(3), Explicit]
		public void Step_4_Optional_Continue_market_history_simulation()
		{
			simulateHistoryFrom(
				MarketFileName,
				saveInterval: 8,
				saveIntervalsCount: 50);
		}

		[Test, Order(3)]
		public void Step_4_Optional_Create_executor_profiles_csv()
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
		public void Step_5_1_Suggest_plan_for_a_project_With_large_volume_And_no_max_cost_And_tight_deadline(
			[Values(100, 150, 300)] double totalPages,
			[Values(10, 15, 30)] double minutesToDeadline)
		{
			var market = loadMarketFromFile(MarketFileName);
			var projectFactory = market.ProjectFactory;

			var project = new Project
			{
				Id = 1,
				TotalWorkPages = totalPages,
				MaxTime = minutesToDeadline / 60,

				MaxCost = totalPages * projectFactory.AverageMaxCostPerPage * 1000,

				DeltaQuality = projectFactory.DeltaQuality,
				DeltaCost = projectFactory.AverageDeltaCost,
				MinQuality = market.ExecutorProfileFactory.MinQualityPerStage[1], // translator
			};

			setPlans(market, project);
			saveProjectToFile(project, $"project_{totalPages}_pg_{minutesToDeadline}_mins.json");
		}

		[Test, Order(3)]
		public void Step_5_2_Suggest_plan_for_a_project_With_large_volume_And_no_max_cost_And_no_min_quality(
			[Values(1000, 4000, 16000)] double totalPages)
		{
			var market = loadMarketFromFile(MarketFileName);
			var projectFactory = market.ProjectFactory;

			var project = new Project
			{
				Id = 1,
				TotalWorkPages = totalPages,
				MaxTime = null,

				MaxCost = totalPages * projectFactory.AverageMaxCostPerPage * 1000,

				DeltaQuality = 0.1,
				// it's ok to pay the the editor +$1 per page to increase Q by 0.1
				// but too expensive to pay the corrector $1 more per page to further increase Q by 0.05
				DeltaCost = 1.8,
				MinQuality = 0
			};

			setPlans(market, project);
			saveProjectToFile(project, $"project_{totalPages}_pg.json");
		}

		[Test, Order(3)]
		public void Step_5_3_Suggest_plan_for_a_project_That_is_small_And_urgent(
			[Values(1, 2, 3)] double totalPages,
			[Values(1, 2, 4)] double hoursToDeadline)
		{
			var market = loadMarketFromFile(MarketFileName);
			var projectFactory = market.ProjectFactory;

			var project = new Project
			{
				Id = 1,
				TotalWorkPages = totalPages,
				MaxTime = hoursToDeadline,
				MaxCost = totalPages * 4,

				DeltaQuality = projectFactory.DeltaQuality,
				DeltaCost = projectFactory.AverageDeltaCost,
				MinQuality = 0.8, // translator
			};

			setPlans(market, project);
			saveProjectToFile(project, $"project_{totalPages}_pg_{hoursToDeadline}_hours.json");
		}

		[Test, Order(3)]
		public void Step_5_4_Suggest_plan_for_a_project_With_high_min_quality(
			[Values(10, 50, 100)] double totalPages,
			[Values(895)] int minQualityPercent
			)
		{
			var market = loadMarketFromFile(MarketFileName);
			var projectFactory = market.ProjectFactory;

			var project = new Project
			{
				Id = 1,
				TotalWorkPages = totalPages,
				MaxTime = null,
				MaxCost = totalPages * projectFactory.AverageMaxCostPerPage,

				DeltaQuality = projectFactory.DeltaQuality,
				DeltaCost = projectFactory.AverageDeltaCost * 0.8,
				MinQuality = minQualityPercent / 1000d
			};

			setPlans(market, project);
			saveProjectToFile(project, $"project_{totalPages}_pg_{minQualityPercent}_promile.json");
		}

		private static void setPlans(Market market, Project project)
		{
			var planSelectionProblem = market.CreatePlanSelectionProblem(project);
			planSelectionProblem.OnePlanPerTimeLimit = true;

			var plans = planSelectionProblem.SuggestPlans(out var rejectedPlans);

			project.Plans = plans;
			project.RejectedPlans = rejectedPlans;
		}

		private static void simulateHistoryFrom(string fileName, int saveInterval, int saveIntervalsCount)
		{
			var market = loadMarketFromFile(fileName);

			market.GenerateProjects(saveInterval * saveIntervalsCount);

			for (int i = 0; i < saveIntervalsCount; i++)
			{
				var sw = new Stopwatch();
				sw.Start();

				market.Run(saveInterval);

				sw.Stop();
				Logger.Debug($"{saveInterval} hours modeled in {sw.ElapsedMilliseconds} ms");

				saveMarketToFile(market, $"market_history_{market.HoursElapsed}.json");
			}

			saveMarketToFile(market, MarketFileName);
		}



		private static void saveProjectToFile(Project project, string file)
		{
			string serialized = serialize(project);

			var path = getFullPath(file);
			Logger.Debug($"Saving plans to: {path}");
			File.WriteAllText(path, serialized);
		}

		private static void saveMarketToFile(Market market, string file)
		{
			string serialized = serialize(market);

			var path = getFullPath(file);
			Logger.Debug($"Saving market to: {path}");
			File.WriteAllText(path, serialized);
		}

		private static void saveProfilesToFile(StringBuilder builder)
		{
			var path = getFullPath(ProfilesFileName);
			Logger.Debug($"Saving profiles to: {path}");

			File.WriteAllText(path, builder.ToString());
		}

		private static Market loadMarketFromFile(string file)
		{
			var path = getFullPath(file);
			var text = File.ReadAllText(path);
			var market = JsonConvert.DeserializeObject<Market>(text);
			return market;
		}

		private static string getFullPath(string file) =>
			Path.GetFullPath(Path.Combine(OutputDirectory, file));

		private static string serialize(object project) =>
			JsonConvert.SerializeObject(project, Formatting.Indented);



		private const string MarketTemplateFileName = "market_template.json";
		private const string MarketWithExecutorsTemplateFileName = "market_template_with_executors.json";

		private const string MarketFileName = "market_history.json";
		private const string ProfilesFileName = "profiles_history.csv";
		private const string OutputDirectory = "output";
	}
}