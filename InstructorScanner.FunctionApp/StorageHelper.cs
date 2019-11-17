﻿using InstructorScanner.Core;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;

namespace InstructorScanner.FunctionApp
{
    public interface IStorageHelper
    {
        Task<bool> FileExistsAsync(string containerName, string fileName);
        Task<string> ReadFileAsync(string containerName, string fileName);
        Task SaveFileAsync(string containerName, string fileName, string fileContents);
    }

    public class StorageHelper : IStorageHelper
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly Lazy<CloudBlobClient> _cloudBlobClient;

        public StorageHelper(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings;

            _cloudBlobClient = new Lazy<CloudBlobClient>(() =>
            {
                var cloudStorageAccount = CloudStorageAccount.Parse(_appSettings.Value.StorageConnectionString);
                return cloudStorageAccount.CreateCloudBlobClient();
            });

        }

        public async Task<bool> FileExistsAsync(string containerName, string fileName)
        {
            return await _cloudBlobClient
                .Value
                .GetContainerReference(containerName)
                .GetBlockBlobReference(fileName)
                .ExistsAsync();
        }

        public async Task<string> ReadFileAsync(string containerName, string fileName)
        {
            var cloudBlobContainer = _cloudBlobClient.Value.GetContainerReference(containerName);

            var blockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);

            using (var ms = new MemoryStream())
            {
                await blockBlob.DownloadToStreamAsync(ms);

                using (var sr = new StreamReader(ms))
                {
                    return await sr.ReadToEndAsync();
                }
            }
        }

        public Task SaveFileAsync(string containerName, string fileName, string fileContents)
        {
            return Task.CompletedTask;
        }
    }
}
