using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Application.Features.CQRS.Commands.ContactCommands;
using SmartCar.Application.Features.CQRS.Handlers.ContactHandlers;
using SmartCar.Application.Features.CQRS.Queries.ContactQueries;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        private readonly CreateContactCommandHandler _createContactCommandHandler;
        private readonly GetContactByIdQueryHandler _getContactByIdQueryHandler;
        private readonly GetContactQueryHandler _getContactQueryHandler;
        private readonly UpdateContactCommandHandler _updateContactCommandHandler;
        private readonly RemoveContactCommandHandler _removeContactCommandHandler;

        public ContactsController(CreateContactCommandHandler createContactCommandHandler, GetContactByIdQueryHandler getContactByIdQueryHandler, GetContactQueryHandler getContactQueryHandler, UpdateContactCommandHandler updateContactCommandHandler, RemoveContactCommandHandler removeContactCommandHandler)
        {
            _createContactCommandHandler = createContactCommandHandler;
            _getContactByIdQueryHandler = getContactByIdQueryHandler;
            _getContactQueryHandler = getContactQueryHandler;
            _updateContactCommandHandler = updateContactCommandHandler;
            _removeContactCommandHandler = removeContactCommandHandler;
        }
        [AllowAnonymous]

        [HttpGet]
        public async Task<IActionResult> ContactList()
        {
            var values = await _getContactQueryHandler.Handle();
            return Ok(values);
        }
        [AllowAnonymous]

        [HttpGet("{id}")]
        public async Task<IActionResult> GetContact(int id)
        {
            var value = await _getContactByIdQueryHandler.Handle(new GetContactByIdQuery(id));
            return Ok(value);
        }

        [HttpPost]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateContact(CreateContactCommand command)
        {
            await _createContactCommandHandler.Handle(command);
            return Ok("Đã thêm thông tin liên hệ");
        }

        [HttpDelete]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveContact(int id)
        {
            await _removeContactCommandHandler.Handle(new RemoveContactCommand(id));
            return Ok("Đã xóa thông tin liên hệ");
        }

        [HttpPut]
                [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateContact(UpdateContactCommand command)
        {
            await _updateContactCommandHandler.Handle(command);
            return Ok("Đã cập nhật thông tin liên hệ");
        }
    }
}
