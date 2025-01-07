using Adims.Domain.Entites;
using System.Data.Entity;

namespace Adims.DataAccess
{
    public class ApplicationContext : DbContext
    {

        public ApplicationContext() : base("AdimsDataBase")
        {

        }

        public DbSet<City> City { get; set; }
        public DbSet<Dealer> Dealer { get; set; }
    }
}
