using Adims.DataAccess.Repository;
using Adims.Domain.Entites;
using Adims.ViewModel.City;
using Adims.ViewModel.Dealer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;

namespace Adims.Service
{

    public interface ICityService
    {

        City Get(Guid id);
        int Add(AddCityVm add);
        int Update(EditCityVm add);
        int Remove(Guid id);
        IEnumerable<CityListVm> GetCitys(Expression<Func<City, bool>> expression = null);
        IEnumerable<SelectListItem> GetDrowDown(bool isAdd = false);
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

        public City Get(Guid id)
        {
            return _cityRepository.Get(id);
        }

        public IEnumerable<CityListVm> GetCitys(Expression<Func<City, bool>> expression = null)
        {
            return _cityRepository.GetAll(expression).Select(s => new CityListVm()
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                InActive= s.InActive,
                CreatedDate = s.CreatedDate,
                LastModifeDate = s.LastModifeDate
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
            model.InActive = editvm.InActive;

            return _cityRepository.Save();

        }

        public IEnumerable<SelectListItem> GetDrowDown(bool isAdd = false)
        {
            return GetCitys(r => !isAdd || !r.InActive).Select(s => new SelectListItem()
            {
                Text = s.Name,
                Value = s.Id.ToString()
            });
        }

    }
}
