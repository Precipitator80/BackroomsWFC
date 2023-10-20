﻿/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Collections;
using Core.Data;
using Core.Model;
using UnityEngine;
using Random = System.Random;

public abstract class Model3d<PARAM> : IModel3d where PARAM : WaveFunctionCollapseModelParams
{
	public const int DIRECTIONS_AMOUNT = 6;

	protected bool[][] wave;

	protected int[][][] propagator;
	int[][][] compatible;
	protected int[] observed;

	Eppy.Tuple<int, int>[] stack;
	int stacksize;

	protected Random random;
	protected readonly int FMX;
	protected readonly int FMY;
	protected readonly int FMZ;
	protected int T;
	protected bool periodic;

	protected double[] weights;
	double[] weightLogWeights;

	int[] sumsOfOnes;
	double sumOfWeights, sumOfWeightLogWeights, startingEntropy;
	double[] sumsOfWeights, sumsOfWeightLogWeights, entropies;

	public WaveFunctionCollapseModelParams ModelParam { get; private set; }

	protected Model3d(PARAM modelParam)
	{
		this.ModelParam = modelParam;

		FMX = modelParam.Width;
		FMY = modelParam.Height;
		FMZ = modelParam.Depth;
	}

	public bool Run(int seed, int limit)
	{
		if (wave == null) Init();

		Clear();
		random = new Random(seed);

		for (int l = 0; l < limit || limit == 0; l++)
		{
			bool? result = Observe();
			if (result != null) return (bool)result;
			Propagate();
		}

		return true;
	}

	public IEnumerator RunViaEnumerator(int seed, int limit, Action<bool> resultCallback, Action<bool[][]> iterationCallback)
	{
		Debug.Log("Running WFC via enumerator");
		if (wave == null) Init();

		Clear();
		if (seed != 0)
		{
			random = new Random(seed);
		}
		else
		{
			random = new Random();
		}


		for (int l = 0; l < limit || limit == 0; l++)
		{
			Debug.Log("Starting iteration step.");
			bool? result = Observe();
			if (result != null)
			{
				Debug.Log("Found result. Doing resultCallback.");
				resultCallback(result.Value);
				yield break;
			}
			Debug.Log("Did not find result. Propagating with iterationCallback. Iteration number: " + l);
			Propagate();
			iterationCallback(wave);
			Debug.Log("Finished iteration callback");
			//yield return null; // Seems to cause this to take ages in the editor.
		}

		resultCallback(false);
	}

	private void Init()
	{
		Debug.Log("Initialising wave function");
		wave = new bool[FMX * FMY * FMZ][];
		compatible = new int[wave.Length][][];
		for (int i = 0; i < wave.Length; i++)
		{
			wave[i] = new bool[T];
			compatible[i] = new int[T][];
			for (int t = 0; t < T; t++) compatible[i][t] = new int[DIRECTIONS_AMOUNT];
		}

		weightLogWeights = new double[T];
		sumOfWeights = 0;
		sumOfWeightLogWeights = 0;

		for (int t = 0; t < T; t++)
		{
			weightLogWeights[t] = weights[t] * Math.Log(weights[t]);
			sumOfWeights += weights[t];
			sumOfWeightLogWeights += weightLogWeights[t];
		}

		startingEntropy = Math.Log(sumOfWeights) - sumOfWeightLogWeights / sumOfWeights;

		sumsOfOnes = new int[FMX * FMY * FMZ];
		sumsOfWeights = new double[FMX * FMY * FMZ];
		sumsOfWeightLogWeights = new double[FMX * FMY * FMZ];
		entropies = new double[FMX * FMY * FMZ];

		stack = new Eppy.Tuple<int, int>[wave.Length * T];
		stacksize = 0;
	}

	bool? Observe()
	{
		Debug.Log("Observing");
		double min = 1E+3;
		int argmin = -1;

		for (int i = 0; i < wave.Length; i++)
		{
			int x, y, z;
			To3D(i, out x, out y, out z);
			if (OnBoundary(x, y, z)) continue;

			int amount = sumsOfOnes[i];
			if (amount == 0) return false;

			double entropy = entropies[i];
			if (amount > 1 && entropy <= min)
			{
				double noise = 1E-6 * random.NextDouble();
				if (entropy + noise < min)
				{
					min = entropy + noise;
					argmin = i;
				}
			}
		}

		if (argmin == -1)
		{
			observed = new int[FMX * FMY * FMZ];
			for (int i = 0; i < wave.Length; i++)
			{
				for (int t = 0; t < T; t++)
				{
					if (wave[i][t])
					{
						observed[i] = t; break;
					}
				}
			}
			return true;
		}

		double[] distribution = new double[T];
		for (int t = 0; t < T; t++) distribution[t] = wave[argmin][t] ? weights[t] : 0;
		int r = distribution.Random(random.NextDouble());

		bool[] w = wave[argmin];
		for (int t = 0; t < T; t++) if (w[t] != (t == r)) Ban(argmin, t);

		return null;
	}

