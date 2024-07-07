using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CustomVisionImagePredictionFunctionApp.Azure
{
    public sealed class StorageManager
    {
        public const string CustomVisionImageContainerName = "customvision-prediction-results";

        public CloudBlobContainer CustomVisionImageContainer { get; }

        public StorageManager()
        {
            string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            // Retrieve storage account from connection string.
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageConnectionString);

            // Create the blob client.
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

            #region Get Container Reference for truckerapp-stage, truckerapp-void, truckerapp

            // Retrieve a reference to a container truckerapp-photos
            CustomVisionImageContainer = cloudBlobClient.GetContainerReference(CustomVisionImageContainerName);

            #endregion
        }

        public async Task InitContainerAsync()
        {
            await CustomVisionImageContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, new BlobRequestOptions(), new OperationContext());
        }
    }
}