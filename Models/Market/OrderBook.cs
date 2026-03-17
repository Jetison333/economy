namespace economy.Models;

public class OrderBook
{
    public List<Order> Bids { get; set; } = [];
    public List<Order> Asks { get; set; } = [];

    public void AddOrder(Order order)
    {
        if (order.IsBid)
        {
            Bids.Add(order);
        }
        else
        {
            Asks.Add(order);
        }
    }

    public void Clear()
    {
        Bids.Clear();
        Asks.Clear();
    }
}
