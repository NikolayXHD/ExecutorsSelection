namespace ExecutorsSelection
{
	public class ProjectFactory
	{
		public Project CreateRandomProject()
		{
			double totalWork = RandomUtil.NextDoubleGamma05(AveragePages);

			bool qualityIsNotImportant = RandomUtil.NextDouble() < QualityNotImportantProbability;
			bool costIsNotImportant = RandomUtil.NextDouble() < CostNotImportantProbability;
			bool costIsUnlimited = RandomUtil.NextDouble() < UnlimitedCostProbability;
			bool noDeadline = RandomUtil.NextDouble() < NoDeadlineProbability;

			double maxCostPerPage = RandomUtil.NextDoubleGamma05(AverageMaxCostPerPage);
			double hoursToDeadline = RandomUtil.NextDoubleGamma05(AverageHoursToDeadline);
			double minQuality = RandomUtil.NextDouble(MinQuality, MaxQuality);
			double deltaCost = RandomUtil.NextDoubleGamma05(AverageDeltaCost);

			var result = new Project
			{
				Id = IdCounter++,

				TotalWorkPages = totalWork,

				MaxCost = costIsUnlimited
					? 100 * AverageMaxCostPerPage
					: totalWork * maxCostPerPage,

				MaxTime = noDeadline
					? (double?) null
					: hoursToDeadline,

				MinQuality = qualityIsNotImportant
					? 0
					: minQuality,

				DeltaQuality = costIsNotImportant && !qualityIsNotImportant
					? 0
					: DeltaQuality,

				DeltaCost = qualityIsNotImportant && !costIsNotImportant
					? 0
					: deltaCost
			};

			return result;
		}

		public int IdCounter { get; set; }

		public double NoDeadlineProbability { get; set; } = 0.1;
		public double AverageHoursToDeadline { get; set; } = 8 * 2;
		public double AveragePages { get; set; } = 100;

		public double AverageMaxCostPerPage { get; set; } = 5;
		public double UnlimitedCostProbability { get; set; } = 0.05;

		public double MinQuality { get; set; } = 0.5;
		public double MaxQuality { get; set; } = 1;

		/// <summary>
		/// Quality importance.
		/// DeltaCost is such an increase of project cost that
		/// when quality changes by <see cref="DeltaQuality"/> 
		/// it keeps utility function (whose maximum we are seeking) unchanged
		/// </summary>
		public double AverageDeltaCost { get; set; } = 0.3;

		/// <summary>
		/// Cost importance.
		/// <see cref="DeltaQuality"/> is such an increase of quality that
		/// when project cost changes by DeltaCost
		/// it keeps utility function (whose maximum we are seeking) unchanged
		/// </summary>
		public double DeltaQuality { get; set; } = 0.01;

		public double QualityNotImportantProbability { get; set; } = 0.1;
		public double CostNotImportantProbability { get; set; } = 0.1;
	}
}