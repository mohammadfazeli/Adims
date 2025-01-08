using System;
using System.ComponentModel.DataAnnotations;

namespace Adims.ViewModel.City
{
    public class EditCityVm
    {
        public Guid Id { get; set; }
        [Required]
        public string Name { get; set; }
        public bool InActive { get; set; }

    }


}
