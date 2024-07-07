using System;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CustomVisionImagePredictionFunctionApp.Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Zone.IoT.FxApp.Models;
using ImagePrediction = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImagePrediction;
using ImageUrl = Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImageUrl;

namespace Zone.IoT.FxApp
{
    public class CustomVision
    {
        private readonly ILogger _logger;

        public CustomVision(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CustomVision>();
        }

        [FunctionName("CustomVision")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "customvision")]
            HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var inputModel = JsonConvert.DeserializeObject<PredictionInputModel>(requestBody);

            var result = await DoCustomVisionPreditionAsync(inputModel.Url, _logger);

            if (result == null) return new OkObjectResult($"Analyze this: {inputModel.Url}");


            var materializedResult = JsonConvert.SerializeObject(result.Predictions,Formatting.Indented);

            var resultFileName = Path.GetFileNameWithoutExtension(inputModel.Url);


            var storageManager = new StorageManager();
            await storageManager.InitContainerAsync();
            
            var blobContainer  = storageManager.CustomVisionImageContainer;
            var blob = blobContainer.GetBlockBlobReference($"{resultFileName}-{Guid.NewGuid()}.json");
            await blob.UploadTextAsync(materializedResult);

            return new OkObjectResult($"Analyzed that: {inputModel.Url} {Environment.NewLine} {materializedResult}");
        }


        #region CustomVision Auth

        private static CustomVisionTrainingClient AuthenticateTraining(string endpoint, string trainingKey)
        {
            // Create the Api, passing in the training key
            CustomVisionTrainingClient trainingApi =
                new CustomVisionTrainingClient(
                    new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.ApiKeyServiceClientCredentials(
                        trainingKey))
                {
                    Endpoint = endpoint
                };
            return trainingApi;
        }

        private static CustomVisionPredictionClient AuthenticatePrediction(string endpoint, string predictionKey)
        {
            // Create a prediction endpoint, passing in the obtained prediction key
            CustomVisionPredictionClient predictionApi =
                new CustomVisionPredictionClient(
                    new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.ApiKeyServiceClientCredentials(
                        predictionKey))
                {
                    Endpoint = endpoint
                };
            return predictionApi;
        }

        #endregion

        #region CustomVision Prediction

        private static readonly HttpClient HttpClient = new HttpClient();

        private static async Task<ImagePrediction?> DoCustomVisionPreditionAsync(string blockblobImageUrl,
            ILogger log)
        {
            // see: https://www.customvision.ai/projects#/settings (Azure login)
            // Add your training key from the settings page of the portal
            string trainingEndpoint = Environment.GetEnvironmentVariable("VISION_TRAINING_ENDPOINT");
            string trainingKey = Environment.GetEnvironmentVariable("VISION_TRAINING_KEY");


            // Create the Api, passing in the training key
            CustomVisionTrainingClient trainingApi = AuthenticateTraining(trainingEndpoint, trainingKey);

            // load a new project
            var projectIdValue = Environment.GetEnvironmentVariable("VISION_PROJECT_ID");
            Guid projectId = Guid.Parse(projectIdValue);
            log.LogInformation($"Loading project with id {projectId}");

            Project project = await trainingApi.GetProjectAsync(projectId);

            // Now there is a trained endpoint, it can be used to make a prediction

            // Add your prediction key from the settings page of the portal
            // The prediction key is used in place of the training key when making predictions
            string predictionEndpoint = Environment.GetEnvironmentVariable("VISION_PREDICTION_ENDPOINT");
            string predictionKey = Environment.GetEnvironmentVariable("VISION_PREDICTION_KEY");

            // Create a prediction endpoint, passing in obtained prediction key
            CustomVisionPredictionClient predictionApi = AuthenticatePrediction(predictionEndpoint, predictionKey);

            // Make a prediction against the new project
            log.LogInformation("Making a prediction:");


            string publishedModelName = Environment.GetEnvironmentVariable("VISION_ITERATION_NAME");






            ImagePrediction? result = null;
            try
            {
                var client = new HttpClient();
                client.BaseAddress = new Uri(blockblobImageUrl);
                var response = await client.GetAsync(blockblobImageUrl);
                response.EnsureSuccessStatusCode();

                Stream imageStream = await response.Content.ReadAsStreamAsync();


                log.LogInformation($"loaded image stream: {imageStream.Position}, {imageStream.Length}");

                //result = await predictionApi.DetectImageAsync(project.Id, publishedModelName, imageStream);

                //// Loop over each prediction and write out the results
                //foreach (PredictionModel c in result.Predictions)
                //{
                //    log.LogInformation($"\t{c.TagName}: {c.Probability:P1}");
                //}

                //result = predictionApi.ClassifyImage(project.Id, publishedModelName, imageStream);

                result = await predictionApi.ClassifyImageUrlAsync(projectId, publishedModelName, new ImageUrl(blockblobImageUrl));


                // Loop over each prediction and write out the results
                foreach (var c in result.Predictions)
                {
                    Console.WriteLine($"\t{c.TagName}: {c.Probability:P1}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                log.LogError(e, e.StackTrace);
            }


            log.LogInformation("Done!");
            return result;
        }

        #endregion
    }
}