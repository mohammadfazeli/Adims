using Adims.DataAccess.Repository;
using Adims.ViewModel.Dealer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Adims.Service
{

    public interface IDealerService
    {
        DealerListVm Get(Guid id);

        int Add(AddDealerVm add);
        int Update(EditDealerVm add);
        int Remove(Guid id);
        IEnumerable<DealerListVm> GetDealers();

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

        public DealerListVm Get(Guid id)
        {
            var dealer = _daelerRepository.Get(id);

            return new DealerListVm()
            {
                CityId = dealer.CityId,
                CityName = dealer.City != null ? dealer.City.Name : "",
                DealerNo = dealer.DealerNo,
                Id = dealer.Id,
                InActive = dealer.InActive,
                OwnerName = dealer.OwnerName
            };

        }

        public IEnumerable<DealerListVm> GetDealers()
        {
            return _daelerRepository.GetAll(null).Select(s => new DealerListVm()
            {
                CityId = s.CityId,
                CityName = s.City != null ? s.City.Name : "",
                DealerNo = s.DealerNo,
                Id = s.Id,
                InActive = s.InActive,
                OwnerName = s.OwnerName
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
            model.Id = editvm.Id;
            model.InActive = editvm.InActive;
            model.OwnerName = editvm.OwnerName;

            return _daelerRepository.Save();


        }
    }
}
