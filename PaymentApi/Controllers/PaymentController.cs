using Microsoft.AspNetCore.Mvc;
using PaymentApi.DTOs;
using Stripe;
using Stripe.Checkout;

namespace PaymentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(ILogger<PaymentController> logger)
        {
            _logger = logger;
        }

        // ==========================================================
        // 1. Hosted Checkout
        // ==========================================================
        [HttpPost("hosted-checkout/create-session")]
        public async Task<IActionResult> CreateCheckoutSession(CreateHostedCheckoutDto request)
        {
            if (request.Amount <= 0)
                return BadRequest("Amount must be greater than zero.");

            try
            {
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    Mode = "payment",

                    LineItems = new List<SessionLineItemOptions>
                    {
                        new()
                        {
                            Quantity = 1,
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "usd",
                                UnitAmount = (long)(request.Amount * 100),
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = request.ProductName
                                }
                            }
                        }
                    },

                    SuccessUrl = "http://localhost:5500/success.html?session_id={CHECKOUT_SESSION_ID}",
                    CancelUrl = "http://localhost:5500/cancel.html"
                };

                var session = await new SessionService().CreateAsync(options);

                return Ok(new
                {
                    SessionId = session.Id,
                    RedirectUrl = session.Url
                });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, ex.Message);
                return BadRequest(new
                {
                    Error = ex.StripeError?.Message ?? ex.Message
                });
            }
        }

        // ==========================================================
        // 2. Embedded Checkout (Payment Element)
        // ==========================================================
        [HttpPost("embedded-checkout/create-payment-intent")]
        public async Task<IActionResult> CreatePaymentIntent(CreatePaymentIntentDto request)
        {
            if (request.Amount <= 0)
                return BadRequest("Amount must be greater than zero.");

            try
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(request.Amount * 100),
                    Currency = "usd",

                    AutomaticPaymentMethods =
                        new PaymentIntentAutomaticPaymentMethodsOptions
                        {
                            Enabled = true
                        }
                };

                var paymentIntent =
                    await new PaymentIntentService().CreateAsync(options);

                return Ok(new
                {
                    PaymentIntentId = paymentIntent.Id,
                    ClientSecret = paymentIntent.ClientSecret
                });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, ex.Message);
                return BadRequest(new
                {
                    Error = ex.StripeError?.Message ?? ex.Message
                });
            }
        }

        // ==========================================================
        // 3. Direct Card Payment (Testing Only)
        // ==========================================================
        [HttpPost("direct-checkout/charge-card")]
        public async Task<IActionResult> ChargeCard([FromBody] DirectCardPaymentDto request)
        {

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100),
                Currency = "usd",
                Confirm = true,
                PaymentMethod = request.PaymentMethodId,

                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never"
                }
            };


            var service = new PaymentIntentService();


            var intent =
                await service.CreateAsync(options);


            return Ok(new
            {
                id = intent.Id,
                status = intent.Status
            });

        }
    }
}