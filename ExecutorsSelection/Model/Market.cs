using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;

namespace ExecutorsSelection
{
	public class Market
	{
		public Market()
		{
			_expectedPagesPerHourPerStage = new Lazy<double[]>(() => 
				ExecutorProfileFactory.AverageWordsPerDayByStage.Select(val =>
					val / ExecutorProfileFactory.HoursPerDay / ExecutorProfileFactory.WordsPerPage)
				.ToArray());
		}

		public void GenerateExecutorProfiles()
		{
			ExecutorsByStage = generateExecutorProfiles(
				ExecutorsCountPerStage, ExecutorProfileFactory);
		}

		public void GenerateProjects(int hours)
		{
			ProjectByCreationTimeInHours = generateProjects(
				hours,
				MarketBehaviour.ClockIntervalHours,
				MarketBehaviour.NewProjectProbability, ProjectFactory);
		}

		public void Run(int hours)
		{
			for (int h = 0; h < hours; h++)
			{
				simulateOneHour();
				HoursElapsed++;
			}
		}

		public PlanSelectionProblem CreatePlanSelectionProblem(Project project)
		{
			var problem = new PlanSelectionProblem
			{
				Project = project,
				ExecutorsByStage = ExecutorsByStage,
				ExpectedPagesPerHourByStage = ExpectedPagesPerHourPerStage,
				Now = HoursElapsed
			};

			return problem;
		}

		private void simulateOneHour()
		{
			var sw = new Stopwatch();
			sw.Start();

			if (ProjectByCreationTimeInHours.TryGetValue(HoursElapsed, out var projects))
				foreach (var project in projects)
				{
					var problem = CreatePlanSelectionProblem(project);
					var plans = problem.SuggestPlans();

					project.Plans = plans;

					if (plans.Length > 0)
						scheduleWork(plans[0], HoursElapsed, project.Id);
				}

			if (HoursElapsed > 0 && HoursElapsed % ExecutorsEconomicBehaviour.PaymentRateUpdatingIntervalHours == 0)
				updatePaymentRates(HoursElapsed);

			sw.Stop();

			_log.Debug($"Hour {HoursElapsed} modeled in {sw.ElapsedMilliseconds} ms");
		}

		private static void scheduleWork(Plan plan, double now, int projectId)
		{
			for (int i = 0; i < plan.Executors.Length; i++)
			{
				var executor = plan.Executors[i];
				var requiredTime = executor.Pages / executor.PagesPerHour;
				executor.Original.Schedule.AddBuzyTime(now, requiredTime, projectId);
			}
		}

		private void updatePaymentRates(int now)
		{
			foreach (var executors in ExecutorsByStage)
				foreach (var executor in executors)
					ExecutorsEconomicBehaviour.UpdatePaymentRate(executor, now);
		}

		

		private static HashSet<ExecutorProfile>[] generateExecutorProfiles(
			int[] executorsCountPerStage,
			ExecutorProfileFactory executorProfileFactory)
		{
			var result = Enumerable.Range(0, executorsCountPerStage.Length)
				.Select(s => new HashSet<ExecutorProfile>(
					Enumerable.Range(0, executorsCountPerStage[s])
						.Select(i => executorProfileFactory.CreateRandomProfile(s)))
				).ToArray();

			return result;
		}

		private static Dictionary<int, List<Project>> generateProjects(
			int hours,
			double clockInterval,
			double newProjectProbability,
			ProjectFactory projectFactory)
		{
			var result = new Dictionary<int, List<Project>>();

			for (double time = 0; time < hours; time += clockInterval)
				if (RandomUtil.NextDouble() < newProjectProbability)
				{
					int hour = (int) time;
					if (!result.TryGetValue(hour, out var projects))
					{
						projects = new List<Project>();
						result.Add(hour, projects);
					}

					var newProject = projectFactory.CreateRandomProject();
					projects.Add(newProject);
				}

			return result;
		}

		public ProjectFactory ProjectFactory { get; set; } =
			new ProjectFactory();
		
		public ExecutorProfileFactory ExecutorProfileFactory { get; set; } =
			new ExecutorProfileFactory();

		public ExecutorsEconomicBehaviour ExecutorsEconomicBehaviour { get; set; } =
			new ExecutorsEconomicBehaviour();
		
		public MarketBehaviour MarketBehaviour { get; set; } =
			new MarketBehaviour();

		public int[] ExecutorsCountPerStage { get; set; } =
			{ 50, 500, 150, 50 };

		public int HoursElapsed { get; set; }

		public HashSet<ExecutorProfile>[] ExecutorsByStage { get; set; }
		public Dictionary<int, List<Project>> ProjectByCreationTimeInHours { get; set; }


		public double[] ExpectedPagesPerHourPerStage
			=> _expectedPagesPerHourPerStage.Value;

		private readonly Lazy<double[]> _expectedPagesPerHourPerStage;

		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
	}
}