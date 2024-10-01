using Azure.Data.Tables;
using System.Text.Json;
using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.IO;
using System.Threading.Tasks;
using System.Net;

namespace MHKAT
{
    public static class AddCustomer
    {
        [Function("TLC")]
        public static async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<NameEntity>(requestBody);

            if (data == null || string.IsNullOrEmpty(data.Customer_Name) || string.IsNullOrEmpty(data.Customer_Email) || string.IsNullOrEmpty(data.Customer_Password))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badResponse.WriteString("Invalid input");
                return badResponse;
            }

            string connectionString = "DefaultEndpointsProtocol=https;AccountName=ha55an;AccountKey=UDKeDju9w9U1uGkKr2G1c2zyIHc5SQ1b57SUREejkqJCYYImcsgpG+NMZ8A9Q8X5m/t+iU8VR9ev+AStFcwLEQ==;EndpointSuffix=core.windows.net";
            var tableClient = new TableClient(connectionString, "Customers");
            await tableClient.CreateIfNotExistsAsync();

            data.PartitionKey = "CustomerPartition";
            data.RowKey = Guid.NewGuid().ToString();

            await tableClient.AddEntityAsync(data);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString($"Customer '{data.Customer_Name}' added successfully.");
            return response;
        }

        private class NameEntity : ITableEntity
        {
            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public string Customer_Name { get; set; } 
            public string Customer_Email { get; set; } 
            public string Customer_Password { get; set; } 
            public ETag ETag { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
        }
    }
}



