namespace economy.Models;

public class Company
{
    public Dictionary<string, int> Inventory { get; set; } = new();
    public int Money { get; set; } = 1000000;
    public string Type { get; set; }
    
    public Dictionary<string, int> Prices { get; set; } = new();
    public Dictionary<string, int> PricesDev { get; set; } = new();
    
    public Dictionary<string, int> Bids { get; set; } = new();
    public Dictionary<string, int> Asks { get; set; } = new();
    public Dictionary<string, int> MarketSucc { get; set; } = new();
    
    public Preferences Preferences { get; set; } = new();
    public bool DoLog { get; set; } = false;
    
    private Market _market;
    private Recipe _recipe;
    
    // Track in-progress recipes
    private class BuildingTask
    {
        public Recipe Recipe { get; set; }
        public int StepsCompleted { get; set; }
        
        public BuildingTask(Recipe recipe)
        {
            Recipe = recipe;
            StepsCompleted = 0;
        }
    }
    
    private Queue<BuildingTask> _buildingTasks = new Queue<BuildingTask>();
    private Random _random = new Random();

    public Company(string type, Market market)
    {
        Type = type;
        _market = market;
        Initialize();
        _recipe = GetBestRecipe();
    }

    private void Initialize()
    {
        foreach (var item in Recipes.Items)
        {
            Inventory[item] = 0;
            Prices[item] = _market.GetPrice(item); // default price
            PricesDev[item] = Prices[item] / 10;
            Bids[item] = 0;
            Asks[item] = 0;
            MarketSucc[item] = 0;
        }
        
    }

    public int GetBuildingValue()
    {
        return Inventory[Type] * GetPrice(Type);
    }

    public int GetOutputValue()
    {
        var val = 0;
        foreach (var item in _recipe.Output)
        {
            val += Inventory[item.Key] * GetPrice(item.Key);
        }
        return val;
    }

    public int GetPrice(string item)
    {
        return Prices.ContainsKey(item) ? Prices[item] : 0;
    }

    public int GetDev(string item)
    {
        return PricesDev.ContainsKey(item) ? PricesDev[item] : 0;
    }

    public int CalculateProfit(Recipe recipe)
    {
        var cost = 0;
        foreach (var item in recipe.Input)
        {
            cost += GetPrice(item.Key) * item.Value;
        }

        var revenue = 0;
        foreach (var item in recipe.Output)
        {
            revenue += GetPrice(item.Key) * item.Value;
        }

        return revenue - cost;
    }

    public Recipe GetBestRecipe()
    {
        var recipes = Recipes.RecipeBook[Type];
        var bestRecipe = new Recipe(new Dictionary<string, int>(), new Dictionary<string, int>()); // Default empty recipe
        var profit = 0;

        foreach (var recipe in recipes)
        {
            if (CalculateProfit(recipe) > profit)
            {
                bestRecipe = recipe;
                profit = CalculateProfit(recipe);
            }
        }

        return bestRecipe;
    }

    private bool HasInputs()
    {
        foreach (var item in _recipe.Input)
        {
            if (Inventory.GetValueOrDefault(item.Key, 0) < item.Value)
                return false;
        }
        return true;
    }

    public void Process()
    {
        int numBuildings = Inventory[Type];
        while (_buildingTasks.Count < numBuildings)
        {
            if (!HasInputs())
                break;

            _buildingTasks.Enqueue(new BuildingTask(_recipe));
            // Subtract inputs
            foreach (var item in _recipe.Input)
            {
                Inventory[item.Key] -= item.Value;
            }
        }

        var remainingTasks = new List<BuildingTask>();

        foreach (var task in _buildingTasks)
        {
            numBuildings--;
            if (numBuildings < 0) // more tasks than buildings
            {
                remainingTasks.Add(task);
                continue; 
            }
            // Execute one step of the task
            task.StepsCompleted++;

            // Finish task if task is complete
            if (task.StepsCompleted >= task.Recipe.Steps)
            {
                // Add outputs
                foreach (var item in _recipe.Output)
                {
                    if (!Inventory.ContainsKey(item.Key))
                        Inventory[item.Key] = 0;
                    Inventory[item.Key] += item.Value;
                }
            }
            else //continue task if not complete
            {
                remainingTasks.Add(task);
            }
        }

        // keep incomplete tasks
        _buildingTasks = new Queue<BuildingTask>(remainingTasks);
    }

