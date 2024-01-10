using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
public class AzureBlobStorageUploader : MonoBehaviour
{
    public static string AccountKey { get; private set; }
    public string configPath;
    private string connectionString;
    private string containerName = "testuploads";
    private string blobName = $"test{DateTime.UtcNow.Ticks}.txt";
    public string localFilePath;

    async void Start()
    {
        LoadConfig();
        await UploadToBlobStorage();
    }

    private void LoadConfig()
    {
        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            ConfigData config = JsonUtility.FromJson<ConfigData>(json);
            AccountKey = config.AccountKey;
            connectionString = $"DefaultEndpointsProtocol=https;AccountName=hololensuploads;AccountKey={AccountKey};EndpointSuffix=core.windows.net";

        }
        else
        {
            Debug.LogError("Config file not found!");
        }
    }

    [Serializable]
    public class ConfigData
    {
        public string AccountKey;
    }

    private async Task UploadToBlobStorage()
  {
    try
    {
      BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
      BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
      BlobClient blobClient = containerClient.GetBlobClient(blobName);

      using (FileStream fs = File.OpenRead(localFilePath))
      {
        await blobClient.UploadAsync(fs, true);
        Debug.Log("File uploaded successfully to Azure Blob Storage.");
      }
    }
    catch (RequestFailedException ex)
    {
      Debug.LogError($"Error uploading file to Azure Blob Storage: {ex.Message}");
    }
    catch (Exception ex)
    {
      Debug.LogError($"An unexpected error occurred: {ex.Message}");
    }
  }
}
