using Core.Data;
using Core.Data.OverlappingModel;
using Core.Data.SimpleTiledModel;
using Core.InputProviders;
using Core.Model;
using Core.Model.New;
using UnityEditor;
using UnityEngine;

namespace Core
{
	public class WaveFunctionCollapseGenerator : MonoBehaviour
	{
		// Provides tile and symmetry data to the generator.
		[SerializeField]
		private InputDataProvider dataProvider;

		// Renders the output of the generator.
		[SerializeField]
		private WaveFunctionCollapseRenderer renderer;

		// The width of the output area.
		[SerializeField]
		private int width;

		// The height of the output area.
		[SerializeField]
		private int height;

		// The depth of the output area. Only works for tiled models.
		[SerializeField]
		private int depth;

		// The width of the tiles.
		[SerializeField]
		private int patternSize;

		[SerializeField]
		private bool periodicInput = false;

		[SerializeField]
		private bool periodicOutput = false;

		[SerializeField]
		private int symmetry = 1;

		[SerializeField]
		private int foundation = 0;

		// The number of times to run a single collapse and propagation step.
		// 0 will run infinitely.
		[SerializeField]
		private int iterations = 0;

		// Variables to reference the overlapping model and its input data used in overlapping generation.
		private InputOverlappingData inputOverlappingData;
		private OvelappingModel2dWrapper overlappingModel;

		// Variable to reference the tiled model used in tiled generation.
		private SimpleTiledMode3d simpleTiledModel;

		// Variable to reference the generation coroutine.
		private Coroutine runningCoroutine;

		// Run the generator using an overlapping model.
		public void GenerateOverlappingOutput()
		{
			Abort();

			inputOverlappingData = dataProvider.GetInputOverlappingData();
			var modelParams = new OverlappingModelParams(width, height, depth, patternSize);
			modelParams.PeriodicInput = periodicInput;
			modelParams.PeriodicOutput = periodicOutput;
			modelParams.Symmetry = symmetry;
			modelParams.Ground = foundation;

			overlappingModel = new OvelappingModel2dWrapper(inputOverlappingData, modelParams);
			renderer.Init(overlappingModel);

			runningCoroutine = StartCoroutine(overlappingModel.Model.RunViaEnumerator(0, iterations, OnResult, OnIteration));
		}

		// Run the generator using a tiled model.
		public void GenerateSimpleTiledOutput()
		{
			Abort();

			// Get the input data and build a model for the generator.
			var inputData = dataProvider.GetInputSimpleTiledData();
			var modelParams = new SimpleTiledModelParams(width, height, depth, periodicOutput);
			simpleTiledModel = new SimpleTiledMode3d(inputData, modelParams);

			// Initialise the renderer and start the generator.
			renderer.Init(simpleTiledModel);
			runningCoroutine = StartCoroutine(simpleTiledModel.RunViaEnumerator(0, iterations, OnResult, OnIteration));
		}

		// Code called after each iteration of the generator.
		private void OnIteration(bool[][] wave)
		{
			Debug.Log("Intermediate iteration step.");
			renderer.UpdateStates();
			Debug.Log("Finished interation step.");
		}

		// Code called after the generator has finished.
		// result: Whether the generator managed to build an output satisfying all constraints or not.
		private void OnResult(bool result)
		{
			Debug.Log("Generation " + (result ? "Succeeded" : "Failed!"));
		}

		// Method to cancel the current generation and clear the renderer.
		public void Abort()
		{
			if (runningCoroutine != null)
			{
				StopCoroutine(runningCoroutine);
			}
			renderer.Clear();
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(WaveFunctionCollapseGenerator))]
	public class WaveFunctionCollapseGeneratorEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			WaveFunctionCollapseGenerator generator = (WaveFunctionCollapseGenerator)target;
			if (GUILayout.Button("Generate Overlapping output"))
			{
				generator.GenerateOverlappingOutput();
			}
			if (GUILayout.Button("Generate Simple tiled output"))
			{
				generator.GenerateSimpleTiledOutput();
			}
			if (GUILayout.Button("Abort and Clear"))
			{
				generator.Abort();
			}
			DrawDefaultInspector();
		}
	}
#endif
}