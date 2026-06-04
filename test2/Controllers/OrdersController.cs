using test2.Exceptions;
using Microsoft.AspNetCore.Mvc;
using test2.DTOs;
using test2.Services;

namespace test2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController:ControllerBase
{
    private readonly IDbService _dbService;

    public OrdersController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        try
        {
            var order = await _dbService.GetOrder(id);
            return Ok(order);
        }
        catch (NotFoundException e)
        {
            return NotFound();
        }
    }

    [HttpPut("{orderId}/fulfill")]
    public async Task<IActionResult> FulfillOrder(int orderId, PostOrderDto dto)
    {
        try
        {
            await _dbService.CreateOrder(orderId, dto);
            return Ok();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (ConflictException e)
        {
            return Conflict(e.Message);
        }
    }
}