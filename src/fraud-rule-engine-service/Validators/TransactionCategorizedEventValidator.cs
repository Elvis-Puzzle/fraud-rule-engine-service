using fraud_rule_engine_service.Contracts.Events;
using FluentValidation;

namespace fraud_rule_engine_service.Validators;

public sealed class TransactionCategorizedEventValidator : AbstractValidator<TransactionCategorizedEvent>
{
    public TransactionCategorizedEventValidator()
    {
        RuleFor(e => e.TransactionId).NotEmpty();
        RuleFor(e => e.AccountId).NotEmpty();
        RuleFor(e => e.CustomerId).NotEmpty();
        RuleFor(e => e.Amount).GreaterThan(0);
        RuleFor(e => e.Currency).NotEmpty().Length(3);
        RuleFor(e => e.Category).NotEmpty();
        RuleFor(e => e.CountryCode).NotEmpty().Length(2);
        RuleFor(e => e.OccurredAt).NotEqual(default(DateTimeOffset));
    }
}
