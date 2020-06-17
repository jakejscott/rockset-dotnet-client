using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RocksetDotnetClient;

namespace RocksetSample
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var apiKey = Environment.GetEnvironmentVariable("ROCKSET_API_KEY") ?? throw new InvalidOperationException("ROCKSET_API_KEY environment variable not found");

            var workspace = "dotnet-test-workspace";
            var collection = "dotnet-test-collection";

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", apiKey);

            IRocksetApiClient rockset = new RocksetApiClient(httpClient);

            await EnsureWorkspaceAsync(rockset, workspace);
            await EnsureCollectionAsync(rockset, workspace, collection);
            await EnsureDocumentAsync(rockset, workspace, collection);


            var queryResponse = await rockset.QueryAsync(new QueryRequest
            {
                Sql = new QueryRequestSql
                {
                    Query = @$"select * from ""{workspace}"".""{collection}"" c where c.Email = 'jake.net@gmail.com'"
                }
            });

            foreach (var result in queryResponse.Results)
            {
                var json = result as JObject;

                Customer customer = json?.ToObject<Customer>();

                Console.WriteLine($"Customer: {customer?.Email}");
            }
        }

        private static async Task EnsureDocumentAsync(IRocksetApiClient rockset, string workspace, string collection)
        {
            var addDocumentsResponse = await rockset.AddDocumentsAsync(workspace, collection, new AddDocumentsRequest
            {
                Data = new List<object>
                {
                    new Customer
                    {
                        Email = "jake.net@gmail.com",
                        FirstName = "Jake",
                        LastName = "Scott"
                    }
                }
            });

            var documentStatus = addDocumentsResponse.Data.Single();
            Console.WriteLine($"Added document {documentStatus._id} with status {documentStatus.Status}");
        }

        private static async Task EnsureWorkspaceAsync(IRocksetApiClient rockset, string workspace)
        {
            try
            {
                GetWorkspaceResponse response = await rockset.GetWorkspaceAsync(workspace);

                Console.WriteLine($"Workspace {response.Data.Name} exists");
            }
            catch (RocksetApiException e)
            {
                if (e.StatusCode == 404)
                {
                    Console.WriteLine($"Creating workspace {workspace}");

                    var response = await rockset.CreateWorkspaceAsync(new CreateWorkspaceRequest
                    {
                        Name = workspace,
                        Description = "dotnet test workspace"
                    });

                    Console.WriteLine($"Created workspace {response.Data.Name}");
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task EnsureCollectionAsync(IRocksetApiClient rockset, string workspace, string collection)
        {
            try
            {
                GetCollectionResponse response = await rockset.GetCollectionAsync(workspace, collection);

                Console.WriteLine($"Collection {response.Data.Name} exists");
            }
            catch (RocksetApiException e)
            {
                if (e.StatusCode == 404)
                {
                    Console.WriteLine($"Creating collection {collection}");

                    var createCollectionResponse = await rockset.CreateCollectionAsync(workspace, new CreateCollectionRequest
                    {
                        Name = collection,
                        Description = "dotnet test collection"
                    });

                    Console.WriteLine($"Created collection {createCollectionResponse.Data.Name}");
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
