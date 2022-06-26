using Microsoft.Azure.Cosmos;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "WebApplication1", Version = "v1" });
}); 
builder.Services.AddHttpClient();
builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
{
    //IHttpClientFactory httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

    //CosmosClientOptions cosmosClientOptions = new CosmosClientOptions
    //{
        //HttpClientFactory = httpClientFactory.CreateClient,
        //ConnectionMode = ConnectionMode.Gateway
    //};

    return new CosmosClient("<cosmosdb_connectionstring>");
    // sample code
    //return new CosmosClient("AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");//, cosmosClientOptions);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApplication1 v1"));
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.WindowsAzure.Storage.Table;

namespace WebApplication1.Controllers;
[ApiController]
[Route("[controller]")]
public class HomeController : ControllerBase
{
    public CosmosClient _client;
    private readonly ILogger<WeatherForecastController> _logger;

    public HomeController(ILogger<WeatherForecastController> logger, CosmosClient client)
    {
        _logger = logger;
        _client = client;
    }
    [HttpGet]
    public async Task<string> getvalue() {
        string dbname = "test";
        string containername = "container1";
        Database database = await _client.CreateDatabaseIfNotExistsAsync(dbname);
        Container container = database.GetContainer(containername);
        var query = container.GetItemQueryIterator<Test>("SELECT c.id FROM c");
        string ss = string.Empty;
        while (query.HasMoreResults)
        {
            FeedResponse<Test> result = await query.ReadNextAsync();
            foreach (var item in result) {
                ss += item.id;
            }
        }
        return ss;
    }
    public class Test : TableEntity { 
        public int id { get; set; }
    }
}
