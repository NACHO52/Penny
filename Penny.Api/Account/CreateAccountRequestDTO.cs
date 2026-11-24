using Penny.Domain.Accounts;

namespace Penny.Api.Account
{
    public sealed record CreateAccountRequestDTO
    {
        public required string Name { get; init; }
        public required CurrencyCode CurrencyCode { get; init; }
        public required decimal InitialBalance { get; init; }
    }
}
