using Adims.DataAccess.Repository;
using Adims.Domain.Entites;
using Adims.ViewModel.Dealer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Adims.Service
{

    public interface IDealerService
    {
        Dealer Get(Guid id);

        int Add(AddDealerVm add);
        int Update(EditDealerVm add);
        int Remove(Guid id);
        IEnumerable<DealerListVm> GetDealers(Expression<Func<Dealer, bool>> expression = null);
        bool CheckExsistCity(Guid cityId);
    }

    public class DealerService : IDealerService
    {
        private readonly IDealerRepository _daelerRepository;

        public DealerService(IDealerRepository daelerRepository)
        {
            this._daelerRepository = daelerRepository;
        }
        public int Add(AddDealerVm add)
        {
            if (add == null)
                throw new NullReferenceException("model is null ");

            _daelerRepository.Add(entity: new Domain.Entites.Dealer()
            {
                CityId = add.CityId,
                DealerNo = add.DealerNo,
                OwnerName = add.OwnerName,
                InActive = add.InActive,

            });
            return _daelerRepository.Save();
        }

        public Dealer Get(Guid id)
        {
            return _daelerRepository.Get(id);
        }

        public bool CheckExsistCity(Guid cityId)
        {
            return _daelerRepository.CheckExistCity(cityId);
        }

        public IEnumerable<DealerListVm> GetDealers(Expression<Func<Dealer, bool>> expression = null)
        {
            return _daelerRepository.GetAll(expression).Select(s => new DealerListVm()
            {
                CityName = s.City != null ? s.City.Name : "",
                DealerNo = s.DealerNo,
                InActive = s.InActive,
                OwnerName = s.OwnerName,
                Id = s.Id,
                Code = s.Code,
                CreatedDate = s.CreatedDate,
                LastModifeDate = s.LastModifeDate
            });
        }

        public int Remove(Guid id)
        {
            _daelerRepository.Delete(id);
            return _daelerRepository.Save();

        }

        public int Update(EditDealerVm editvm)
        {
            var model = _daelerRepository.Get(editvm.Id);

            if (model == null)
                throw new NullReferenceException("model is null ");

            model.CityId = editvm.CityId;
            model.DealerNo = editvm.DealerNo;
            model.InActive = editvm.InActive;
            model.OwnerName = editvm.OwnerName;

            return _daelerRepository.Save();


        }
    }
}
