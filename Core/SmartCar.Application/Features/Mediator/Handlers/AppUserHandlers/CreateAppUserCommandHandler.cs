using MediatR;
using SmartCar.Application.Features.Mediator.Commands.AppUserCommands;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Security;

namespace SmartCar.Application.Features.Mediator.Handlers.AppUserHandlers
{
    public class CreateAppUserCommandHandler : IRequestHandler<CreateAppUserCommand>
    {
        private readonly IRepository<AppUser> _userRepository;
        private readonly IRepository<AppRole> _roleRepository;

        public CreateAppUserCommandHandler(
            IRepository<AppUser> userRepository,
            IRepository<AppRole> roleRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        public async Task Handle(CreateAppUserCommand request, CancellationToken cancellationToken)
        {
            PasswordPolicy.EnsureValid(request.Password);
            var customerRole = await _roleRepository.GetByFilterAsync(x => x.AppRoleName == "Customer");
            if (customerRole is null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy role Customer. Hãy chạy script nâng cấp cơ sở dữ liệu trước khi đăng ký tài khoản.");
            }

            await _userRepository.CreateAsync(new AppUser
            {
                Password = PasswordSecurity.Hash(request.Password ?? string.Empty),
                Username = (request.Username ?? string.Empty).Trim(),
                AppRoleId = customerRole.AppRoleId,
                Email = (request.Email ?? string.Empty).Trim(),
                Phone = (request.Phone ?? string.Empty).Trim(),
                Name = (request.Name ?? string.Empty).Trim(),
                Surname = (request.Surname ?? string.Empty).Trim(),
                IsVehiclePartner = false,
                EmailConfirmed = false
            });
        }
    }
}
