namespace economy.Models;

public class Market
{
    public Dictionary<string, OrderBook> Orders { get; set; } = new();
    
    public Dictionary<string, int> Prices { get; set; } = new()
    {
        { "iron", 400 },
        { "crafter", 1000 }
    };

    public Dictionary<string, int> Volume { get; set; } = new();

    public List<Company> _companies = new();

    public Market()
    {
        foreach (var item in Recipes.Items)
        {
            Orders[item] = new OrderBook();
            Volume[item] = 0;
        }

        for (int i = 0; i < 1000; i++)
        {
            var company = new Company("crafter", this);
            company.Inventory["crafter"] = 1;
            _companies.Add(company);
        }
    }

    public void AddOrder(Order order)
    {
        Orders[order.Item].AddOrder(order);
    }

    public int GetPrice(string item)
    {
        return Prices.ContainsKey(item) ? Prices[item] : 0;
    }

    private void SortOrders(List<Order> buys, List<Order> sells)
    {
        buys.Sort((a, b) => b.Price.CompareTo(a.Price)); // descending
        sells.Sort((a, b) => a.Price.CompareTo(b.Price)); // ascending
    }

    private int MatchOrders(List<Order> buys, List<Order> sells, string item)
    {
        var clearingPrice = Prices[item];
        var bi = 0;
        var si = 0;

        while (bi < buys.Count && si < sells.Count)
        {
            if (buys[bi].Price < sells[si].Price)
            {
                break;
            }

            var buyOrder = buys[bi];
            var sellOrder = sells[si];
            
            clearingPrice = (buyOrder.Price + sellOrder.Price) / 2;

            var buyQtyRemaining = buyOrder.QuantityRemaining;
            var sellQtyRemaining = sellOrder.QuantityRemaining;
            var qtyFilled = Math.Min(buyQtyRemaining, sellQtyRemaining);
            Volume[item] += qtyFilled;

            buyOrder.QuantityFilled += qtyFilled;
            sellOrder.QuantityFilled += qtyFilled;

            if (buyOrder.QuantityRemaining == 0)
                bi += 1;
            if (sellOrder.QuantityRemaining == 0)
                si += 1;
        }

        return clearingPrice;
    }

    private void SettleOrders(List<Order> buys, List<Order> sells, int clearingPrice)
    {
        
        foreach (Order order in buys.Concat(sells))
        {
            order.Callback?.Invoke(order, clearingPrice);
        }
    }

    public void Resolve()
    {
        foreach (var item in Recipes.Items)
        {
            var buys = new List<Order>(Orders[item].Bids);
            var sells = new List<Order>(Orders[item].Asks);

            SortOrders(buys, sells);
            int clearingPrice = MatchOrders(buys, sells, item);
            SettleOrders(buys, sells, clearingPrice);

            Prices[item] = clearingPrice;
            
            Orders[item].Clear();
        }
    }

    public void Step()
    {
        foreach( var item in Recipes.Items)
        {
            Volume[item] = 0;
        }
        
        
        foreach (var company in _companies)
        {
            company.Step();
        }
        Resolve();

        if (Prices["iron"] < 100)
        {
            foreach( var company in _companies)
            {
                company.Money = (int) (company.Money * 1.1);
            }
        }
        

        //Console.WriteLine($"prices:     {string.Join(", ", Prices)}");
        //Console.WriteLine($"volume:     {string.Join(", ", Volume)}");
    }
}
