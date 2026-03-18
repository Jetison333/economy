namespace economy.Models;

using YamlDotNet.Serialization;

public class Recipe
{
    [YamlMember(Alias = "inputs")]
    public Dictionary<string, int> Inputs { get; set; } = new();

    [YamlMember(Alias = "outputs")]
    public Dictionary<string, int> Outputs { get; set; } = new();

    [YamlMember(Alias = "steps")]
    public int Steps { get; set; }

    public Recipe(Dictionary<string, int> input, Dictionary<string, int> output, int steps = 1)
    {
        Inputs = input;
        Outputs = output;
        Steps = steps;
    }

    public Recipe() { }
}
