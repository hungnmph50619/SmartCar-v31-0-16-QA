using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Features.Mediator.Commands.ReviewCommands;

namespace SmartCar.Application.Validators.ReviewValidators
{
    public class CreateReviewValidator:AbstractValidator<CreateReviewCommand>
    {
        public CreateReviewValidator()
        {
            RuleFor(x => x.CustomerName).NotEmpty().WithMessage("Vui lòng không để trống tên khách hàng!");
            RuleFor(x => x.CustomerName).MinimumLength(5).WithMessage("Vui lòng nhập ít nhất 5 ký tự!");
            RuleFor(x => x.RaytingValue).NotEmpty().WithMessage("Vui lòng không để trống điểm đánh giá");
            RuleFor(x => x.Comment).NotEmpty().WithMessage("Vui lòng không để trống nội dung đánh giá");
            RuleFor(x => x.Comment).MinimumLength(50).WithMessage("Nội dung đánh giá phải có ít nhất 50 ký tự");
            RuleFor(x => x.Comment).MaximumLength(500).WithMessage("Nội dung đánh giá không được vượt quá 500 ký tự");
        }
    }
}
