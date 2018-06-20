namespace ExecutorsSelection
{
	public class MarketBehaviour
	{
		public double ClockIntervalHours { get; set; } = 0.1;
		public double NewProjectProbability { get; set; } = 0.5; // 0.75 * 8 / 0.1 ~ 40 projects / day
	}
}