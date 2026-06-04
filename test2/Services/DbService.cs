using Microsoft.EntityFrameworkCore;
using test2.Data;
using test2.DTOs;
using test2.Exceptions;

namespace test2.Services;

public class DbService:IDbService
{
    private readonly DatabaseContext _context;

    public DbService(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<GetOrderDto> GetOrder(int orderId)
    {
        var order = await _context.Orders
            .Select(e => new GetOrderDto
            {
                Id = e.Id,
                CreatedAt = e.CreatedAt,
                FulfilledAt = e.FulfilledAt,
                Status = e.Status,
                Client = new Client()
                {
                    FirstName = e.Client.FirstName,
                    LastName = e.Client.LastName,
                },
                Products = e.ProductOrders.Select(e=>new OrderLine()
                {
                    Name = e.Product.Name,
                    Price = e.Product.Price,
                    Amount = e.Amount
                }).ToList()
                
                
            }).FirstOrDefaultAsync(e=>e.Id == orderId);
        if (order is null)
            throw new NotFoundException();
        
        return order;
    }

    public async Task CreateOrder(int orderId, PostOrderDto dto)
    {
        using var  transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = await  _context.Orders
                .FirstOrDefaultAsync(e => e.Id == orderId);

            if (order is null)
            {
                throw new NotFoundException("not found");
            }
            
            var status = await _context.Statuses.FirstOrDefaultAsync(e => e.Name.Equals(dto.StatusName));
            if (status is null) throw new NotFoundException("not found");
            
            if(order.FulfilledAt !=null) throw new NotFoundException("not found");
            
            order.StatusId = status.Id;
            order.FulfilledAt = DateTime.Now;
            var relatedProducts = _context.ProdOrders.Where(e => e.OrderId == orderId);
            _context.ProdOrders.RemoveRange(relatedProducts);
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}