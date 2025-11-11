using Microsoft.AspNetCore.Mvc;
using UrlShortener.UserService.Application.Interfaces;
using UrlShortener.UserService.Application.Requests;
using FluentValidation;

namespace UrlShortener.UserService.Presentation.Controllers;

public record AuthenticateRequest(string Email, string Password);

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        try
        {
            var created = await service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ValidationException vx)
        {
            return BadRequest(vx.Errors);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await service.GetByIdAsync(id);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (users, total) = await service.GetAllAsync(page, pageSize);
        Response.Headers["X-Total-Count"] = total.ToString();
        return Ok(users);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var updated = await service.UpdateAsync(id, request);
            return updated == null ? NotFound() : Ok(updated);
        }
        catch (ValidationException vx)
        {
            return BadRequest(vx.Errors);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequest credentials)
    {
        var user = await service.AuthenticateAsync(credentials.Email, credentials.Password);
        return user == null ? Unauthorized() : Ok(user);
    }
}