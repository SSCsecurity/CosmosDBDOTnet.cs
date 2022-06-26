public interface ICosmosDbService
{
    Task<IEnumerable<Item>> GetMultipleAsync(string query);
    Task<Item> GetAsync(string id);
    Task AddAsync(Item item);
    Task UpdateAsync(string id, Item item);
    Task DeleteAsync(string id);
}

public class CosmosDbService : ICosmosDbService
{
    private Container _container;

    public CosmosDbService(
        CosmosClient cosmosDbClient,
        string databaseName,
        string containerName)
    {
        _container = cosmosDbClient.GetContainer(databaseName, containerName);
    }

    public async Task AddAsync(Item item)
    {
        await _container.CreateItemAsync(item, new PartitionKey(item.Id));
    }

    public async Task DeleteAsync(string id)
    {
        await _container.DeleteItemAsync<Item>(id, new PartitionKey(id));
    }

    public async Task<Item> GetAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<Item>(id, new PartitionKey(id));
            return response.Resource;
        }
        catch (CosmosException) //For handling item not found and other exceptions
        {
            return null;
        }
    }

    public async Task<IEnumerable<Item>> GetMultipleAsync(string queryString)
    {
        var query = _container.GetItemQueryIterator<Item>(new QueryDefinition(queryString));

        var results = new List<Item>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return results;
    }

    public async Task UpdateAsync(string id, Item item)
    {
        await _container.UpsertItemAsync(item, new PartitionKey(id));
    }
}

private static async Task<CosmosDbService> InitializeCosmosClientInstanceAsync(IConfigurationSection configurationSection)
{
    var databaseName = configurationSection["DatabaseName"];
    var containerName = configurationSection["ContainerName"];
    var account = configurationSection["Account"];
    var key = configurationSection["Key"];

    var client = new Microsoft.Azure.Cosmos.CosmosClient(account, key);
    var database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
    await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

    var cosmosDbService = new CosmosDbService(client, databaseName, containerName);
    return cosmosDbService;
}

public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddSingleton<ICosmosDbService>(InitializeCosmosClientInstanceAsync(Configuration.GetSection("CosmosDb")).GetAwaiter().GetResult());
}

[Route("api/[controller]")]
[ApiController]
public class ItemsController : ControllerBase
{
    private readonly ICosmosDbService _cosmosDbService;

    public ItemsController(ICosmosDbService cosmosDbService)
    {
        _cosmosDbService = cosmosDbService ?? throw new ArgumentNullException(nameof(cosmosDbService));
    }

    // GET api/items
    [HttpGet]
    public async Task<IActionResult> List()
    {
        return Ok(await _cosmosDbService.GetMultipleAsync("SELECT * FROM c"));
    }

    // GET api/items/5
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        return Ok(await _cosmosDbService.GetAsync(id));
    }

    // POST api/items
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Item item)
    {
        item.Id = Guid.NewGuid().ToString();
        await _cosmosDbService.AddAsync(item);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }

    // PUT api/items/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Edit([FromBody] Item item)
    {
        await _cosmosDbService.UpdateAsync(item.Id, item);
        return NoContent();
    }

    // DELETE api/items/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _cosmosDbService.DeleteAsync(id);
        return NoContent();
    }
}

