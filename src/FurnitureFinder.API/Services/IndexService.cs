using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;

namespace FurnitureFinder.API.Services;

public class IndexService(IOptions<SearchConfig> searchConfig, IOptions<OpenAIConfig> openAiConfig, ILogger<IndexService> logger)
    : IIndexService
{
    private readonly SearchClient _searchClient = new(
            new Uri(searchConfig.Value.Endpoint),
            searchConfig.Value.IndexName,
            new AzureKeyCredential(searchConfig.Value.Key));

    private readonly string _synonymMapName = "color-synonym-map";

    private readonly string _semanticConfigurationName = "default";

    private readonly string _vectorConfigName = "product-summary-vector-config";
    private readonly string _vectorProfileName = "product-summary-vector-profile";
    private readonly string _vectorizerName = "product-summary-vectorizer";
    private readonly int _embeddingDimensions = openAiConfig.Value.EmbeddingDimensions;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public async Task CreateIndexAsync(CancellationToken cancellationToken = default)
    {
        //Create Synonym Map
        SynonymMap synonymMap = CreateSynonymMap();

        //Get Search Fields
        List<SearchField> fields = GetSearchFields();

        //Get Semantic Configuration
        SemanticSearch semanticSearchConfiguration = GetSemanticSearchConfiguration();

        //Get Vectors Configuration
        VectorSearch vectorSearchConfiguration = GetVectorSearchConfiguration();

        //TODO: Get Embedding Skillset

        //TODO: Enable autocomplete and suggestions

        //Create Index
        var index = new SearchIndex(searchConfig.Value.IndexName)
        {
            Fields = fields,
            Similarity = new BM25Similarity(),
            SemanticSearch = semanticSearchConfiguration,
            VectorSearch = vectorSearchConfiguration
        };

        var adminClient = new SearchIndexClient(new Uri(searchConfig.Value.Endpoint), new AzureKeyCredential(searchConfig.Value.Key));

        await adminClient.CreateOrUpdateSynonymMapAsync(synonymMap, cancellationToken: cancellationToken);

        await adminClient.DeleteIndexAsync(index, cancellationToken: cancellationToken);

        await adminClient.CreateOrUpdateIndexAsync(index, cancellationToken: cancellationToken);
    }

    public async Task MergeOrUploadProductsAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default)
    {
        if (products == null || !products.Any())
        {
            products = JsonSerializer.Deserialize<List<Product>>(File.ReadAllText("sample data/sample_furniture_data.json"), _jsonSerializerOptions) ?? [];
        }

        IndexDocumentsResult result = await _searchClient.MergeOrUploadDocumentsAsync<Product>(products, cancellationToken: cancellationToken);

        result.Results.ToList().ForEach(r =>
        {
            if (!r.Succeeded)
            {
                logger.LogWarning("Failed to index document with key: {Key}, Error: {ErrorMessage}", r.Key, r.ErrorMessage);
            }
        });
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

        return new SynonymMap(
            name: _synonymMapName,
            synonyms: string.Join("\n", value));
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

        //vectorSearchSettings.Compressions.Add(new BinaryQuantizationCompression(""));

        vectorSearchSettings.Vectorizers.Add(new AzureOpenAIVectorizer(_vectorizerName)
        {
            Parameters = new AzureOpenAIVectorizerParameters
            {
                ResourceUri = new Uri(openAiConfig.Value.Endpoint),
                ApiKey = openAiConfig.Value.Key,
                ModelName = openAiConfig.Value.EmbeddingModelName,
                DeploymentName = openAiConfig.Value.EmbeddingDeploymentName
            }
        });

        return vectorSearchSettings;
    }
}
