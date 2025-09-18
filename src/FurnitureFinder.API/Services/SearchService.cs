using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System.Text.RegularExpressions;

namespace FurnitureFinder.API.Services;

public class SearchService(IOptions<AzureConfiguration> configOptions)
    : ISearchService
{
    private readonly SearchClient _searchClient = new(
            new Uri(configOptions.Value.Search.Endpoint),
            configOptions.Value.Search.IndexName,
            new Azure.AzureKeyCredential(configOptions.Value.Search.Key));

    private readonly string _semanticConfigurationName = configOptions.Value.Search.SemanticConfigurationName;

    public async Task<(string, List<ProductSearchResult>)> FindComplementaryFurnitureAsync(FurnitureAnalysisResult analysis,
        RecommendationRequest request, CancellationToken cancellationToken = default)
    {
        // Extract category from search text (e.g., "rugs" from "show me rugs that go with this chair")
        //string? targetCategory = ExtractTargetCategory(request.SearchText);

        var tags = ExtractTags(analysis.OpenAIConciseDescription);

        var options = new SearchOptions
        {
            Size = 20,
            QueryType = SearchQueryType.Semantic, // Enable semantic search
            SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = _semanticConfigurationName,
            },
            Filter = BuildFilterExpression(tags, targetCategory: null),
        };

        // Use only the search intent for semantic search
        //string query = request.SearchText ?? string.Empty;
        string query = GenerateSearchText(tags, analysis.Description);

        // Execute search
        var results = await _searchClient.SearchAsync<SearchDocument>(query, options, cancellationToken);

        var recommendations = new List<ProductSearchResult>();

        // Collect all results first
        var allResults = new List<(SearchResult<SearchDocument> Result, ProductSearchResult Product)>();

        await foreach (var result in results.Value.GetResultsAsync())
        {
            // For semantic search, use RerankerScore with threshold around 1.0-1.5
            if (result.SemanticSearch?.RerankerScore == null)
                continue;

            SearchDocument doc = result.Document;

            var product = new ProductSearchResult(doc.GetString("id"),
                doc.GetString("TopCategory"),
                doc.GetString("Category"),
                doc.GetString("AdvertisingCopy"),
                doc.GetString("DescriptionTitle"),
                doc.GetString("Decor"),
                doc.GetString("Theme"),
                doc.GetString("PrimaryColor"),
                Dimensions: default,
                doc.GetString("DetailCategory"),
                doc.GetString("ReferenceColor"),
                doc.GetString("ProductFactoryFinish"),

                doc.ContainsKey("Attributes") && doc["Attributes"] is IEnumerable<object> attributeObjects
                    ? [.. attributeObjects.Select(a =>
                    {
                        var attributeObject = new SearchDocument(a as IDictionary<string, object>);
                        return new ProductAttribute(attributeObject.GetString("AttributeName"), attributeObject.GetString("AttributeValues"));
                    })]
                    : [],

                doc.GetString("Material"),
                doc.GetString("SKU"),
                doc.GetString("PrimaryFinish"),

                doc.ContainsKey("Assets") && doc["Assets"] is IEnumerable<object> assetObjects
                    ? [.. assetObjects.Select(a =>
                    {
                        var assetDoc = new SearchDocument(a as IDictionary<string, object>);
                        return new Asset(assetDoc.GetInt32("DivisionId"), assetDoc.GetString("AssetId"), assetDoc.GetString("AssetUsage"), assetDoc.GetString("AssetType"));
                    })]
                    : []);

            allResults.Add((result, product));
        }

        // Sort by RerankerScore in descending order and take the products
        recommendations = allResults
            .OrderByDescending(x => x.Product.Assets?.Any() == true) // Items with assets first
                                                                     //.ThenByDescending(x => x.Result.Score) // Then by semantic score
            .ThenByDescending(x => x.Result.SemanticSearch?.RerankerScore) // Then by semantic score
                                                                           //.OrderByDescending(x => x.Result.Score) // Then by semantic score
                                                                           //.ThenByDescending(x => x.Result.SemanticSearch?.RerankerScore) // Then by semantic score
            .Select(x => x.Product)
            .ToList();

        //return (query, recommendations);
        return ($"Filter: {options.Filter}: Query: {query}", recommendations);
    }

    private static string GenerateSearchText(FurnitureTags tags, string aiDescription)
    {
        // Generate search text from OpenAIConciseDescription
        var searchParts = new List<string>();

        //// Add furniture type if available
        //if (!string.IsNullOrWhiteSpace(tags.FurnitureType))
        //{
        //    searchParts.Add($"Find items in {tags.FurnitureType} category");
        //}

        if (!string.IsNullOrWhiteSpace(aiDescription))
        {
            searchParts.Add($"Find items that match {aiDescription}");
        }

        // Add style preferences
        if (tags.Styles?.Length > 0)
        {
            searchParts.Add($"in {string.Join(" or ", tags.Styles)} theme or decor");
        }

        // Add color preferences
        if (tags.Colors?.Length > 0)
        {
            searchParts.Add($"with {string.Join(" or ", tags.Colors)} colors");
        }

        //// Add material preferences
        //if (tags.Materials?.Length > 0)
        //{
        //    searchParts.Add($"made of {string.Join(" or ", tags.Materials)}");
        //}

        return string.Join(" ", searchParts);
    }

    private static string? BuildFilterExpression(FurnitureTags tags, string? targetCategory)
    {
        var filters = new List<string>();
        filters.Add($"search.in(TopCategory, 'ADULT', '|')");

        ////Filter by target category if specified
        //if (!string.IsNullOrWhiteSpace(tags.FurnitureType))
        //{
        //    //filters.Add($"search.in(Category, '{tags.FurnitureType}', '|') and (search.in(TopCategory, 'ADULT', '|') or search.in(TopCategory, 'KID', '|'))");
        //    filters.Add($"search.in(Category, '{tags.FurnitureType}', '|') and search.in(TopCategory, 'ADULT', '|')");
        //}
        //else
        //{
        //    filters.Add($"search.in(TopCategory, 'ADULT', '|') or search.in(TopCategory, 'KID', '|')");
        //}

        //// Add color filter if available
        //if (tags.Colors.Length > 0)
        //{
        //    var escapedColors = string.Join("|", tags.Colors.Select(c => c.Trim().Replace("'", "''")));
        //    filters.Add($"search.in(PrimaryColor, '{escapedColors}', '|') or search.in(ReferenceColor, '{escapedColors}', '|')");
        //}

        //// Add material filter if available
        //if (tags.Materials?.Length > 0)
        //{
        //    var escapedMaterials = string.Join("|", tags.Materials.Select(m => m.Trim().Replace("'", "''")));
        //    filters.Add($"search.in(Material, '{escapedMaterials}', '|')");
        //}

        //// Add style/theme filter if available
        //if (tags.Styles?.Length > 0)
        //{
        //    var escapedStyles = string.Join("|", tags.Styles.Select(s => s.Trim().Replace("'", "''")));
        //    filters.Add($"search.in(Theme, '{escapedStyles}', '|') or search.in(Decor, '{escapedStyles}', '|')");
        //}

        return filters.Count > 0 ? string.Join(" and ", filters) : null;
    }

    private static FurnitureTags ExtractTags(string openAIConciseDescription)
    {
        FurnitureTags furnitureTags = new();

        if (string.IsNullOrWhiteSpace(openAIConciseDescription))
            return furnitureTags;

        // Regex pattern to capture key-value pairs
        string pattern = @"(?<key>Furniture Type|Style|Color|Material)\s*:\s*(?<value>.+)";
        var matches = Regex.Matches(openAIConciseDescription, pattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            string key = match.Groups["key"].Value.Trim();
            string value = match.Groups["value"].Value.Trim();

            if (string.IsNullOrEmpty(value))
                continue;

            switch (key.ToLowerInvariant())
            {
                case "furniture type":
                    furnitureTags.FurnitureType = value;
                    break;
                case "style":
                    furnitureTags.Styles = value.Split(",");
                    break;
                case "color":
                    furnitureTags.Colors = value.Split(",");
                    break;
                case "material":
                    furnitureTags.Materials = value.Split(",");
                    break;
            }
        }

        return furnitureTags;
    }

    private static string? ExtractTargetCategory(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return null;

        Dictionary<string, List<string>> _categorySynonyms = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "ACCENT", new List<string>
                {
                    "Side table",
                    "End table",
                    "Console",
                    "Entryway piece",
                    "Decorative furniture",
                    "Statement piece"
                }
            },
            {
                "ACCESSORIES", new List<string>
                {
                    "Decor items",
                    "Trinkets",
                    "Add-ons",
                    "Ornaments",
                    "Small decor",
                    "Finishing touches"
                }
            },
            {
                "APPLIANCE", new List<string>
                {
                    "Machine",
                    "Device",
                    "Equipment",
                    "Household gadget",
                    "Power tool",
                    "Utility item"
                }
            },
            {
                "BAR", new List<string>
                {
                    "Wine rack",
                    "Liquor cabinet",
                    "Bar cart",
                    "Drink station",
                    "Cocktail table",
                    "Pub furniture"
                }
            },
            {
                "BEDDING EXTRAS", new List<string>
                {
                    "Pillows",
                    "Duvet",
                    "Comforter",
                    "Mattress topper",
                    "Bedspread",
                    "Linens"
                }
            },
            {
                "BEDROOM", new List<string>
                {
                    "Bed frame",
                    "Nightstand",
                    "Dresser",
                    "Wardrobe",
                    "Chest of drawers",
                    "Boudoir furniture"
                }
            },
            {
                "DINING", new List<string>
                {
                    "Dining table",
                    "Dining chairs",
                    "Table set",
                    "Eat-in",
                    "Kitchen table",
                    "Banquette"
                }
            },
            {
                "DINING ROOM", new List<string>
                {
                    "Formal dining",
                    "Buffet",
                    "Hutch",
                    "China cabinet",
                    "Sideboard",
                    "Dining suite"
                }
            },
            {
                "ELECTRONICS", new List<string>
                {
                    "TV",
                    "Stereo",
                    "Speaker",
                    "Sound system",
                    "Smart device",
                    "Entertainment tech"
                }
            },
            {
                "FOUNDATION", new List<string>
                {
                    "Box spring",
                    "Bed base",
                    "Support frame",
                    "Underbed support",
                    "Mattress base"
                }
            },
            {
                "HEAT", new List<string>
                {
                    "Heater",
                    "Fireplace",
                    "Radiator",
                    "Space heater",
                    "Heating unit"
                }
            },
            {
                "HOME ACCENT", new List<string>
                {
                    "Vase",
                    "Sculpture",
                    "Tray",
                    "Candle holder",
                    "Wall shelf",
                    "Decorative bowl"
                }
            },
            {
                "HOME DECOR", new List<string>
                {
                    "Art",
                    "Wall hanging",
                    "Mirror",
                    "Clock",
                    "Picture frame",
                    "Centerpiece"
                }
            },
            {
                "HOME ENTERTAINMENT", new List<string>
                {
                    "Media console",
                    "TV stand",
                    "Game cabinet",
                    "Home theater",
                    "Entertainment center"
                }
            },
            {
                "HOME OFFICE", new List<string>
                {
                    "Desk",
                    "Office chair",
                    "Filing cabinet",
                    "Workstation",
                    "Study table",
                    "Computer desk"
                }
            },
            {
                "INFANT", new List<string>
                {
                    "Crib",
                    "Changing table",
                    "Bassinet",
                    "Nursery",
                    "Baby furniture",
                    "Rocker"
                }
            },
            {
                "KITCHEN APPLIANCE", new List<string>
                {
                    "Fridge",
                    "Oven",
                    "Microwave",
                    "Blender",
                    "Toaster",
                    "Dishwasher"
                }
            },
            {
                "LAMP", new List<string>
                {
                    "Light",
                    "Lighting",
                    "Table lamp",
                    "Floor lamp",
                    "Desk lamp",
                    "Shade light"
                }
            },
            {
                "LAUNDRY APPLIANCE", new List<string>
                {
                    "Washer",
                    "Dryer",
                    "Laundry machine",
                    "Clothes cleaner",
                    "Utility appliance"
                }
            },
            {
                "LEGACY", new List<string>
                {
                    "Antique",
                    "Vintage",
                    "Heirloom",
                    "Classic",
                    "Traditional",
                    "Heritage"
                }
            },
            {
                "LIVING ROOM", new List<string>
                {
                    "Sofa",
                    "Couch",
                    "Coffee table",
                    "Recliner",
                    "Sectional",
                    "TV stand"
                }
            },
            {
                "MAINTENANCE", new List<string>
                {
                    "Repair kit",
                    "Cleaning supplies",
                    "Tools",
                    "Furniture care",
                    "Polish",
                    "Fixing items"
                }
            },
            {
                "MATTRESS", new List<string>
                {
                    "Foam bed",
                    "Spring mattress",
                    "Sleep surface",
                    "Bed cushion",
                    "Memory foam"
                }
            },
            {
                "MATTRESS SET", new List<string>
                {
                    "Bed set",
                    "Mattress and box spring",
                    "Sleep system",
                    "Complete bed",
                    "Mattress combo"
                }
            },
            {
                "OCCASIONAL", new List<string>
                {
                    "Side table",
                    "Nesting table",
                    "Accent table",
                    "Coffee table",
                    "Occasional chair"
                }
            },
            {
                "RUG", new List<string>
                {
                    "Rug",
                    "Carpet",
                    "Runner",
                    "Mat",
                    "Area rug",
                    "Floor covering"
                }
            },
            {
                "SEATING", new List<string>
                {
                    "Chair",
                    "Stool",
                    "Bench",
                    "Recliner",
                    "Armchair",
                    "Loveseat"
                }
            },
            {
                "SHADE", new List<string>
                {
                    "Lampshade",
                    "Window shade",
                    "Curtain",
                    "Blind",
                    "Light cover"
                }
            },
            {
                "SUPPLIES", new List<string>
                {
                    "Stock",
                    "Inventory",
                    "Essentials",
                    "Materials",
                    "Replacements"
                }
            },
            {
                "WALL DECOR", new List<string>
                {
                    "Wall art",
                    "Painting",
                    "Poster",
                    "Hanging",
                    "Tapestry",
                    "Wall sculpture"
                }
            }
        };

        foreach (var kvp in _categorySynonyms)
        {
            if (kvp.Value.Any(synonym => searchText.Contains(synonym, StringComparison.OrdinalIgnoreCase)))
            {
                return kvp.Key;
            }
        }

        return null;
    }

    public class FurnitureTags
    {
        public string FurnitureType { get; set; } = string.Empty;

        public string[] Colors { get; internal set; }

        public string[] Materials { get; internal set; }

        public string[] Styles { get; internal set; }
    }
}
