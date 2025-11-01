using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using FurnitureFinder.Shared.Configurations;
using FurnitureFinder.Shared.Models;
using FurnitureFinder.Shared.Services.Interfaces;

namespace FurnitureFinder.Shared.Services;

public class SearchIndexService : ISearchIndexService
{
    private readonly SearchClient _searchClient;
    private readonly ILogger<SearchIndexService> _logger;
    private readonly SearchConfig _searchConfig;
    private readonly OpenAIConfig _openAiConfig;

    private readonly string _synonymMapName = "color-synonym-map";
    private readonly string _semanticConfigurationName = "default";
    private readonly string _vectorConfigName = "product-summary-vector-config";
    private readonly string _vectorProfileName = "product-summary-vector-profile";
    private readonly string _vectorizerName = "product-summary-vectorizer";
    private readonly int _embeddingDimensions;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public SearchIndexService(IOptions<SearchConfig> searchConfig,
                              IOptions<OpenAIConfig> openAiConfig,
                              ILogger<SearchIndexService> logger)
    {
        _searchConfig = searchConfig.Value;
        _openAiConfig = openAiConfig.Value;
        _logger = logger;
        _embeddingDimensions = openAiConfig.Value.EmbeddingDimensions;

        _searchClient = new SearchClient(new Uri(_searchConfig.Endpoint),
                                         _searchConfig.IndexName,
                                         new AzureKeyCredential(_searchConfig.Key));
    }

    public async Task CreateIndexAsync(CancellationToken cancellationToken = default)
    {
        // Create Synonym Map
        SynonymMap synonymMap = CreateSynonymMap();

        // Get Search Fields
        List<SearchField> fields = GetSearchFields();

        // Get Semantic Configuration
        SemanticSearch semanticSearchConfiguration = GetSemanticSearchConfiguration();

        // Get Vectors Configuration
        VectorSearch vectorSearchConfiguration = GetVectorSearchConfiguration();

        // Create Index
        var index = new SearchIndex(_searchConfig.IndexName)
        {
            Fields = fields,
            Similarity = new BM25Similarity(),
            SemanticSearch = semanticSearchConfiguration,
            VectorSearch = vectorSearchConfiguration
        };

        var adminClient = new SearchIndexClient(new Uri(_searchConfig.Endpoint),
                                                new AzureKeyCredential(_searchConfig.Key));

        await adminClient.CreateOrUpdateSynonymMapAsync(synonymMap, cancellationToken: cancellationToken);

        await adminClient.DeleteIndexAsync(index, cancellationToken: cancellationToken);

        await adminClient.CreateOrUpdateIndexAsync(index, cancellationToken: cancellationToken);
    }

    public async Task<List<Product>> GetProductsWithoutEmbeddingsAsync(int count, CancellationToken cancellationToken = default)
    {
        try
        {
            var searchOptions = new SearchOptions
            {
                Filter = "vectorRetrieved eq false or vectorRetrieved eq null",
                Size = count
            };

            var searchResult = await _searchClient.SearchAsync<Product>("*", searchOptions, cancellationToken);

            var products = new List<Product>();

            await foreach (var result in searchResult.Value.GetResultsAsync()
                .AsPages(pageSizeHint: 50))
            {
                foreach (var product in result.Values)
                {
                    products.Add(product.Document);
                }
            }

            _logger.LogInformation("Retrieved {Count} products without embeddings", products.Count);

            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products without embeddings");
            throw;
        }
    }

