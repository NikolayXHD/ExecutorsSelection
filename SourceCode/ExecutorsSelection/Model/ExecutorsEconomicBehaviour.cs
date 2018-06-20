namespace ExecutorsSelection
{
	public class ExecutorsEconomicBehaviour
	{
		public int PaymentRateUpdatingIntervalHours { get; set; } = 8 * 3;

		public double BusinessPeriodToEstimate { get; set; } = 8 * 4;
		public double BusinessRateToIncreasePayment { get; set; } = 0.8;
		public double BusinessRateToDecreasePayment { get; set; } = 0.01;
		public double IncrementMultiplier { get; set; } = 1.1; // 10 percent

		public void UpdatePaymentRate(ExecutorProfile executor, int now)
		{
			double period = BusinessPeriodToEstimate;
			var busyTime = executor.Schedule.GetBusyTimeInPast(now, period);

			var businessRate = busyTime / period;

			double multiplier;

			if (businessRate < BusinessRateToDecreasePayment)
				multiplier = 1 / IncrementMultiplier;
			else if (businessRate > BusinessRateToIncreasePayment)
				multiplier = IncrementMultiplier;
			else
				return;

			executor.PaymentRatePerPage *= multiplier;
			executor.PaymentRateHistory.Add(new PaymentRateSnapshot
			{
				Hour = now,
				Value = executor.PaymentRatePerPage
			});
		}
	}
}