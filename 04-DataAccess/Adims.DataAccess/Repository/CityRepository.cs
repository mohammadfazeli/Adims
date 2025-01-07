using Adims.Domain.Entites;

namespace Adims.DataAccess.Repository
{

    public interface ICityRepository : IBsaeRepository<City>
    {

    }

    public class CityRepository : BsaeRepository<City>, ICityRepository
    {
        public CityRepository(ApplicationContext appContext) : base(appContext)
        {
        }
    }

}
