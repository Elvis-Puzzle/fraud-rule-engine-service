using fraud_rule_engine_service.Domain.Models;
using Microsoft.Extensions.Options;

namespace fraud_rule_engine_service.Domain.Rules;

/// <summary>Flags a large transfer to a payee the customer has never paid before.</summary>
public sealed class NewPayeeLargeTransferRule(IOptions<FraudRuleOptions> options) : IFraudRule
{
    private readonly FraudRuleOptions _options = options.Value;

    public string Code => "NEW_PAYEE_LARGE_TRANSFER";
    public string Name => "New Payee Large Transfer";

    public RuleEvaluationResult Evaluate(TransactionEvaluationContext context)
    {
        var current = context.Current;
        if (!current.IsNewPayee || current.Amount < _options.NewPayeeAmountThreshold)
        {
            return RuleEvaluationResult.NotTriggered(Code, Name);
        }

        return RuleEvaluationResult.Trigger(Code, Name, 70,
            $"First-time transfer of {current.Amount:N2} {current.Currency} to payee {current.CounterpartyAccountId ?? "unknown"} exceeds {_options.NewPayeeAmountThreshold:N2}.");
    }
}
