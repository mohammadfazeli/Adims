using System.ComponentModel.DataAnnotations;

namespace Adims.ViewModel.City
{
    public class AddCityVm
    {
        [Required]
        public string Name { get; set; }
        public bool InActive { get; set; }

    }


}
