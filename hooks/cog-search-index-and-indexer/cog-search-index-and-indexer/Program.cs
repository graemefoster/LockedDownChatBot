// See https://aka.ms/new-console-template for more information

using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

var searchEndpoint = Environment.GetEnvironmentVariable("OUT_AZURE_SEARCH_ENDPOINT")!;
var storageAccountName = Environment.GetEnvironmentVariable("OUT_AZURE_STORAGE_ACCOUNT_NAME")!;
var infoBlobContainerName = Environment.GetEnvironmentVariable("OUT_AZURE_STORAGE_CONTAINER_NAME")!;
var expectedIndexName = Environment.GetEnvironmentVariable("OUT_AZURE_SEARCH_INDEX_NAME")!;
var resourceGroup = Environment.GetEnvironmentVariable("OUT_AZURE_RESOURCE_GROUP")!;

var armClient = new ArmClient(new AzureCliCredential());

var storageAccount = armClient
    .GetDefaultSubscription()
    .GetResourceGroup(resourceGroup).Value
    .GetStorageAccount(storageAccountName);

var primaryKey = storageAccount.Value.GetKeys().ElementAt(0).Value;
var blobConnectionString = "DefaultEndpointsProtocol=https;AccountName=" + storageAccountName + ";AccountKey=" +
                           primaryKey + ";EndpointSuffix=core.windows.net";

var searchIndexClient = new SearchIndexClient(new Uri(searchEndpoint), new DefaultAzureCredential());
var searchIndexerClient = new SearchIndexerClient(new Uri(searchEndpoint), new DefaultAzureCredential());

var vectorSearchConfig = new VectorSearch()
{
    AlgorithmConfigurations =
    {
        new HnswVectorSearchAlgorithmConfiguration("vector-search-config")
    }
};

var index = new SearchIndex(expectedIndexName)
{
    VectorSearch = vectorSearchConfig,
    Fields = new List<SearchField>()
    {
        new SearchField("id", SearchFieldDataType.String)
            { IsKey = true, IsFilterable = false, IsSearchable = false },
        new SearchField("contentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
        {
            IsFilterable = false, IsSearchable = true, IsSortable = false, IsFacetable = false,
            IsHidden = true,
            VectorSearchConfiguration = "vector-search-config",
            VectorSearchDimensions = 1536, //text-embedding-ada-002
        },
        new SearchField("content", SearchFieldDataType.String)
            { IsFilterable = false, IsSearchable = true, IsSortable = false, IsFacetable = false },
        new SearchField("metadata_storage_name", SearchFieldDataType.String)
            { IsFilterable = true, IsSearchable = false, IsSortable = true },
        new SearchField("metadata_storage_size", SearchFieldDataType.Int64)
            { IsFilterable = true, IsSearchable = false, IsSortable = true },
        new SearchField("metadata_storage_content_type", SearchFieldDataType.String)
            { IsFilterable = true, IsSearchable = false, IsSortable = true },
    },
    SemanticSettings = new SemanticSettings()
    {
        Configurations =
        {
            new SemanticConfiguration("default", new PrioritizedFields()
            {
                TitleField = new SemanticField() { FieldName = "metadata_storage_name" },
                ContentFields = { new SemanticField() { FieldName = "content" } },
                KeywordFields = { new SemanticField() { FieldName = "content" } },
                
            })
        },
        DefaultConfiguration = "default"
    }
};
var indexDataSource = new SearchIndexerDataSourceConnection(
    "info-ds",
    SearchIndexerDataSourceType.AzureBlob,
    blobConnectionString,
    new SearchIndexerDataContainer(infoBlobContainerName)
);

var indexer = new SearchIndexer(
    "info-indexer",
    indexDataSource.Name,
    expectedIndexName
)
{
    Schedule = new IndexingSchedule(TimeSpan.FromHours(1)),
    Parameters = new IndexingParameters()
    {
        IndexingParametersConfiguration = new IndexingParametersConfiguration()
        {
            ParsingMode = BlobIndexerParsingMode.Json
        }
    }
};

await searchIndexClient.CreateOrUpdateIndexAsync(index);
await searchIndexerClient.CreateOrUpdateDataSourceConnectionAsync(indexDataSource);
await searchIndexerClient.CreateOrUpdateIndexerAsync(indexer);

Console.WriteLine("Successfully configured Index and Indexer for Cognitive Search Service");