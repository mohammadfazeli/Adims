using System;

namespace Adims.ViewModel.City
{

    public class CityListVm
    {
        public Guid Id { get; set; }
        public int Code { get; set; }
        public string Name { get; set; }
        public bool InActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifeDate { get; set; }
    }


}
