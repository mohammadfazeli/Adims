using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adims.Domain.Entites
{
    [Table(nameof(City))]
    public class City : BaseEntity
    {
        public City()
        {

        }

        public string Name { get; set; }
        public virtual ICollection<Dealer> Dealers { get; set; }

    }

}