    public async Task MergeOrUploadProductsAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default)
    {
        try
        {
            // If no products provided, try to load from sample data file
            if (products == null || !products.Any())
            {
                if (File.Exists("sample data/sample_furniture_data.json"))
                {
                    var json = await File.ReadAllTextAsync("sample data/sample_furniture_data.json", cancellationToken);
                    products = JsonSerializer.Deserialize<List<Product>>(json, _jsonSerializerOptions) ?? [];
                }
                else
                {
                    _logger.LogWarning("No products provided and sample data file not found");
                    return;
                }
            }

            var batch = IndexDocumentsBatch.MergeOrUpload(products);
            IndexDocumentsResult result = await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);

            var failedDocuments = result.Results.Where(r => !r.Succeeded).ToList();
            foreach (var failed in failedDocuments)
            {
                _logger.LogWarning("Failed to update document with key: {Key}, Error: {ErrorMessage}",
                  failed.Key, failed.ErrorMessage);
            }

            _logger.LogInformation("Successfully updated {SuccessCount} products out of {TotalCount}",
                result.Results.Count(r => r.Succeeded), products.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating products");
            throw;
        }
    }

    private SynonymMap CreateSynonymMap()
    {
        string[] value =
        [
            "white, ivory, alabaster, off-white, cream",
            "black, ebony, onyx, jet black, charcoal",
            "gray, grey, silver, slate, pewter, stone",
            "brown, chocolate, espresso, walnut, chestnut, mocha, taupe, cocoa",
            "beige, tan, khaki, sand, camel, oatmeal",
            "red, crimson, burgundy, maroon, ruby, wine",
            "blue, navy, cobalt, sapphire, indigo, denim",
            "green, emerald, olive, sage, forest, mint, moss",
            "yellow, gold, mustard, ochre, honey",
            "orange, tangerine, rust, coral, amber, terracotta",
            "pink, blush, rose, fuchsia, magenta, salmon",
            "purple, plum, violet, lavender, lilac, eggplant",
            "teal, turquoise, aqua, seafoam, cyan",
            "off-white, cream, eggshell, linen, parchment",
            "charcoal, graphite, ash, dark gray",
            "wood, oak, maple, pine, birch, mahogany",
            "metallic, chrome, brass, bronze, copper, steel"
        ];

        return new SynonymMap(name: _synonymMapName, synonyms: string.Join("\n", value));
    }

    private List<SearchField> GetSearchFields()
    {
        return
        [
            new SimpleField("id", SearchFieldDataType.String) { IsKey = true },

            new SearchableField("sku"),

            new SearchableField("name"),

            new SearchableField("description") { AnalyzerName = LexicalAnalyzerName.StandardLucene },

            new SearchableField("productSummary") { AnalyzerName = LexicalAnalyzerName.StandardLucene },

            new SimpleField("vectorRetrieved", SearchFieldDataType.Boolean) { IsFilterable = true },

            new SearchField("productSummaryVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
            {
                IsSearchable = true,
                VectorSearchDimensions = _embeddingDimensions,
                VectorSearchProfileName = _vectorProfileName
            },

            new SimpleField("price", SearchFieldDataType.Double) { IsFilterable = true },

            new SearchableField("category") { IsFacetable = true },

            new SearchableField("subcategory") { IsFacetable = true },

            new SimpleField("style", SearchFieldDataType.Collection(SearchFieldDataType.String)) { IsFacetable = true },

            new ComplexField("colors")
            {
                Fields =
                {
                    new SimpleField("primary", SearchFieldDataType.String),
                    new SimpleField("secondary", SearchFieldDataType.String),
                    new SimpleField("all_colors", SearchFieldDataType.Collection(SearchFieldDataType.String))
                }
            },

            new SearchableField("colorKeywords", collection: true)
            {
                IsFacetable = true,
                AnalyzerName = LexicalAnalyzerName.StandardLucene,
                SynonymMapNames = { _synonymMapName }
            },

            new SimpleField("materials", SearchFieldDataType.Collection(SearchFieldDataType.String)) { IsFacetable = true },

            new SimpleField("room_types", SearchFieldDataType.Collection(SearchFieldDataType.String)),

            new SimpleField("features", SearchFieldDataType.Collection(SearchFieldDataType.String)),

            new SimpleField("tags", SearchFieldDataType.Collection(SearchFieldDataType.String))
        ];
    }

    private SemanticSearch GetSemanticSearchConfiguration()
    {
        SemanticSearch semanticSearchSettings = new()
        {
            DefaultConfigurationName = _semanticConfigurationName,
        };

        SemanticPrioritizedFields semanticFields = new()
        {
            TitleField = new SemanticField("name")
        };

        semanticFields.ContentFields.Add(new SemanticField("productSummary"));

        semanticFields.KeywordsFields.Add(new SemanticField("style"));
        semanticFields.KeywordsFields.Add(new SemanticField("room_types"));
        semanticFields.KeywordsFields.Add(new SemanticField("materials"));
        semanticFields.KeywordsFields.Add(new SemanticField("colorKeywords"));
        semanticFields.KeywordsFields.Add(new SemanticField("tags"));
        semanticFields.KeywordsFields.Add(new SemanticField("features"));
        semanticFields.KeywordsFields.Add(new SemanticField("category"));
        semanticFields.KeywordsFields.Add(new SemanticField("subcategory"));

        SemanticConfiguration semanticConfig = new(_semanticConfigurationName, semanticFields)
        {
            RankingOrder = RankingOrder.BoostedRerankerScore
        };

        semanticSearchSettings.Configurations.Add(semanticConfig);

        return semanticSearchSettings;
    }

    private VectorSearch GetVectorSearchConfiguration()
    {
        VectorSearch vectorSearchSettings = new();

        vectorSearchSettings.Algorithms.Add(new HnswAlgorithmConfiguration(_vectorConfigName)
        {
            Parameters = new HnswParameters
            {
                Metric = VectorSearchAlgorithmMetric.Cosine,
                M = 4,
                EfConstruction = 400,
                EfSearch = 500
            }
        });

        vectorSearchSettings.Profiles.Add(new VectorSearchProfile(_vectorProfileName, _vectorConfigName)
        {
            VectorizerName = _vectorizerName
        });

        vectorSearchSettings.Vectorizers.Add(new AzureOpenAIVectorizer(_vectorizerName)
        {
            Parameters = new AzureOpenAIVectorizerParameters
            {
                ResourceUri = new Uri(_openAiConfig.Endpoint),
                ApiKey = _openAiConfig.Key,
                ModelName = _openAiConfig.EmbeddingModelName,
                DeploymentName = _openAiConfig.EmbeddingDeploymentName
            }
        });

        return vectorSearchSettings;
    }
}
