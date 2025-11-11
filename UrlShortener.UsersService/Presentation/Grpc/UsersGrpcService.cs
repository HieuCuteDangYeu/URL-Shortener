using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using UrlShortener.Infrastructure.Messaging;
using UrlShortener.Infrastructure.Messaging.Events;
using UrlShortener.Shared.Protos;
using UrlShortener.UserService.Application.Interfaces;
using static UrlShortener.Shared.Protos.UserService;

namespace UrlShortener.UserService.Presentation.Grpc;

public class UsersGrpcService(IUserService userService, IMessageBroker messageBroker) : UserServiceBase
{
    public override async Task<User> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        try
        {
            var appRequest = new CreateUserRequest
            {
                FirstName = request.FirstName ?? string.Empty,
                LastName = request.LastName ?? string.Empty,
                Email = request.Email ?? string.Empty,
                PhoneNumber = string.IsNullOrEmpty(request.PhoneNumber) ? string.Empty : request.PhoneNumber,
                Role = string.IsNullOrEmpty(request.Role) ? "User" : request.Role,
                Password = request.Password ?? string.Empty,
            };

            var user = await userService.CreateAsync(appRequest);

            messageBroker.Publish("user-created", new UserCreatedEvent
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            });

            return new User
            {
                Id = user.Id.ToString(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
            };
        }
        catch (ValidationException ex)
        {
            var errors = string.Join("; ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<GetAllUsersResponse> GetAllUsers(GetAllUsersRequest request, ServerCallContext context)
    {
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 10;
        var id = Guid.TryParse(request.Id, out var guid) ? guid : (Guid?)null;
        var (user, totalCount) = await userService.GetAllAsync(id, page, pageSize);

        var resp = new GetAllUsersResponse { TotalCount = totalCount };

        foreach (var u in user)
        {
            resp.Users.Add(new User
            {
                Id = u.Id.ToString(),
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role
            });
        }

        return resp;
    }

    public override async Task<User> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid id"));

        var appRequest = new UpdateUserRequest
        {
            FirstName = string.IsNullOrEmpty(request.FirstName) ? null : request.FirstName,
            LastName = string.IsNullOrEmpty(request.LastName) ? null : request.LastName,
            PhoneNumber = string.IsNullOrEmpty(request.PhoneNumber) ? null : request.PhoneNumber,
            Role = string.IsNullOrEmpty(request.Role) ? null : request.Role,
            Email = !string.IsNullOrEmpty(request.Email) ? request.Email : null,
            Password = string.IsNullOrEmpty(request.Password) ? null : request.Password
        };

        try
        {
            var updated = await userService.UpdateAsync(id, appRequest);
            return updated == null
                ? throw new RpcException(new Status(StatusCode.NotFound, "User not found"))
                : new User
            {
                Id = updated.Id.ToString(),
                FirstName = updated.FirstName,
                LastName = updated.LastName,
                Email = updated.Email,
                PhoneNumber = updated.PhoneNumber,
                Role = updated.Role
            };
        }
        catch (ValidationException ex)
        {
            var errors = string.Join("; ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task<Empty> DeleteUser(DeleteUserRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid id"));

        var deleted = await userService.DeleteAsync(id);
        if (!deleted)
            throw new RpcException(new Status(StatusCode.NotFound, "User not found"));

        return new Empty();
    }
}