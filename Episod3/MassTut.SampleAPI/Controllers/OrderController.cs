using MassTransit;
using MassTransit.Definition;
using MassTut.Components.Consumers;
using MassTut.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MassTut.SampleAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        readonly ILogger<OrderController> _logger;
        readonly IRequestClient<SubmitOrder> _submitOrderRequestClient;
        private readonly ISendEndpointProvider _endpointProvider;

        public OrderController(
            ILogger<OrderController> logger,
            IRequestClient<SubmitOrder> submitOrderRequestClient,
            ISendEndpointProvider endpointProvider)
        {
            _logger = logger;
            _submitOrderRequestClient = submitOrderRequestClient;
            _endpointProvider = endpointProvider;
        }
        [HttpPost]
        public async Task<IActionResult> Post(Guid id, string customerNumber)
        {
            try
            {
                var endpoint =await _endpointProvider.GetSendEndpoint(new Uri($"exchange:submit-order"));
                await endpoint.Send<SubmitOrder>(new
                {
                    OrderId = id,
                    Timestamp = InVar.Timestamp,
                    CustomerNumber = customerNumber
                });
                return Ok();
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex.Message,ex.StackTrace);
                return StatusCode(StatusCodes.Status500InternalServerError,ex.Message);
            }
            
        }

        [HttpPut]
        public async Task<IActionResult> Put(Guid id, string customerNumber)
        {
            try
            {
                var (accepted, rejected) = await _submitOrderRequestClient.GetResponse<OrderSubmissionAccepted, OrderSubmissionRejected>(new
                {
                    OrderId = id,
                    Timestamp = InVar.Timestamp,
                    CustomerNumber = customerNumber
                });
                if (accepted.IsCompletedSuccessfully)
                {
                    var response = await accepted;

                    return Accepted(response);
                }

                if (accepted.IsCompleted)
                {
                    await accepted;

                    return Problem("Order was not accepted");
                }
                else
                {
                    var response = await rejected;

                    return BadRequest(response.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex.StackTrace);
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

        }


    }
}
