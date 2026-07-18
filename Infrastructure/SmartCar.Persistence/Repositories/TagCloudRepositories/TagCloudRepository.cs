using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartCar.Application.Interfaces.TagCloudInterfaces;
using SmartCar.Domain.Entities;
using SmartCar.Persistence.Context;

namespace SmartCar.Persistence.Repositories.TagCloudRepositories
{
    public class TagCloudRepository : ITagCloudRepository
    {
        private readonly CarBookContext _context;
        public TagCloudRepository(CarBookContext context)
        {
            _context = context;
        }
        public List<TagCloud> GetTagCloudsByBlogID(int id)
        {
            var values = _context.TagClouds.Where(x => x.BlogID == id).ToList();
            return values;
        }
    }
}
