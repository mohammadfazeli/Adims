using System;

namespace Adims.ViewModel.Dealer
{
    public class DealerListVm
    {
        public Guid Id { get; set; }
        public string OwnerName { get; set; }
        public string DealerNo { get; set; }
        public bool InActive { get; set; }
        public Guid CityId { get; set; }
        public string CityName { get; set; }

    }


}
