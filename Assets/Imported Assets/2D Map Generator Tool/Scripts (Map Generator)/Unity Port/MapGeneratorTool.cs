using UnityEngine;
using System.Linq;
using MapGeneratorTool.DataModels;
using System.Collections.Generic;

namespace MapGeneratorTool.UnityPort
{
    public class MapGeneratorTool : MonoBehaviour, IDataModelValidation
    {
        public int width = 100;
        public int height = 100;

        public WaterNoiseMapParameters waterNoiseMapParameters;
        public GroundNoiseMapParameters heightNoiseMapParameters;
        public GroundNoiseMapParameters temperatureNoiseMapParameters;

        public BiomesDiagram biomesDiagram;
        public WaterLayers waterLayers;

        public GraphicalGenerationType generationType = GraphicalGenerationType.TileMap;
        public SpaceOrientationType orientationType = SpaceOrientationType.Orientation3D;
        public bool generateOnStart = true;
        public bool generateRandomSeed = true;
        public int seed;

        public TilesMap Map { get; set; }

        private void Start()
        {
            if (generateOnStart)
            {
                if(generateRandomSeed)
                    RandomSeed();
                
                TryGenerate();
            }
        }

        public void RandomSeed()
        {
            seed = Random.Range(int.MinValue, int.MaxValue);
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; --i)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            Map = null;
        }

        public void TryGenerate()
        {
            IEnumerable<ValidationError> errors = Validate();
            if(errors.Any())
            {
                foreach(var error in errors)
                    Debug.LogWarning(error);
            }
            else
            {
                Generate();
            }
        }

        private void Generate()
        {
            Clear();

            Generator.Generator generator = new Generator.Generator(width, height, seed, waterLayers.ToModel(), biomesDiagram.ToModel(),
                waterNoiseMapParameters.ToModel(), heightNoiseMapParameters.ToModel(), temperatureNoiseMapParameters.ToModel());

            generator.Generate();
            Map = generator.Map;

            ISpaceOrientation spaceOrientation = new SpaceOrientationFactory().GetSpaceOrientation(orientationType);

            IGraphicalMapGeneratorTool graphicalMapGeneratorTool = new GraphicalMapGeneratorToolFactory().GetGraphicalMapGeneratorTool(generationType, spaceOrientation);
            graphicalMapGeneratorTool.Render(transform, generator.Map);

            ObjectsGenerator objectsGenerator = new ObjectsGenerator(spaceOrientation);
            objectsGenerator.Render(transform, generator.AwaitingObjects);
        }  

        public IEnumerable<ValidationError> Validate()
        {
            DataModelValidator dataModelValidator = new DataModelValidator();
            dataModelValidator.ValidateModel(biomesDiagram, "Biomes Diagram");
            dataModelValidator.ValidateModel(waterLayers, "Water Layers");

            return dataModelValidator.Errors;
        }
    }
}