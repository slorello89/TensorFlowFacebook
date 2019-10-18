using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace TensorFlowFacebook
{
    public class TFEngine
    {
        static readonly string _assetsPath = Path.Combine(Environment.CurrentDirectory, "assets");
        static readonly string _imagesFolder = Path.Combine(_assetsPath, "inputs-train\\data");
        static readonly string _trainTagsTsv = Path.Combine(_imagesFolder, "tags.tsv");
        static readonly string _testTagsTsv = Path.Combine(_imagesFolder, "test-tags.tsv");
        static readonly string _savePath = Path.Combine(_assetsPath, "inputs-predict-single\\data");        
        static readonly string _inceptionTensorFlowModel = Path.Combine(_assetsPath, "inputs-train\\inception", "tensorflow_inception_graph.pb");
        static readonly object _lock = new object();
        static readonly object _instanceLock = new object();
        private static WebClient _client = new WebClient();
        private static TFEngine _instance;

        private IEstimator<ITransformer> _pipeline;
        private ITransformer _model;
        private MLContext _mlContext;
        public static TFEngine Instance 
        { 
            get 
            {
                lock (_instanceLock)
                {
                    if (_instance == null)
                    {
                        _instance = new TFEngine();
                    }
                    return _instance;
                }
                
            }
        }

        private TFEngine()
        {
            _mlContext = new MLContext();
            _model = GenerateModel();
        }
        public class ImageData
        {
            [LoadColumn(0)]
            public string ImagePath;

            [LoadColumn(1)]
            public string Label;
        }
        public class ImagePrediction : ImageData
        {
            public float[] Score;

            public string PredictedLabelValue;
        }
        private struct InceptionSettings
        {
            public const int ImageHeight = 224;
            public const int ImageWidth = 224;
            public const float Mean = 117;
            public const float Scale = 1;
            public const bool ChannelsLast = true;
        }

        public ITransformer GenerateModel()
        {
            _pipeline = _mlContext.Transforms.LoadImages(outputColumnName: "input", imageFolder: _imagesFolder, inputColumnName: nameof(ImageData.ImagePath))
                // The image transforms transform the images into the model's expected format.
                .Append(_mlContext.Transforms.ResizeImages(outputColumnName: "input", imageWidth: InceptionSettings.ImageWidth, imageHeight: InceptionSettings.ImageHeight, inputColumnName: "input"))
                .Append(_mlContext.Transforms.ExtractPixels(outputColumnName: "input", interleavePixelColors: InceptionSettings.ChannelsLast, offsetImage: InceptionSettings.Mean))
                .Append(_mlContext.Model.LoadTensorFlowModel(_inceptionTensorFlowModel).ScoreTensorFlowModel(outputColumnNames: new[] { "softmax2_pre_activation" }, inputColumnNames: new[] { "input" }, addBatchDimensionInput: true))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "LabelKey", inputColumnName: "Label"))
                .Append(_mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(labelColumnName: "LabelKey", featureColumnName: "softmax2_pre_activation"))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabelValue", "PredictedLabel")).AppendCacheCheckpoint(_mlContext);
            IDataView trainingData = _mlContext.Data.LoadFromTextFile<ImageData>(path: _trainTagsTsv, hasHeader: false);
            ITransformer model = _pipeline.Fit(trainingData);
            IDataView testData = _mlContext.Data.LoadFromTextFile<ImageData>(path: _testTagsTsv, hasHeader: false);
            IDataView predictions = model.Transform(trainingData);
            
            // Create an IEnumerable for the predictions for displaying results
            IEnumerable<ImagePrediction> imagePredictionData = _mlContext.Data.CreateEnumerable<ImagePrediction>(predictions, false);
            DisplayResults(imagePredictionData);
            MulticlassClassificationMetrics metrics = _mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: "LabelKey", predictedLabelColumnName: "PredictedLabel");
            Console.WriteLine($"LogLoss is: {metrics.LogLoss}");
            Console.WriteLine($"PerClassLogLoss is: {String.Join(" , ", metrics.PerClassLogLoss.Select(c => c.ToString()))}");
            return model;
        }

        private static void DisplayResults(IEnumerable<ImagePrediction> imagePredictionData)
        {
            foreach (ImagePrediction prediction in imagePredictionData)
            {
                Console.WriteLine($"Image: {Path.GetFileName(prediction.ImagePath)} predicted as: {prediction.PredictedLabelValue} with score: {prediction.Score.Max()} ");
            }
        }
        public static IEnumerable<ImageData> ReadFromTsv(string file, string folder)
        {
            return File.ReadAllLines(file)
                .Select(line => line.Split('\t'))
                .Select(line => new ImageData()
                {
                    ImagePath = Path.Combine(folder, line[0])
                });
        }

        public string AddTrainingImage(string imageUrl, string label)
        {
            try
            {
                var id = Guid.NewGuid();
                var fileName = Path.Combine(_imagesFolder, $"{id}.jpg");
                lock (_lock)
                {
                    _client.DownloadFile(imageUrl, fileName);
                }

                File.AppendAllText(_trainTagsTsv, $"{id}.jpg\t{label}\n");
                
                _model = GenerateModel();
                return $"I have trained myself to recognize the image you sent me as a {label}. Your wisdom and teaching is apprecatited";
            }
            catch (Exception)
            {
                return "something went wrong when trying to train on image";
            }
            
            
        }
        public string ClassifySingleImage(string imageUrl)
        {
            try
            {
                var filename = Path.Combine(_savePath, $"{Guid.NewGuid()}.jpg");
                lock (_lock)
                {
                    _client.DownloadFile(imageUrl, filename);
                }


                var imageData = new ImageData()
                {
                    ImagePath = filename
                };

                var predictor = _mlContext.Model.CreatePredictionEngine<ImageData, ImagePrediction>(_model);
                var prediction = predictor.Predict(imageData);
                var response = $"I'm about {prediction.Score.Max() * 100}% sure that the image you sent me is a {prediction.PredictedLabelValue}";
                Console.WriteLine($"Image: {Path.GetFileName(imageData.ImagePath)} predicted as: {prediction.PredictedLabelValue} with score: {prediction.Score.Max() * 100} ");
                return response;
            }
            catch (Exception)
            {
                return "Something went wrong when trying to classify image";
            }
            
        }
    }
}