    public void ResolveOrder(Order order, int realPrice)
    {
        var item = order.Item;
        int quantity = order.QuantityFilled;
        
        if (!MarketSucc.ContainsKey(item))
            MarketSucc[item] = 0;
        MarketSucc[item] += quantity;

        if (order.IsBid) // Buying items
        {
            if (!Inventory.ContainsKey(item))
                Inventory[item] = 0;
            Inventory[item] += quantity;
            Money -= realPrice * quantity;
        }
        else // Selling items
        {
            Money += realPrice * quantity;
            Inventory[item] -= quantity;
        }
    }

    private float MapValue(float value, float n, float x, float y)
    {
        return x + value / n * (y - x);
    }

    private List<(int price, int quantity)> CreateBuckets(string item, int totalQuantity, int numBuckets = 10)
    {
        var price = GetPrice(item);
        var dev = GetDev(item);
        var upper = price + dev;
        var lower = Math.Max(price - dev, 0);
        
        var quantityPerBucket = totalQuantity / numBuckets;
        var remainingQuantity = totalQuantity % numBuckets;
        
        var bucketList = new List<(int price, int quantity)>();
        for (int b = 0; b < numBuckets; b++)
        {
            var bucketPrice = (int)MapValue(b, numBuckets, lower, upper);
            var bucketQty = quantityPerBucket + (b < remainingQuantity ? 1 : 0);
            if (bucketQty > 0)
            {
                bucketList.Add((bucketPrice, bucketQty));
            }
        }
        return bucketList;
    }

    public void SendBids()
    {
        const int buckets = 10;
        
        // calculate bucket data for all items and total cost
        var bidBuckets = new Dictionary<string, List<(int price, int quantity)>>();
        var totalCost = 0;
        
        foreach (var item in Bids)
        {
            if (item.Value > 0)
            {
                var bucketList = CreateBuckets(item.Key, item.Value, buckets);
                bidBuckets[item.Key] = bucketList;
                
                foreach (var (bucketPrice, bucketQty) in bucketList)
                {
                    totalCost += bucketPrice * bucketQty;
                }
            }
        }
        
        // adjust quantities if total cost exceeds money
        if (totalCost > Money)
        {
            var frac = (float)Money / totalCost;
            
            foreach (var item in bidBuckets)
            {
                var itemBuckets = item.Value;
                var itemCost = itemBuckets.Sum(b => b.price * b.quantity);
                var allowedCost = (int)(itemCost * frac);
                var currentCost = itemCost;
                
                // Remove from most expensive buckets first
                for (int i = itemBuckets.Count - 1; i >= 0 && currentCost > allowedCost; i--)
                {
                    var (bucketPrice, bucketQty) = itemBuckets[i];
                    var excessCost = currentCost - allowedCost;
                    var qtyToRemove = Math.Min(bucketQty, excessCost / bucketPrice);
                    
                    itemBuckets[i] = (bucketPrice, bucketQty - qtyToRemove);
                    currentCost -= qtyToRemove * bucketPrice;
                }
            }
        }
        
        // create orders
        foreach (var item in bidBuckets)
        {
            foreach (var (bucketPrice, bucketQty) in item.Value)
            {
                if (bucketQty > 0)
                {
                    _market?.AddOrder(new Order(item.Key, bucketPrice, bucketQty, true, ResolveOrder));
                }
            }
        }
    }

    public void SendAsks()
    {
        const int buckets = 10;
        
        foreach (var item in Asks)
        {
            var quantity = Math.Min(item.Value, Inventory.GetValueOrDefault(item.Key, 0));
            
            if (quantity > 0)
            {
                var bucketList = CreateBuckets(item.Key, quantity, buckets);
                
                foreach (var (bucketPrice, bucketQty) in bucketList)
                {
                    _market?.AddOrder(new Order(item.Key, bucketPrice, bucketQty, false, ResolveOrder));
                }
            }
        }
    }

    public void SendOrders()
    {
        SendBids();
        SendAsks();
    }

    public void UpdatePrices()
    {
        if (DoLog)
        {
            Console.WriteLine($"MarketSucc:    {string.Join(", ", MarketSucc)}");
        }
        foreach (var item in Recipes.Items)
        {
            if (Bids.GetValueOrDefault(item, 0) + Asks.GetValueOrDefault(item, 0) + MarketSucc.GetValueOrDefault(item, 0) == 0)
            {
                var slope = Preferences.MarketClearSlope;
                var marketPrice = _market?.GetPrice(item) ?? Prices[item];
                Prices[item] = (int)((1 - slope) * Prices[item] + slope * marketPrice);
            }
            else
            {
                var succFrac = (float)MarketSucc.GetValueOrDefault(item, 0) / 
                              (MarketSucc.GetValueOrDefault(item, 0) + Bids.GetValueOrDefault(item, 0) + Asks.GetValueOrDefault(item, 0));
                
                if (Bids.GetValueOrDefault(item, 0) > 0)
                {
                    Prices[item] += (int)((0.5 - succFrac) * PricesDev[item] * Preferences.PriceSlope);
                }
                else
                {
                    Prices[item] -= (int)((0.5 - succFrac) * PricesDev[item] * Preferences.PriceSlope);
                }
                var olddev = PricesDev[item];
                PricesDev[item] += (int)((Math.Abs(2.0 * succFrac - 1.0) * 2.0 - 1.0) * Preferences.PriceDevSlope);
                if (PricesDev[item] < 0)
                {
                    PricesDev[item] = 0;
                }
            }

            if (PricesDev[item] * 2 > Prices[item])
            {
                PricesDev[item] = Prices[item] / 2;
            }

            Bids[item] = 0;
            Asks[item] = 0;
            MarketSucc[item] = 0;
        }
    }

