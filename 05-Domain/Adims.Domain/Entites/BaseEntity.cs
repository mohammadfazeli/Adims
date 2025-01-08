using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adims.Domain.Entites
{
    public class BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Code { set; get; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifeDate { get; set; }

        public BaseEntity()
        {
            Id = Guid.NewGuid();
        }
    }
}
