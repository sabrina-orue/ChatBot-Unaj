using Microsoft.Bot.Builder.Adapters.Facebook;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatBot.Unaj
{
    public class FacebookAdapterWithErrorHandler : FacebookAdapter
    {
        public FacebookAdapterWithErrorHandler(IConfiguration configuration, ILogger<BotFrameworkHttpAdapter> logger)
                : base(configuration, logger)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                // Log any leaked exception from the application.
                logger.LogError(exception, $" FACEBOOK {exception.Message}");

                // Send a message to the user
                await turnContext.SendActivityAsync("FACEBOOOK 2");
                await turnContext.SendActivityAsync("FACEBOOOOK 4");

                // Send a trace activity, which will be displayed in the Bot Framework Emulator
                await turnContext.TraceActivityAsync("FACEBOOOOK 5");
            };
        }
    }
}
