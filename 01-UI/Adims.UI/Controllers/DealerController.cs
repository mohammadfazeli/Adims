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

        public ActionResult Details(Guid id)
        {
            var dealer = _dealerService.GetDealers(s => s.Id == id).FirstOrDefault();
            return View(dealer);
        }

        // فرم ایجاد
        public ActionResult Create()
        {

            return View(new AddDealerVm()
            {
                CityItems = _cityService.GetDrowDown(true)
            }); ;
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
            dealer.CityItems = _cityService.GetDrowDown(true);
            return View(dealer);
        }

        // فرم ویرایش
        public async Task<ActionResult> Edit(Guid id)
        {
            var dealer = _dealerService.Get(id);
            if (dealer == null)
            {
                return HttpNotFound();
            }
            return View(new EditDealerVm()
            {
                CityItems = _cityService.GetDrowDown(),
                CityId = dealer.CityId,
                DealerNo = dealer.DealerNo,
                InActive = dealer.InActive,
                OwnerName = dealer.OwnerName,
                Id = dealer.Id
            });
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
            dealer.CityItems = _cityService.GetDrowDown();
            return View(dealer);
        }

        // حذف
        public async Task<ActionResult> Delete(Guid id)
        {
            var dealer = _dealerService.Get(id);
            if (dealer == null)
            {
                return HttpNotFound();
            }

            return View(new EditDealerVm() { Id = dealer.Id, DealerNo = dealer.DealerNo });
        }

        // تأیید حذف
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            _dealerService.Remove(id);
            return RedirectToAction(nameof(Index));
        }
    }

}