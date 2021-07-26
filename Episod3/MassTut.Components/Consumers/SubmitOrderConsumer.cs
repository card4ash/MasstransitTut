using MassTransit;
using MassTut.Contracts;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MassTut.Components.Consumers
{
    /// <summary>
    /// 
    /// </summary>
    public class SubmitOrderConsumer : IConsumer<SubmitOrder>
    {
        readonly ILogger<SubmitOrderConsumer> _logger;
        public SubmitOrderConsumer()
        {

        }
        public SubmitOrderConsumer(ILogger<SubmitOrderConsumer> logger)
        {
            _logger = logger;
        }
        public async Task Consume(ConsumeContext<SubmitOrder> context)
        {
            _logger?.Log(LogLevel.Warning, "SubmitOrderConsumer: {CustomerNumber}", context.Message.CustomerNumber);
            if (context.Message.CustomerNumber.Contains("TEST"))
            {
                if (context.RequestId != null)
                {
                    await context.RespondAsync<OrderSubmissionRejected>(new
                    {
                        InVar.Timestamp,
                        context.Message.OrderId,
                        context.Message.CustomerNumber,
                        Reason = $"Test Customer cannot submit orders: {context.Message.CustomerNumber}"
                    });
                }

                return;
            }
            if (context.RequestId != null)
            {
                await context.RespondAsync<OrderSubmissionAccepted>(new
                {
                    InVar.Timestamp,
                    context.Message.OrderId,
                    context.Message.CustomerNumber
                });
            }
        }
    }
}
