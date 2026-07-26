namespace PaymentApi.DTOs
{
    public record CreateHostedCheckoutDto(string ProductName, decimal Amount);
}
