using Penny.Domain.Account;

namespace Penny.Api.Account;

public sealed record AccountResponseDTO(
    int Id,
    string Name,
    CurrencyCode CurrencyCode,
    decimal InitialBalance);