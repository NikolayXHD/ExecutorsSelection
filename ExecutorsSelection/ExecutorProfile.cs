using MathNet.Numerics.Distributions;

namespace ExecutorsSelection
{
	public class ExecutorProfile
	{
		public string Name { get; set; }

		public double WorkUnitsPerHour { get;set; }
		public double PaymentRatePerWorkUnit { get; set; }
		public int WorkStage { get; set; }
		public double Quality { get; set; }

		public override string ToString()
		{
			return $"{Name}: {_stageNames[WorkStage]} {(Quality * 100).Format()}% ${PaymentRatePerWorkUnit.Format()}/pg {WorkUnitsPerHour.Format()} pg/hr";
		}

		private static readonly string[] _stageNames = 
		{
			"Translate",
			"Edit",
			"Correct"
		};
	}

	public class ExecutorProfileFactory
	{
		private Gamma getSpeedDistribution(int stageNumber)
		{
			var result = _speedDistributionByStage[stageNumber];
			if (result != null)
				return result;

			var speedAverage = AverageWordsPerDayByStage[stageNumber] / WordsPerPage / HoursPerDay;
			var speedVariance = speedAverage * 0.5;

			_speedDistributionByStage[stageNumber] = createGamma(speedAverage, speedVariance);
			return _speedDistributionByStage[stageNumber];
		}

		private static Gamma createGamma(double average, double variance) =>
			new Gamma(average * average / variance, average / variance);

		private Gamma getPaymentRateDistribution()
		{
			if (_paymentRateDistribution != null)
				return _paymentRateDistribution;

			var rateAverage = AveragePaymentRatePerWord * WordsPerPage;
			var rateVariance = rateAverage * 0.5;

			_paymentRateDistribution = createGamma(rateAverage, rateVariance);
			return _paymentRateDistribution;
		}

		private Normal getQualityDistribution()
		{
			if (_qualityDistribution != null)
				return _qualityDistribution;

			_qualityDistribution = new Normal(QualityAverage, QualityVariance);
			return _qualityDistribution;
		}

		public double[] AverageWordsPerDayByStage { get; set; } = { 2000, 4000, 12000 };
		
		public double AveragePaymentRatePerWord { get; set; } = 0.01;
		public double QualityAverage { get; set; } = 0.8;
		public double QualityVariance { get; set; } = 0.2;

		public const double WordsPerPage = 250;
		public const double HoursPerDay = 8;

		private readonly Gamma[] _speedDistributionByStage = new Gamma[3];
		private Gamma _paymentRateDistribution;
		private Normal _qualityDistribution;
	}
}