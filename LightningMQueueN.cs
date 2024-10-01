using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;

namespace MHKAT
{
    public class LightningMQueueN
    {
        private const string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=ha55an;AccountKey=UDKeDju9w9U1uGkKr2G1c2zyIHc5SQ1b57SUREejkqJCYYImcsgpG+NMZ8A9Q8X5m/t+iU8VR9ev+AStFcwLEQ==;EndpointSuffix=core.windows.net";
        private const string QueueName = "transactions";

        [Function("LightningMQueueN")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var transaction = JsonSerializer.Deserialize<Transaction>(requestBody);

            if (transaction == null || !IsValidTransaction(transaction))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badResponse.WriteString("Invalid transaction data");
                return badResponse;
            }

            var tableClient = new TableClient(ConnectionString, "Transactions");
            await tableClient.CreateIfNotExistsAsync();

            transaction.Transaction_Date = DateTime.SpecifyKind(transaction.Transaction_Date, DateTimeKind.Utc);
            transaction.PartitionKey = "TransactionsPartition";
            transaction.RowKey = Guid.NewGuid().ToString();

            await tableClient.AddEntityAsync(transaction);

            string message = $"New transaction by Customer {transaction.Transaction_Id} of Product {transaction.Product_ID} at {transaction.Transaction_Category} on {transaction.Transaction_Date}";

            var queueClient = new QueueClient(ConnectionString, QueueName);
            await queueClient.CreateIfNotExistsAsync();
            await queueClient.SendMessageAsync(message);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString($"Transaction '{transaction.Transaction_Id}' added successfully.");
            return response;
        }

        private static bool IsValidTransaction(Transaction transaction)
        {
            return transaction.Transaction_Id > 0 &&
                   transaction.Product_ID > 0 &&
                   !string.IsNullOrEmpty(transaction.Transaction_Category) &&
                   transaction.Transaction_Date != default;
        }

        public class Transaction : ITableEntity
        {
            
            public int Transaction_Id { get; set; }

            public string? PartitionKey { get; set; }
            public string? RowKey { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; } 

            [Required(ErrorMessage = "Choose a customer!")]
            public int Customer_ID { get; set; }

            [Required(ErrorMessage = "Pick a valid product.")]
            public int Product_ID { get; set; }

            [Required(ErrorMessage = "Select a date within valid range.")]
            public DateTime Transaction_Date { get; set; }

            [Required(ErrorMessage = "Pick a valid category.")]
            public string? Transaction_Category { get; set; }
        }

    }
}





