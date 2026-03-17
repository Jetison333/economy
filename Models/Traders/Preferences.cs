namespace economy.Models;

public class Preferences
{
    public double ReserveBuilding { get; set; } = 0.5;
    public double ReserveOutput { get; set; } = 0.5;
    public double ReserveInput { get; set; } = 1.0;
    
    public string PriceSetter { get; set; } = "linear";
    public double MarketClearSlope { get; set; } = 0.05;
    public double PriceSlope { get; set; } = 1;
    public double PriceDevSlope { get; set; } = 4.0;
    
    public double RecipeChangeProb { get; set; } = 0.1;
}
