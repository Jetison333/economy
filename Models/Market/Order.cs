namespace economy.Models;

public class Order
{
    public string Item { get; set; }
    public int Price { get; set; }
    public int Quantity { get; set; }
    public int QuantityFilled { get; set; }
    public bool IsBid { get; set; }
    public Action<Order, int>? Callback { get; set; }

    public Order(string item, int price, int quantity, bool isBid, Action<Order, int>? callback = null)
    {
        Item = item;
        Price = price;
        Quantity = quantity;
        QuantityFilled = 0;
        IsBid = isBid;
        Callback = callback;
    }

    public int QuantityRemaining => Quantity - QuantityFilled;
}
