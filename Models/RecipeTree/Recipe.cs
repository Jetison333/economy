namespace economy.Models;

public class Recipe
{
    public Dictionary<string, int> Input { get; set; }
    public Dictionary<string, int> Output { get; set; }
    public int Steps { get; set; }

    public Recipe(Dictionary<string, int> input, Dictionary<string, int> output, int steps = 1)
    {
        Input = input;
        Output = output;
        Steps = steps;
    }
}
