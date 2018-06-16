using System;
using System.Collections.Generic;

namespace ExecutorsSelection
{
	/// <summary>
	/// Основная Задача Линейного программирования 
	/// <para>Transpose(A) * x &lt;= b</para>
	/// <para>cx -> max</para>
	/// </summary>
	public class LinearProgrammingProblem
	{
		private readonly double[] _c;
		private readonly double[,] _a;
		private readonly double[] _b;
		private readonly HashSet<int> _nonBasicSet = new HashSet<int>();
		private readonly HashSet<int> _basicSet = new HashSet<int>();
		private double _v;

		/// <summary>
		/// Создать Основную Задачу Линейного программирования 
		/// <para>Transpose(A) * x &lt;= b</para>
		/// <para>cx -> max</para>
		/// </summary>
		/// <param name="a">Матрица размерности m x n</param>
		/// <param name="b">Вектор длины m</param> 
		/// <param name="c">Вектор длины n - коеффициенты максимизируемой линейной целевой функции</param>
		public LinearProgrammingProblem(double[] b, double[] c, double[,] a)
		{
			int bLen = b.Length;
			int cLen = c.Length;

			if (bLen != a.GetLength(0))
				throw new ArgumentException("Number of constraints in A doesn't match number in b.");

			if (cLen != a.GetLength(1))
				throw new ArgumentException("Number of variables in c doesn't match number in A.");

			// Extend max fn coefficients vector with 0 padding
			_c = new double[cLen + bLen];
			Array.Copy(c, _c, cLen);

			// Extend coefficient matrix with 0 padding
			_a = new double[cLen + bLen, cLen + bLen];

			for (int i = 0; i < bLen; i++)
				for (int j = 0; j < cLen; j++)
					_a[i + cLen, j] = a[i, j];

			// Extend constraint right-hand side vector with 0 padding
			_b = new double[cLen + bLen];
			Array.Copy(b, 0, _b, cLen, bLen);

			// Populate non-basic and basic sets
			for (int j = 0; j < cLen; j++)
				_nonBasicSet.Add(j);

			for (int i = 0; i < bLen; i++)
				_basicSet.Add(cLen + i);
		}

		public (double Value, double[] Vector) FindMaximum()
		{
			while (true)
			{
				// Find highest coefficient for entering var
				int enteringIndex = int.MinValue;
				double cEntering = 0;

				foreach (var j in _nonBasicSet)
				{
					if (_c[j] > cEntering)
					{
						cEntering = _c[j];
						enteringIndex = j;
					}
				}

				// If no coefficient > 0, there's no more maximizing to do, and we're almost done
				if (enteringIndex == int.MinValue)
					break;

				// Find lowest check ratio
				double minRatio = double.PositiveInfinity;
				int leavingIndex = int.MinValue;

				foreach (var i in _basicSet)
				{
					if (_a[i, enteringIndex] > 0)
					{
						double r = _b[i] / _a[i, enteringIndex];
						if (r < minRatio)
						{
							minRatio = r;
							leavingIndex = i;
						}
					}
				}

				// Unbounded
				if (double.IsPositiveInfinity(minRatio))
					return (minRatio, null);

				pivot(enteringIndex, leavingIndex);
			}

			// Extract amounts and slack for optimal solution
			int n = _b.Length;
			var x = new double[n];

			foreach (int i in _basicSet)
				x[i] = _b[i];

			/*for (var i = 0; i < n; i++)
				if (_basicSet.Contains(i))
					x[i] = _b[i];
				else
					x[i] = 0;*/

			// Return max and variables
			return (_v, x);
		}

		private void pivot(int enteringIndex, int leavingIndex)
		{
			_nonBasicSet.Remove(enteringIndex);
			_basicSet.Remove(leavingIndex);

			_b[enteringIndex] = _b[leavingIndex] / _a[leavingIndex, enteringIndex];

			foreach (int j in _nonBasicSet)
				_a[enteringIndex, j] = _a[leavingIndex, j] / _a[leavingIndex, enteringIndex];

			_a[enteringIndex, leavingIndex] = 1 / _a[leavingIndex, enteringIndex];

			foreach (int i in _basicSet)
			{
				_b[i] = _b[i] - _a[i, enteringIndex] * _b[enteringIndex];

				foreach (int j in _nonBasicSet)
					_a[i, j] = _a[i, j] - _a[i, enteringIndex] * _a[enteringIndex, j];

				_a[i, leavingIndex] = -1 * _a[i, enteringIndex] * _a[enteringIndex, leavingIndex];
			}

			_v = _v + _c[enteringIndex] * _b[enteringIndex];

			foreach (int j in _nonBasicSet)
				_c[j] = _c[j] - _c[enteringIndex] * _a[enteringIndex, j];

			_c[leavingIndex] = -1 * _c[enteringIndex] * _a[enteringIndex, leavingIndex];

			_nonBasicSet.Add(leavingIndex);
			_basicSet.Add(enteringIndex);
		}
	}
}