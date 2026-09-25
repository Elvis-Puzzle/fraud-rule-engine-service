namespace fraud_rule_engine_service.Common.Exceptions;

public sealed class NotFoundException(string message) : Exception(message);
