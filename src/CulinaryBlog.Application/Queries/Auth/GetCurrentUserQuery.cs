namespace CulinaryBlog.Application.Queries.Auth;

using CulinaryBlog.Application.DTOs.Auth;
using MediatR;

public record GetCurrentUserQuery(string UserId) : IRequest<UserProfileDto?>;
