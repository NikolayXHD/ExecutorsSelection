using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExecutorsSelection
{
	public class ExecutorProfile
	{
		public string Name { get; set; }

		public double PagesPerHour { get;set; }
		public double PaymentRatePerPage { get; set; }
		public int WorkStage { get; set; }
		public double Quality { get; set; }

		public ExecutorSchedule Schedule { get; set; } = new ExecutorSchedule();

		public List<PaymentRateSnapshot> PaymentRateHistory { get; set; }

		public ProjectAssignment CreateAssignment(double pagesCount)
		{
			return new ProjectAssignment
			{
				Name = Name,
				Pages = pagesCount,
				PagesPerHour = PagesPerHour,
				PaymentRatePerPage = PaymentRatePerPage,
				WorkStage = WorkStage,
				Quality = Quality,
				Original = this
			};
		}

		public override string ToString()
		{
			return $"{Name}: {WorkStageNames.Names[WorkStage]} {(Quality * 100).Format2()}% ${PaymentRatePerPage.Format2()}/pg {PagesPerHour.Format2()} pg/hr";
		}
	}

	public class ProjectAssignment
	{
		public string Name { get; set; }

		public double Pages { get; set; }

		public double PagesPerHour { get;set; }
		public double PaymentRatePerPage { get; set; }
		public int WorkStage { get; set; }
		public double Quality { get; set; }

		[JsonIgnore]
		public double Time => Pages / PagesPerHour;

		[JsonIgnore]
		public ExecutorProfile Original { get; set; }
	}
}