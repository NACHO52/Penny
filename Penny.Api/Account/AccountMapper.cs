using AccountEntity = Penny.Domain.Account.Account;

namespace Penny.Api.Account;

internal static class AccountMapper
{
    public static AccountEntity ToEntity(CreateAccountRequestDTO request, string userId)
    {
        return new AccountEntity
        {
            Name = request.Name.Trim(),
            CurrencyCode = request.CurrencyCode,
            InitialBalance = request.InitialBalance,
            UserId = userId
        };
    }


    public static AccountResponseDTO ToResponse(AccountEntity account)
    {
        return new AccountResponseDTO(
            account.Id,
            account.Name,
            account.CurrencyCode,
            account.InitialBalance);
    }
}