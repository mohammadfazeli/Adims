using System;

namespace Adims.ViewModel.Dealer
{
    public class AddDealerVm
    {        
        public string OwnerName { get; set; }
        public string DealerNo { get; set; }
        public bool InActive { get; set; }
        public Guid CityId { get; set; }
    }


}
