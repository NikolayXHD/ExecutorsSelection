using System.Text;

namespace ExecutorsSelection
{
	public class Plan
	{
		public double Quality { get; set; }
		public double Cost { get; set; }
		public double Score { get; set; }

		public double Time { get; set; }
		public PlanSubtotalsByStage[] StageSubtotals { get; set; }

		public ProjectAssignment[] Executors { get; set; }

		public override string ToString()
		{
			var builder = new StringBuilder();

			builder
				.Append("Time: ")
				.AppendLine(Time.Format2())
				.Append("Quality: ")
				.AppendLine(Quality.Format2())
				.Append("Cost: ")
				.AppendLine(Cost.Format2())
				.Append("Score: ")
				.AppendLine(Score.Format2())
				.AppendLine();

			for (int i = 0; i < Executors.Length; i++)
			{
				if (Executors[i].Pages > 0)
				{
					builder.Append("[ ");
					builder.Append(Executors[i].Pages.Format2());
					builder.Append(" pages ] ");
					builder.AppendLine(Executors[i].ToString());
				}
			}

			return builder.ToString();
		}
	}

	public class PlanSubtotalsByStage
	{
		public string Stage { get; set; }
		public int ExecutorsNumber { get; set; }
		public double Time { get; set; }
	}
}