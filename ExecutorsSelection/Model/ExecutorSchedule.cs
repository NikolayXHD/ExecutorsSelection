using System;
using System.Collections.Generic;

namespace ExecutorsSelection
{
	public class ExecutorSchedule
	{
		public void AddBuzyTime(double now, double duration, int projectId)
		{
			double start = BuzyHours.Count == 0
				? now
				: BuzyHours[BuzyHours.Count - 1].End;

			var segment = new BusyTimeSegment
			{
				Start = start,
				End = start + duration,
				ProjectId = projectId
			};

			BuzyHours.Add(segment);
		}

		public double GetAvailableTime(double now, double duration)
		{
			double freeFrom = getMinNonScheduledTime(now);
			var result = now + duration - freeFrom;

			return result;
		}

		public double GetScheduledTime(double now)
		{
			double freeFrom = getMinNonScheduledTime(now);
			return freeFrom - now;
		}

		public double GetBusyTimeInPast(double now, double duration)
		{
			double start = now - duration;

			double busyTime = 0;
			for (int i = BuzyHours.Count - 1; i >= 0; i--)
			{
				var period = BuzyHours[i];

				if (period.Start >= now)
					continue;

				if (period.End <= start)
					break;

				double delta = Math.Min(now, period.End) - Math.Max(start, period.Start);
				busyTime += delta;
			}

			return busyTime;
		}

		private double getMinNonScheduledTime(double now)
		{
			double freeFrom;
			if (BuzyHours.Count == 0)
				freeFrom = now;
			else
				freeFrom = BuzyHours[BuzyHours.Count - 1].End;

			return Math.Max(freeFrom, now);
		}

		public List<BusyTimeSegment> BuzyHours { get; set; } = new List<BusyTimeSegment>();
	}
}