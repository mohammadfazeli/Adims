using Adims.Domain.Entites;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace Adims.DataAccess.Repository
{
    public interface IBsaeRepository<T>
    {

        IEnumerable<T> GetAll(Expression<Func<T, bool>> expression = null);

        void Add(T entity);

        void Delete(Guid id);

        T Get(Guid id);


        int Save();
    }


    public class BsaeRepository<T> : IBsaeRepository<T> where T : BaseEntity
    {
        private readonly ApplicationContext _appContext;
        protected DbSet<T> _entity;
        public BsaeRepository(ApplicationContext appContext)
        {
            this._appContext = appContext;
            _entity = appContext.Set<T>();
        }
        public virtual void Add(T entity)
        {
            _entity.Add(entity);
        }

        public virtual void Delete(Guid id)
        {
            var model = _entity.Find(id);
            if (model == null)
            {
                throw new ArgumentNullException($"not found by id : {id}");
            }
            _entity.Remove(model);
        }

        public virtual T Get(Guid id)
        {
            return _entity.Find(id);

        }

        public virtual IEnumerable<T> GetAll(Expression<Func<T, bool>> expression = null)
        {
            var query = _entity.AsNoTracking().AsQueryable();

            if (expression != null)
                query = query.Where(expression);

            return query.AsEnumerable();
        }

        public virtual int Save()
        {
            return _appContext.SaveChanges();
        }
    }
}
