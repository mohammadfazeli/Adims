using Adims.Domain.Entites;
using System;
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


        public override int SaveChanges()
        {
            SetAudit();
            return base.SaveChanges();
        }

        private void SetAudit()
        {
            var items = ChangeTracker.Entries<BaseEntity>();
            foreach (var item in items)
            {
                switch (item.State)
                {
                    case EntityState.Added:
                        item.Entity.CreatedDate = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        item.Entity.LastModifeDate = DateTime.UtcNow;
                        break;
                }
            }
        }
    }
}
