namespace economy.Models;

public static class Recipes
{
    public static readonly string[] Items = { "crafter", "iron" };

    public static readonly Dictionary<string, List<Recipe>> RecipeBook = new()
    {
        {
            "crafter", new List<Recipe>
            {
                new Recipe(
                    new Dictionary<string, int>(),
                    new Dictionary<string, int> { { "iron", 1 } },
                    steps: 1
                ),
                new Recipe(
                    new Dictionary<string, int> { { "iron", 5 } },
                    new Dictionary<string, int> { { "crafter", 1 } },
                    steps: 5
                )
            }
        }
    };
}
