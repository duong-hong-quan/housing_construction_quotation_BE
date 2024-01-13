﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HCQS.BackEnd.DAL.Models
{
    public class QuotationDetail
    {
        [Key]
        public Guid Id { get; set; }

        public int Quantity { get; set; }
        public double Total { get; set; }
        public Guid? ConstructionMaterialId { get; set; }

        [ForeignKey(nameof(ConstructionMaterialId))]
        public ConstructionMaterial? ConstructionMaterial { get; set; }

        public Guid? QuotationId { get; set; }

        [ForeignKey(nameof(QuotationId))]
        public Quotation? Quotation { get; set; }
    }
}