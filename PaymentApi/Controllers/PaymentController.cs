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
            if (request == null)
                return BadRequest(new
                {
                    Status = 400,
                    Error = "Invalid request."
                });

            if (request.Amount <= 0)
                return BadRequest(new
                {
                    Status = 400,
                    Error = "Amount must be greater than zero."
                });

            if (string.IsNullOrWhiteSpace(request.ProductName))
                return BadRequest(new
                {
                    Status = 400,
                    Error = "Product name is required."
                });
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
                    Status = 200,
                    Message = "Checkout session created successfully.",
                    SessionId = session.Id,
                    RedirectUrl = session.Url
                });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, ex.Message);
                return BadRequest(new
                {
                    Status=400,
                    Error = ex.StripeError?.Message ?? ex.Message
               
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected Error");

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Status = 500,
                    Error = "An unexpected error occurred."
                });
            }
        }

        // ==========================================================
        // 2. Embedded Checkout (Payment Element)
        // ==========================================================
        [HttpPost("embedded-checkout/create-payment-intent")]
        public async Task<IActionResult> CreatePaymentIntent(CreatePaymentIntentDto request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    Status = 400,
                    Error = "Invalid request."
                });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new
                {
                    Status = 400,
                    Error = "Amount must be greater than zero."
                });
            }
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
                    Status = 200,
                    Message = "Payment Intent created successfully.",
                    PaymentIntentId = paymentIntent.Id,
                    ClientSecret = paymentIntent.ClientSecret
                });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, ex.Message);
                return BadRequest(new
                {
                    Status = 400,
                    Error = ex.StripeError?.Message ?? ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected Error");

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Status = 500,
                    Error = "An unexpected error occurred."
                });
            }
        }

        // ==========================================================
        // 3. Direct Card Payment (Testing Only)
        // ==========================================================
        [HttpPost("direct-checkout/charge-card")]
        public async Task<IActionResult> ChargeCard([FromBody] DirectCardPaymentDto request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    Status = 400,
                    Error = "Invalid request."
                });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new
                {
                    Status = 400,
                    Error = "Amount must be greater than zero."
                });
            }

            if (string.IsNullOrWhiteSpace(request.PaymentMethodId))
            {
                return BadRequest(new
                {
                    Status = 400,
                    Error = "PaymentMethodId is required."
                });
            }


            try
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = Convert.ToInt64(request.Amount * 100),
                    Currency = "usd",

                    PaymentMethod = request.PaymentMethodId,

                    Confirm = true,

                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                        AllowRedirects = "never"
                    }
                };


                var service = new PaymentIntentService();

                var intent = await service.CreateAsync(options);


                return Ok(new
                {
                    Status = 200,
                    Message = "Payment completed successfully.",
                    PaymentIntentId = intent.Id,
                    PaymentStatus = intent.Status
                });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe Error");

                return BadRequest(new
                {
                    Status = 400,
                    Error = ex.StripeError?.Message ?? ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected Error");

                return StatusCode(500, new
                {
                    Status = 500,
                    Error = "An unexpected error occurred."
                });
            }
        }
    }
}