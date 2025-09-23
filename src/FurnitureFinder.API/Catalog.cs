namespace FurnitureFinder.API;

public class Catalog
{
    public required List<Product> Products { get; set; } = [];
}

public class Product
{
    public int Id { get; set; }

    public required string SKU { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public  required string Category { get; set; }

    public required string Subcategory { get; set; }

    public string[] Style { get; set; } = [];

    public required Colors Colors { get; set; }

    public string[] Materials { get; set; } = [];

    public string[] RoomTypes { get; set; } = [];

    public string[] Reatures { get; set; } = [];

    public string[] Tags { get; set; } = [];
}

public class Colors
{
    public required string Primary { get; set; }

    public string? Secondary { get; set; }

    public string[] AllColors { get; set; } = [];
}