    public void MakeBid(string item, int num)
    {
        if (num <= 0)
            throw new ArgumentException("Use MakeAsk instead to sell items");
        
        if (!Bids.ContainsKey(item))
            Bids[item] = 0;
        Bids[item] += num;
    }

    public void MakeAsk(string item, int num)
    {
        if (!Asks.ContainsKey(item))
            Asks[item] = 0;
        Asks[item] = num;
    }

    private void BuyInputs(List<string> processed)
    {
        foreach (var item in _recipe.Input)
        {
            processed.Add(item.Key);
            var numberBuy = (int)(item.Value * Preferences.ReserveInput * Inventory[Type]) - Inventory.GetValueOrDefault(item.Key, 0);
            if (numberBuy <= 0)
                continue;
            MakeBid(item.Key, numberBuy);
        }
    }

    private void SellOutputs(List<string> processed)
    {
        var currentOutputValue = GetOutputValue();
        var targetOutputValue = (int)(Preferences.ReserveOutput * Money);
        
        if (currentOutputValue <= targetOutputValue)
            return;
        
        var excessValue = currentOutputValue - targetOutputValue;
        var frac = (float)excessValue / currentOutputValue;

        foreach (var item in _recipe.Output)
        {
            processed.Add(item.Key);
            var numberSell = (int)(Inventory.GetValueOrDefault(item.Key, 0) * frac);
            if (numberSell <= 0)
                continue;
            MakeAsk(item.Key, numberSell);
        }
    }

    private void SellLeftovers(List<string> processed)
    {
        foreach (var item in Inventory)
        {
            if (processed.Contains(item.Key))
                continue;
            MakeAsk(item.Key, item.Value);
        }
    }

    private void BalanceBuildings(List<string> processed)
    {
        processed.Add(Type);
        var targetBuildingValue = (int)(Money * Preferences.ReserveBuilding);
        var numberBuy = (int)(targetBuildingValue / GetPrice(Type)) - Inventory[Type];
        
        if (numberBuy == 0)
            return;
        
        if (numberBuy > 0)
        {
            MakeBid(Type, numberBuy);
        }
        else
        {
            MakeAsk(Type, -numberBuy);
        }
    }

    public void Market()
    {
        var processed = new List<string>();

        UpdatePrices();
        BuyInputs(processed);
        SellOutputs(processed);
        BalanceBuildings(processed);
        SellLeftovers(processed);
        SendOrders();
    }

    public void Log()
    {
        if (DoLog)
        {
            Console.WriteLine();
            Console.WriteLine($"money:      {Money}");
            Console.WriteLine($"Recipe:   {string.Join(", ", _recipe.Input.Select(i => $"{i.Value} {i.Key}"))} -> {string.Join(", ", _recipe.Output.Select(i => $"{i.Value} {i.Key}"))}");
            Console.WriteLine($"prices:     {string.Join(", ", Prices)}");
            Console.WriteLine($"prices_dev: {string.Join(", ", PricesDev)}");
            Console.WriteLine($"inventory:  {string.Join(", ", Inventory)}");
            Console.WriteLine($"asks:       {string.Join(", ", Asks)}");
            Console.WriteLine($"bids:       {string.Join(", ", Bids)}");
        }
    }

    public void RemoveEphemeralItems()
    {
        foreach (var item in Recipes.EmphemeralItems)
        {
            Inventory[item] = 0;
        }
    }

    public void Planning()
    {
        if (_random.NextDouble() < Preferences.RecipeChangeProb)
        {
            _recipe = GetBestRecipe();
        }

        if (_recipe.Output.Keys.Any(k => Recipes.EmphemeralItems.Contains(k)))
        {
            Process();
        }

        Market();
    }

    public void Step()
    {

        if (!_recipe.Output.Keys.Any(k => Recipes.EmphemeralItems.Contains(k)))
        {
            Process();
        }
        Log();
    }
}