	protected void Propagate()
	{
		while (stacksize > 0)
		{
			var e1 = stack[stacksize - 1];
			stacksize--;

			int i1 = e1.Item1;
			int x1, y1, z1;
			To3D(i1, out x1, out y1, out z1);
			bool[] w1 = wave[i1];

			for (int d = 0; d < DIRECTIONS_AMOUNT; d++)
			{
				int dx = DX[d], dy = DY[d], dz = DZ[d];
				int x2 = x1 + dx, y2 = y1 + dy, z2 = z1 + dz;
				if (OnBoundary(x2, y2, z2)) continue;

				if (x2 < 0) x2 += FMX;
				else if (x2 >= FMX) x2 -= FMX;
				if (y2 < 0) y2 += FMY;
				else if (y2 >= FMY) y2 -= FMY;
				if (z2 < 0) z2 += FMY;
				else if (z2 >= FMZ) z2 -= FMZ;

				int i2 = To1D(x2, y2, z2);
				int[] p = propagator[d][e1.Item2];
				int[][] compat = compatible[i2];

				for (int l = 0; l < p.Length; l++)
				{
					int t2 = p[l];
					int[] comp = compat[t2];

					comp[d]--;
					if (comp[d] == 0)
					{
						Ban(i2, t2);
					}
				}
			}
		}
	}

	protected void Ban(int i, int t)
	{
		wave[i][t] = false;

		int[] comp = compatible[i][t];
		for (int d = 0; d < DIRECTIONS_AMOUNT; d++) comp[d] = 0;
		stack[stacksize] = new Eppy.Tuple<int, int>(i, t);
		stacksize++;

		double sum = sumsOfWeights[i];
		entropies[i] += sumsOfWeightLogWeights[i] / sum - Math.Log(sum);

		sumsOfOnes[i] -= 1;
		sumsOfWeights[i] -= weights[t];
		sumsOfWeightLogWeights[i] -= weightLogWeights[t];

		sum = sumsOfWeights[i];
		entropies[i] -= sumsOfWeightLogWeights[i] / sum - Math.Log(sum);
	}

	protected virtual void Clear()
	{
		for (int i = 0; i < wave.Length; i++)
		{
			for (int t = 0; t < T; t++)
			{
				wave[i][t] = true;
				for (int d = 0; d < DIRECTIONS_AMOUNT; d++) compatible[i][t][d] = propagator[opposite[d]][t].Length;
			}

			sumsOfOnes[i] = weights.Length;
			sumsOfWeights[i] = sumOfWeights;
			sumsOfWeightLogWeights[i] = sumOfWeightLogWeights;
			entropies[i] = startingEntropy;
		}
	}

	protected void CalculateEntropyAndPatternIdAt(int x, int y, int z, out double entropy, out int? patternId)
	{
		int indexInWave = To1D(x, y, z);
		int amount = 0;
		patternId = null;
		var possiblePatternsFlags = wave[indexInWave];
		for (int t = 0; t < T; t++)
		{
			if (possiblePatternsFlags[t])
			{
				amount += 1;
				patternId = t;
			}
		}

		if (amount != 1)
		{
			patternId = null;
		}

		entropy = entropies[indexInWave] / startingEntropy;
	}

	// Converts a vector into an index for a 1D representation of a 3D space.
	protected int To1D(int x, int y, int z)
	{
		return z * FMX * FMY + y * FMX + x;
	}

	// Converts an index for a 1D representation of a 3D space into a vector.
	protected void To3D(int index, out int x, out int y, out int z)
	{
		z = index / (FMX * FMY);
		index -= z * FMX * FMY;
		y = index / FMX;
		x = index % FMX;
	}

	protected abstract bool OnBoundary(int x, int y, int z);

	public abstract CellState GetCellStateAt(int x, int y, int z);

	protected static int[] DX = { -1, 0, 1, 0, 0, 0 };
	protected static int[] DY = { 0, 0, 0, 0, -1, 1 };
	protected static int[] DZ = { 0, 1, 0, -1, 0, 0 };
	static int[] opposite = { 2, 3, 0, 1, 5, 4 };

}
