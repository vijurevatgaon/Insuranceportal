using InsuranceClient.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InsuranceClient.Helpers
{
    public class StorageHelper
    {
        private CloudStorageAccount storageAccount;
        private CloudBlobClient blobClient;
        private CloudTableClient tableClient;
        private CloudQueueClient queueClient;

        public string ConnectionString
        {
            set {
                this.storageAccount = CloudStorageAccount.Parse(value);
                this.blobClient = storageAccount.CreateCloudBlobClient();
                this.tableClient = storageAccount.CreateCloudTableClient();
                this.queueClient= storageAccount.CreateCloudQueueClient();
            }

        }

        public async Task<Customer> InsertCustomerAsync(string tableName, Customer customer)
        {

            var table = tableClient.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync();
            TableOperation insertoperation = TableOperation.Insert(customer);

            var result = await table.ExecuteAsync(insertoperation);
            return result.Result as Customer;
        }

        public async Task<string> UploadCustomerImage(string ContainerName, string ImagePath)
        {

            var container = blobClient.GetContainerReference(ContainerName);
            await container.CreateIfNotExistsAsync();
            var ImageName = Path.GetFileName(ImagePath);
            var blob = container.GetBlockBlobReference(ImageName);
            await blob.DeleteIfExistsAsync();
            await blob.UploadFromFileAsync(ImagePath);
            return blob.Uri.AbsoluteUri;

        }

        public async Task AddMessageAsync(string queueName, Customer customer)
        {

            var queue = queueClient.GetQueueReference(queueName);

            await queue.CreateIfNotExistsAsync();
            var messageBody = JsonConvert.SerializeObject(customer);

            CloudQueueMessage mesage = new CloudQueueMessage(messageBody);

            await queue.AddMessageAsync(mesage, TimeSpan.FromDays(3), TimeSpan.Zero, null, null);
        }

    } 

}
