﻿using FluentValidation;
using HCQS.BackEnd.Common.Dto.Request;

namespace HCQS.BackEnd.Common.Validator
{
    public class CreateQuotationDeallingStaffValidator : AbstractValidator<CreateQuotationDeallingStaffRequest>
    {
        public CreateQuotationDeallingStaffValidator()
        {
            RuleFor(x => x.QuotationId).NotNull().NotEmpty().WithMessage("The QuotationId must be required!");
            RuleFor(x => x.RawMaterialDiscount).NotNull().NotEmpty().WithMessage("The MaterialDiscount must be required!");
        }
    }
}