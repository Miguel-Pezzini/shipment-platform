using FluentValidation;
using ShipmentPlatform.Application.DTOs;

namespace ShipmentPlatform.Application.Validators;

public class PagedQueryValidator : AbstractValidator<PagedQuery>
{
    public PagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PerPage).InclusiveBetween(1, PagedQuery.MaxPerPage);
    }
}
