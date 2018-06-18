namespace ExecutorsSelection
{
	public class PlanSelectionProblem
	{
		public double? MaxTime { get; set; }
		
		public double MinQuality { get;set; }
		public double MaxCost { get; set; }

		/// <summary>
		/// <see cref="DeltaCost"/> is such an increase of project cost that
		/// when quality changes by <see cref="DeltaQuality"/> 
		/// it keeps utility function (whose maximum we are seeking) unchanged
		/// </summary>
		public double DeltaCost { get; set; }

		/// <summary>
		/// <see cref="DeltaQuality"/> is such an increase of quality that
		/// when project cost changes by <see cref="DeltaCost"/> 
		/// it keeps utility function (whose maximum we are seeking) unchanged
		/// </summary>
		public double DeltaQuality { get; set; }
	}
}