using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartCar.Application.Interfaces.AppUserInterfaces;
using SmartCar.Domain.Entities;
using SmartCar.Persistence.Context;

namespace SmartCar.Persistence.Repositories.AppUserRepositories
{
    public class AppUserRepository : IAppUserRepository
    {
        private readonly CarBookContext _context;
        public AppUserRepository(CarBookContext context)
        {
            _context = context;
        }
        public async Task<List<AppUser>> GetByFilterAsync(Expression<Func<AppUser, bool>> filter)
        {
            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return await _context.AppUsers
                .AsNoTracking()
                .Where(filter)
                .ToListAsync();
        }
    }
}
