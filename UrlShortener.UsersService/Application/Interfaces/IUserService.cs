using UrlShortener.Shared.Protos;

namespace UrlShortener.UserService.Application.Interfaces;

public interface IUserService
{
    Task<Domain.Entities.User> CreateAsync(CreateUserRequest request);
    Task<Domain.Entities.User?> GetByIdAsync(Guid id);
    Task<(List<Domain.Entities.User> Users, int TotalCount)> GetAllAsync(Guid? id, int page, int pageSize);
    Task<Domain.Entities.User?> UpdateAsync(Guid id, UpdateUserRequest request);
    Task<bool> DeleteAsync(Guid id);
}