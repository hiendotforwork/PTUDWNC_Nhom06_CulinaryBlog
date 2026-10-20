namespace CulinaryBlog.Application.Commands.Auth.Register;

using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Constants;
using CulinaryBlog.Domain.Entities;
using MediatR;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegistrationPendingResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundTaskQueue? _backgroundTasks;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IBackgroundTaskQueue? backgroundTasks = null)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _backgroundTasks = backgroundTasks;
    }

    public async Task<RegistrationPendingResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // 1. Check email uniqueness (early check)
        var existingEmailUser = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);
        if (existingEmailUser != null)
        {
            throw AuthConflictException.EmailExists();
        }

        // 2. Check username uniqueness (early check)
        var existingNameUser = await _userRepository.FindByUserNameAsync(request.UserName, cancellationToken);
        if (existingNameUser != null)
        {
            throw AuthConflictException.UserNameExists();
        }

        // 3. Begin atomic transaction for user creation and role assignment
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        // 4. Create user entity
        var user = ApplicationUser.Create(request.DisplayName, request.Email, request.UserName);

        // 5. Save user with hashed password
        var (createSucceeded, createErrors) = await _userRepository.CreateAsync(user, request.Password);
        if (!createSucceeded)
        {
            var errorsList = createErrors.ToList();

            // Detect concurrent race-condition duplicates from UserManager / DB constraints
            if (errorsList.Any(e => e.Contains("DuplicateEmail", StringComparison.OrdinalIgnoreCase) ||
                                   (e.Contains("email", StringComparison.OrdinalIgnoreCase) && 
                                    (e.Contains("already", StringComparison.OrdinalIgnoreCase) || e.Contains("taken", StringComparison.OrdinalIgnoreCase)))))
            {
                throw AuthConflictException.EmailExists();
            }

            if (errorsList.Any(e => e.Contains("DuplicateUserName", StringComparison.OrdinalIgnoreCase) ||
                                   (e.Contains("user name", StringComparison.OrdinalIgnoreCase) && 
                                    (e.Contains("already", StringComparison.OrdinalIgnoreCase) || e.Contains("taken", StringComparison.OrdinalIgnoreCase)))))
            {
                throw AuthConflictException.UserNameExists();
            }

            throw new IdentityOperationException("User creation failed", errorsList);
        }

        // 6. Assign default Author role
        var (roleSucceeded, roleErrors) = await _userRepository.AddToRoleAsync(user, AppRoles.Author);
        if (!roleSucceeded)
        {
            throw new IdentityOperationException("Assigning role failed", roleErrors);
        }

        // Persist task intents in the same transaction as the registration.
        if (_backgroundTasks is not null)
        {
            await _backgroundTasks.WelcomeAsync(user.Id, cancellationToken);
            await _backgroundTasks.EmailConfirmationAsync(user.Id, cancellationToken);
        }

        // 7. Commit transaction
        await transaction.CommitAsync(cancellationToken);

        return new RegistrationPendingResponse(user.Email ?? request.Email, EmailConfirmationRequired: true);
    }
}
