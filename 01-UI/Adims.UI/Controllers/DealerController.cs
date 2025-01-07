using Adims.Service;
using Adims.ViewModel.Dealer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Adims.UI.Controllers
{
    public class DealerController : Controller
    {
        private readonly IDealerService _dealerService;
        private readonly ICityService _cityService;

        public DealerController(IDealerService dealerService, ICityService cityService)
        {
            this._dealerService = dealerService;
            this._cityService = cityService;
        }

        // نمایش لیست
        public ActionResult Index()
        {
            List<DealerListVm> dealers = _dealerService.GetDealers().ToList();
            return View(dealers);
        }

        // فرم ایجاد
        public ActionResult Create()
        {
            ViewData["CityId"] = new SelectList(_cityService.GetCitys(), "Id", "Name");
            return View();
        }

        // ثبت اطلاعات
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(AddDealerVm dealer)
        {
            if (ModelState.IsValid)
            {
                _dealerService.Add(dealer);
                return RedirectToAction(nameof(Index));
            }
            ViewData["CityId"] = new SelectList(_cityService.GetCitys(), "Id", "Name", dealer.CityId);
            return View(dealer);
        }

        // فرم ویرایش
        public async Task<ActionResult> Edit(Guid id)
        {
            DealerListVm dealer = _dealerService.Get(id);
            if (dealer == null)
            {
                return HttpNotFound();
            }
            ViewData["CityId"] = new SelectList(_cityService.GetCitys(), "Id", "Name", dealer.CityId);
            return View(dealer);
        }

        // ذخیره تغییرات ویرایش
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(Guid id, EditDealerVm dealer)
        {
            if (id != dealer.Id)
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {
                _dealerService.Update(dealer);

                return RedirectToAction(nameof(Index));
            }
            ViewData["CityId"] = new SelectList(_cityService.GetCitys(), "Id", "Name", dealer.CityId);
            return View(dealer);
        }

        // حذف
        public async Task<ActionResult> Delete(Guid id)
        {
            DealerListVm dealer = _dealerService.Get(id);
            if (dealer == null)
            {
                return HttpNotFound();
            }

            return View(dealer);
        }

        // تأیید حذف
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            DealerListVm dealer = _dealerService.Get(id);
            _dealerService.Remove(id);
            return RedirectToAction(nameof(Index));
        }
    }

}