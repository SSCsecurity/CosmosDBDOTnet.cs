namespace Dot6.API.CosmosDB.Demo.Services;
 
public interface ICarCosmosService
{
 
}

using Microsoft.Azure.Cosmos;
 
namespace Dot6.API.CosmosDB.Demo.Services;
public class CarCosmosService : ICarCosmosService
{
    private readonly Container _container;
    public CarCosmosService(CosmosClient cosmosClient,
    string databaseName,
    string containerName)
    {
        _container = cosmosClient.GetContainer(databaseName, containerName);
    }
}

builder.Services.AddSingleton<ICarCosmosService>(options =>
{
    string url = builder.Configuration.GetSection("AzureCosmosDbSettings")
    .GetValue<string>("URL");
    string primaryKey = builder.Configuration.GetSection("AzureCosmosDbSettings")
    .GetValue<string>("PrimaryKey");
    string dbName = builder.Configuration.GetSection("AzureCosmosDbSettings")
    .GetValue<string>("DatabaseName");
    string containerName = builder.Configuration.GetSection("AzureCosmosDbSettings")
    .GetValue<string>("ContainerName");
	
    var cosmosClient = new CosmosClient(
        url,
        primaryKey
    );
	
    return new CarCosmosService(cosmosClient, dbName, containerName);
});

using Dot6.API.CosmosDB.Demo.Services;
using Microsoft.AspNetCore.Mvc;
namespace Dot6.API.CosmosDB.Demo.Controllers;
 
[ApiController]
[Route("[controller]")]
public class CarController: ControllerBase
{
    public readonly ICarCosmosService _carCosmosService;
    public CarController(ICarCosmosService carCosmosService)
    {
        _carCosmosService = carCosmosService;
    }
}

using Newtonsoft.Json;
 
namespace Dot6.API.CosmosDB.Demo.Models;
 
public class Car
{
    [JsonProperty("id")]
    public string Id { get; set; }
    [JsonProperty("make")]
    public string Make { get; set; }
    [JsonProperty("model")]
    public string Model { get; set; }
}

Task<List<Car>> Get(string sqlCosmosQuery);

public async Task<List<Car>> Get(string sqlCosmosQuery)
{
	var query = _container.GetItemQueryIterator<Car>(new QueryDefinition(sqlCosmosQuery));
 
	List<Car> result = new List<Car>();
	while (query.HasMoreResults)
	{
		var response = await query.ReadNextAsync();
		result.AddRange(response);
	}
 
	return result;
}

[HttpGet]
public async Task<IActionResult> Get()
{
	var sqlCosmosQuery = "Select * from c";
	var result = await _carCosmosService.Get(sqlCosmosQuery);
	return Ok(result);
}

Task<Car> AddAsync(Car newCar);

public async Task<Car> AddAsync(Car newCar)
{
   var item = await _container.CreateItemAsync<Car>(newCar, new PartitionKey(newCar.Make));
   return item;
}

[HttpPost]
public async Task<IActionResult> Post(Car newCar)
{
	newCar.Id = Guid.NewGuid().ToString();
	var result = await _carCosmosService.AddAsync(newCar);
	return Ok(result);
}

Task<Car> Update(Car carToUpdate);


public async Task<Car> Update(Car carToUpdate)
{
  var item = await _container.UpsertItemAsync<Car>(carToUpdate, new PartitionKey(carToUpdate.Make));
  return item;
}

[HttpPut]
public async Task<IActionResult> Put(Car carToUpdate)
{
  var result = await _carCosmosService.Update(carToUpdate);
  return Ok(result);
}

