using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adims.Domain.Entites
{
    [Table(nameof(Dealer))]
    public class Dealer : BaseEntity
    {
        public Dealer()
        {

        }

        public string OwnerName { get; set; }
        public string DealerNo { get; set; }
        public bool InActive { get; set; }
        public Guid CityId { get; set; }
        [ForeignKey(nameof(CityId))]
        public City City { get; set; }
    }

}
