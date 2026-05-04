using API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.DTOs.Requests;
using SharedLibrary.DTOs.Responses;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionsController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpGet("status/{memberId}")]
        public async Task<ActionResult<SubscriptionStatusResponse>> GetStatus(int memberId)
        {
            try
            {
                var status = await _subscriptionService.GetStatusAsync(memberId);
                return Ok(status);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("upgrade/{memberId}")]
        public async Task<ActionResult<SubscriptionStatusResponse>> Upgrade(int memberId, [FromBody] SubscriptionUpgradeRequest request)
        {
            try
            {
                var result = await _subscriptionService.UpgradeAsync(memberId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("renew/{memberId}")]
        public async Task<ActionResult<SubscriptionStatusResponse>> Renew(int memberId, [FromBody] SubscriptionRenewRequest request)
        {
            try
            {
                var result = await _subscriptionService.RenewAsync(memberId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("calculate-price")]
        public async Task<ActionResult<PriceCalculationResponse>> CalculatePrice(int subscriptionId, int durationInMonths)
        {
            try
            {
                var price = await _subscriptionService.CalculatePriceAsync(subscriptionId, durationInMonths);
                return Ok(price);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("process-notifications")]
        public async Task<IActionResult> ProcessNotifications()
        {
            await _subscriptionService.ProcessNotificationsAsync();
            return Ok("Notifications processed");
        }
    }
}
