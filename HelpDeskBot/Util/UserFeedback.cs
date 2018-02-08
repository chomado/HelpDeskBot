using Autofac;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Table;

namespace HelpDeskBot.Util
{
    public class FeedbackEntity : TableEntity
    {
        public int Rate { get; set; }
        public string Comment { get; set; }

        public FeedbackEntity(int rate, string comment)
        {
            this.PartitionKey = "popopo";
            this.RowKey = System.Guid.NewGuid().ToString();
            this.Rate = rate;
            this.Comment = comment;
        }

        public FeedbackEntity() { }

        public async Task SendFeedbackToAzure()
        {
            // Retrieve the storage account from the connection string.
            //CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString);
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                    CloudConfigurationManager.GetSetting("StorageConnectionString"));
            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the "people" table.
            CloudTable table = tableClient.GetTableReference("feedback");

            // Create a new customer entity.
            FeedbackEntity testData = new FeedbackEntity(rate: this.Rate, comment: this.Comment);

            // Create the TableOperation object that inserts the customer entity.
            TableOperation insertOperation = TableOperation.Insert(testData);

            // Execute the insert operation.
            await table.ExecuteAsync(insertOperation);
        }
    }
}