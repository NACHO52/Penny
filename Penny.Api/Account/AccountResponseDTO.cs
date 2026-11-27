using Penny.Domain.Account;

namespace Penny.Api.Account;

using System.Text.Json.Serialization;

public sealed record AccountResponseDTO(
    int Id,
    string Name,
    CurrencyCode CurrencyCode,
    decimal InitialBalance,
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString)] decimal CurrentBalance);
