using System.Collections.Generic;

namespace ExecutorsSelection
{
	public class Project
	{
		public int Id { get; set; }

		public double TotalWorkPages { get; set; }

		public double? MaxTime { get; set; }
		
		public double MinQuality { get;set; }
		public double MaxCost { get; set; }

		/// <summary>
		/// Quality importance.
		/// <see cref="DeltaCost"/> is such an increase of project cost that
		/// when quality changes by <see cref="DeltaQuality"/> 
		/// it keeps utility function (whose maximum we are seeking) unchanged
		/// </summary>
		public double DeltaCost { get; set; }

		/// <summary>
		/// Cost importance.
		/// <see cref="DeltaQuality"/> is such an increase of quality that
		/// when project cost changes by <see cref="DeltaCost"/> 
		/// it keeps utility function (whose maximum we are seeking) unchanged
		/// </summary>
		public double DeltaQuality { get; set; }

		public Plan[] Plans { get; set; }

		public List<Plan> RejectedPlans { get; set; }
	}
}