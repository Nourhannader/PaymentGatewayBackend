namespace PaymentApi.DTOs
{
    public record DirectCardPaymentDto(
    //string CardNumber,
    //long ExpMonth,
    //long ExpYear,
    //string Cvc,
    string PaymentMethodId,
    decimal Amount
);
}
