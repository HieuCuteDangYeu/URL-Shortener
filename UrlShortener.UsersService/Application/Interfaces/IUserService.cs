using UrlShortener.UserService.Application.Requests;
using UrlShortener.UserService.Application.Responses;

namespace UrlShortener.UserService.Application.Interfaces;

public interface IUserService
{
    Task<UserDto> CreateAsync(CreateUserRequest request);
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<(List<UserDto> Users, int TotalCount)> GetAllAsync(int page, int pageSize);
    Task<UserDto?> UpdateAsync(Guid id, UpdateUserRequest request);
    Task<bool> DeleteAsync(Guid id);

    // Optional auth helper
    Task<UserDto?> AuthenticateAsync(string email, string password);
}