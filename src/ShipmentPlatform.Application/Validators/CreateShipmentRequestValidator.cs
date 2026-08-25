using FluentValidation;
using ShipmentPlatform.Application.DTOs;

namespace ShipmentPlatform.Application.Validators;

public class CreateShipmentRequestValidator : AbstractValidator<CreateShipmentRequest>
{
    public CreateShipmentRequestValidator()
    {
        RuleFor(x => x.SenderName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RecipientName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OriginCity).NotEmpty().MaximumLength(120);
        RuleFor(x => x.DestinationCity).NotEmpty().MaximumLength(120);
        RuleFor(x => x.WeightKg).GreaterThan(0).LessThanOrEqualTo(10_000);
    }
}
