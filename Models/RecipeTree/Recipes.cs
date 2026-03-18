using YamlDotNet.Serialization;

namespace economy.Models;

public class RecipeData
{
    [YamlMember(Alias = "items")]
    public List<string> Items { get; set; } = new();

    [YamlMember(Alias = "emphemeral_items")]
    public List<string> EmphemerealItems { get; set; } = new();

    [YamlMember(Alias = "recipes")]
    public Dictionary<string, List<Recipe>> Recipes { get; set; } = new();
}

public static class Recipes
{
    public static readonly string[] Items;

    public static readonly string[] EmphemeralItems;

    public static readonly Dictionary<string, List<Recipe>> RecipeBook;

    static Recipes()
    {
        string yaml = ReadFile();
        var deserializer = new DeserializerBuilder().Build();
        var data = deserializer.Deserialize<RecipeData>(yaml);

        if (data == null)
            throw new InvalidOperationException("Failed to deserialize recipes YAML");

        Items = data.Items.ToArray();
        EmphemeralItems = data.EmphemerealItems.ToArray();
        RecipeBook = data.Recipes;
    }

    private static string ReadFile()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models/RecipeTree/Recipes.yaml");
        return File.ReadAllText(path);
    }
}

