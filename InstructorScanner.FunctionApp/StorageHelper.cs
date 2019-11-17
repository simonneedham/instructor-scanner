using InstructorScanner.Core;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace InstructorScanner.FunctionApp
{
    public interface IStorageHelper
    {
        Task<string> ReadFileAsync(string containerName, string fileName);
        Task SaveFileAsync(string containerName, string fileName, string fileContents);
    }

    public class StorageHelper
    {
        private readonly IOptions<AppSettings> _appSettings;

        public StorageHelper(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        public async Task<string> ReadFileAsync(string containerName, string fileName)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(_appSettings.Value.StorageConnectionString);
            var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            var cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);

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

        public async Task SaveFileAsync(string containerName, string fileName, string fileContents)
        {
            throw new NotImplementedException();
        }
    }
}
