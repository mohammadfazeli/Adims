using Adims.Domain.Entites;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Data.Entity;
using System.Linq;
using System;

namespace Adims.DataAccess.Repository
{
    public interface IDealerRepository : IBsaeRepository<Dealer>
    {
        bool CheckExistCity(Guid cityId);
    }


    public class DealerRepository : BsaeRepository<Dealer>, IDealerRepository
    {
        public DealerRepository(ApplicationContext appContext) : base(appContext)
        {
        }

        public override IEnumerable<Dealer> GetAll(Expression<System.Func<Dealer, bool>> expression = null)
        {
            var query = this._entity.AsNoTracking().Include(s => s.City);

            if (expression != null)
                query = query.Where(expression);

            return query.AsEnumerable();
        }

        public bool CheckExistCity(Guid cityId)
        {
            return this._entity.AsNoTracking().Any(s => s.CityId == cityId);
        }


    }






}
