using Adims.DataAccess.Repository;
using Adims.ViewModel.City;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Adims.Service
{

    public interface ICityService
    {

        int Add(AddCityVm add);
        int Update(EditCityVm add);
        int Remove(Guid id);
        IEnumerable<CityListVm> GetCitys();

    }

    public class CityService : ICityService
    {
        private readonly ICityRepository _cityRepository;

        public CityService(ICityRepository daelerRepository)
        {
            this._cityRepository = daelerRepository;
        }
        public int Add(AddCityVm add)
        {
            if (add == null)
                throw new NullReferenceException("model is null ");

            _cityRepository.Add(entity: new Domain.Entites.City()
            {
                Name = add.Name,
            });

            return _cityRepository.Save();
        }

        public IEnumerable<CityListVm> GetCitys()
        {
            return _cityRepository.GetAll(null).Select(s => new CityListVm()
            {
                Id = s.Id,
                Name = s.Name
            });
        }

        public int Remove(Guid id)
        {
            _cityRepository.Delete(id);
            return _cityRepository.Save();
        }

        public int Update(EditCityVm editvm)
        {
            var model = _cityRepository.Get(editvm.Id);

            if (model == null)
                throw new NullReferenceException("model is null ");

            model.Id = editvm.Id;
            model.Name = editvm.Name;

           return _cityRepository.Save();

        }
    }
}
