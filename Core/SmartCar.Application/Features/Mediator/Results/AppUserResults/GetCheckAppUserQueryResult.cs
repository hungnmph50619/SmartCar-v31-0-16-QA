using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCar.Application.Features.Mediator.Results.AppUserResults
{
    public class GetCheckAppUserQueryResult
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsVehiclePartner { get; set; }
        public int TokenVersion { get; set; }
        public bool IsExist { get; set; }
        public string? FailureReason { get; set; }
        public DateTime? LockoutEnd { get; set; }
    }
}
