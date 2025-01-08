using Adims.Service;
using Adims.ViewModel.City;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Adims.UI.Controllers
{
    public class CityController : Controller
    {
        private readonly ICityService _CityService;
        private readonly IDealerService _dealerService;

        public CityController(ICityService cityService, IDealerService dealerService)
        {
            this._CityService = cityService;
            _dealerService = dealerService;
        }

        // نمایش لیست
        public ActionResult Index()
        {
            List<CityListVm> Citys = _CityService.GetCitys().ToList();
            return View(Citys);
        }

        public ActionResult Details(Guid id)
        {
            var city = _CityService.GetCitys(s=>s.Id==id).FirstOrDefault();
            return View(city);
        }



        // فرم ایجاد
        public ActionResult Create()
        {
            ViewData["CityId"] = new SelectList(_CityService.GetCitys(), "Id", "Name");
            return View();
        }

        // ثبت اطلاعات
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(AddCityVm City)
        {
            if (ModelState.IsValid)
            {
                _CityService.Add(City);
                return RedirectToAction(nameof(Index));
            }
            return View(City);
        }

        // فرم ویرایش
        public async Task<ActionResult> Edit(Guid id)
        {
            var City = _CityService.Get(id);
            if (City == null)
            {
                return HttpNotFound();
            }
            return View(new EditCityVm() { Name = City.Name, Id = City.Id , InActive = City.InActive });
        }

        // ذخیره تغییرات ویرایش
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, EditCityVm City)
        {
            if (id != City.Id)
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {
                _CityService.Update(City);

                return RedirectToAction(nameof(Index));
            }
            return View(City);
        }

        // حذف
        public async Task<ActionResult> Delete(Guid id)
        {
            var City = _CityService.Get(id);
            if (City == null)
            {
                return HttpNotFound();
            }

            return View(new EditCityVm() { Name = City.Name, Id = City.Id });
        }

        // تأیید حذف
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            var City = _CityService.Get(id);
            if (_dealerService.CheckExsistCity(id))
            {
                return RedirectToAction(nameof(Index));
            }
            _CityService.Remove(id);
            return RedirectToAction(nameof(Index));
        }
    }

}