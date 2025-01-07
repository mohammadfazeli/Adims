using Adims.Domain.Entites;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Data.Entity;
using System.Linq;

namespace Adims.DataAccess.Repository
{
    public interface IDaelerRepository : IBsaeRepository<Dealer>
    {

    }


    public class DaelerRepository : BsaeRepository<Dealer>, IDaelerRepository
    {
        public DaelerRepository(ApplicationContext appContext) : base(appContext)
        {
        }

        public override IEnumerable<Dealer> GetAll(Expression<System.Func<Dealer, bool>> expression = null)
        {
            var query = this._entity.AsNoTracking().Include(s => s.City);

            if (expression != null)
                query = query.Where(expression);

            return query.AsEnumerable();
        }
    }






}
