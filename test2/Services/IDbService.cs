using test2.DTOs;

namespace test2.Services;

public interface IDbService
{
    Task<GetOrderDto> GetOrder(int orderId);
    Task CreateOrder(int orderId, PostOrderDto dto);
}