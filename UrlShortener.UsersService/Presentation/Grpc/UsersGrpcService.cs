using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using UrlShortener.UserService.Application.Interfaces;
using UrlShortener.UserService.Application.Responses;
using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using UrlShortener.UserService.Application.Requests;
// Alias must match the proto option csharp_namespace above
using Proto = UrlShortener.UsersService.Protos;

namespace UrlShortener.UserService.Presentation.Grpc;

public class UsersGrpcService : Proto.UserService.UserServiceBase
{
    private readonly IUserService _userService;
    public UsersGrpcService(IUserService userService) => _userService = userService;

    public override async Task<Proto.User> CreateUser(Proto.CreateUserRequest request, ServerCallContext context)
    {
        try
        {
            var appRequest = new CreateUserRequest
            {
                FirstName = request.FirstName ?? string.Empty,
                LastName = request.LastName ?? string.Empty,
                Email = request.Email ?? string.Empty,
                PhoneNumber = string.IsNullOrEmpty(request.PhoneNumber) ? null : request.PhoneNumber,
                Role = string.IsNullOrEmpty(request.Role) ? "User" : request.Role,
                Password = request.Password ?? string.Empty,
                ConfirmPassword = request.ConfirmPassword ?? string.Empty
            };

            var created = await _userService.CreateAsync(appRequest);
            return ToProto(created);
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

    public override async Task<Proto.User> GetUser(Proto.GetUserRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid id"));

        var user = await _userService.GetByIdAsync(id);
        if (user == null)
            throw new RpcException(new Status(StatusCode.NotFound, "User not found"));

        return ToProto(user);
    }

    public override async Task<Proto.GetAllUsersResponse> GetAllUsers(Proto.GetAllUsersRequest request, ServerCallContext context)
    {
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        var result = await _userService.GetAllAsync(page, pageSize);
        var users = result.Users;
        var total = result.TotalCount;

        var resp = new Proto.GetAllUsersResponse { TotalCount = total };
        resp.Users.AddRange(users.Select(ToProto));
        return resp;
    }

    public override async Task<Proto.User> UpdateUser(Proto.UpdateUserRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid id"));

        var appRequest = new UpdateUserRequest
        {
            FirstName = string.IsNullOrEmpty(request.FirstName) ? null : request.FirstName,
            LastName = string.IsNullOrEmpty(request.LastName) ? null : request.LastName,
            PhoneNumber = string.IsNullOrEmpty(request.PhoneNumber) ? null : request.PhoneNumber,
            Role = string.IsNullOrEmpty(request.Role) ? null : request.Role,
            Password = string.IsNullOrEmpty(request.Password) ? null : request.Password
        };

        try
        {
            var updated = await _userService.UpdateAsync(id, appRequest);
            if (updated == null)
                throw new RpcException(new Status(StatusCode.NotFound, "User not found"));

            return ToProto(updated);
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

    public override async Task<Empty> DeleteUser(Proto.DeleteUserRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid id"));

        var deleted = await _userService.DeleteAsync(id);
        if (!deleted)
            throw new RpcException(new Status(StatusCode.NotFound, "User not found"));

        return new Empty();
    }

    public override async Task<Proto.AuthenticateResponse> Authenticate(Proto.AuthenticateRequest request, ServerCallContext context)
    {
        var user = await _userService.AuthenticateAsync(request.Email ?? string.Empty, request.Password ?? string.Empty);
        if (user == null)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid credentials"));

        return new Proto.AuthenticateResponse { User = ToProto(user) };
    }

    private static Proto.User ToProto(UserDto dto) =>
        new Proto.User
        {
            Id = dto.Id.ToString(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber ?? string.Empty,
            Role = dto.Role,
            CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(dto.CreatedAt, DateTimeKind.Utc))
        };
}