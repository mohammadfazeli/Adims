using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Adims.ViewModel.Dealer
{
    public class AddDealerVm
    {
        [Required]
        public string OwnerName { get; set; }
        [Required]
        [MaxLength(50)]
        public string DealerNo { get; set; }
        public bool InActive { get; set; }
        [Required]
        public Guid CityId { get; set; }
        public IEnumerable<SelectListItem> CityItems { get; set; }
    }


}
