﻿using System;
using System.Collections.Generic;

namespace ExecutorsSelection
{
	public class ExecutorProfileFactory
	{
		public ExecutorProfile CreateRandomProfile(int stageNumber)
		{
			double rate = getRandomPaymentPerPageRate();

			return new ExecutorProfile
			{
				Name = getRandomName(),
				WorkStage = stageNumber,
				Quality = getRandomQuality(stageNumber),
				PagesPerHour = getRandomPagesPerHourSpeed(stageNumber),
				PaymentRatePerPage = rate,
				PaymentRateHistory = new List<PaymentRateSnapshot>
				{
					new PaymentRateSnapshot { Hour = 0, Value = rate }
				}
			};
		}

		private double getRandomPagesPerHourSpeed(int stageNumber) =>
			RandomUtil.NextDoubleGamma05(AverageWordsPerDayByStage[stageNumber] / WordsPerPage / HoursPerDay);

		private double getRandomQuality(int stageNumber)
		{
			double min = MinQualityPerStage[stageNumber];
			double max = MaxQualityPerStage[stageNumber];

			var result = max - RandomUtil.NextDoubleGamma05(1 - QualityAverage) * (max - min);
			return Math.Max(0d, result);
		}

		private double getRandomPaymentPerPageRate() =>
			RandomUtil.NextDoubleGamma05(AveragePaymentRatePerWord * WordsPerPage);

		private static string getRandomName()
		{
			var result = HumanNames.Names.RandomElement() + " " + HumanNames.Surnames.RandomElement();
			return result;
		}

		public double[] AverageWordsPerDayByStage { get; set; } = { 4000, 2000, 4000, 12000 };
		public double[] MinQualityPerStage { get; set; } = { 0.5, 0.7, 0.9, 0.95 };
		public double[] MaxQualityPerStage { get; set; } = { 0.7, 0.9, 0.95, 1 };

		public double AveragePaymentRatePerWord { get; set; } = 0.01;
		public double QualityAverage { get; set; } = 0.8;


		public const double WordsPerPage = 250;
		public const double HoursPerDay = 8;
	}
}