namespace test2.DTOs;

public class GetOrderDto
{

    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? FulfilledAt { get; set; }
    public string Status { get; set; }
    public Client Client { get; set; }
    public IEnumerable<OrderLine> Products { get; set; }
}

public class Client
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class OrderLine
{
    public string Name { get; set; }
    public double Price { get; set; }
    public int Amount { get; set; }
}


