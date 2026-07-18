using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Features.Mediator.Commands.ReviewCommands;
using SmartCar.Application.Interfaces;
using SmartCar.Domain.Entities;

namespace SmartCar.Application.Features.Mediator.Handlers.ReviewHandlers
{
    public class UpdateReviewHandler : IRequestHandler<UpdateReviewCommand>
    {
        private readonly IRepository<Review> _repository;
        public UpdateReviewHandler(IRepository<Review> repository)
        {
            _repository = repository;
        }
        public async Task Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
        {
            var values = await _repository.GetByIdAsync(request.ReviewId);
            if (values is null) throw new KeyNotFoundException("Không tìm thấy đánh giá.");
            values.Comment = request.Comment;
            values.RaytingValue = request.RaytingValue;
            values.ReviewDate = DateTime.UtcNow;
            await _repository.UpdateAsync(values);
        }
    }
}
