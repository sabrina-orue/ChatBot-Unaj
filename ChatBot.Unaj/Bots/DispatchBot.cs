// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChatBot.Unaj.Entities;
using ChatBot.Unaj.Infrastructure;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatBot.Unaj.Bots
{
    public class DispatchBot : ActivityHandler
    {
        private readonly ILogger<DispatchBot> _logger;
        private readonly IBotServices _botServices;

        public DispatchBot(IBotServices botServices, ILogger<DispatchBot> logger)
        {
            _logger = logger;
            _botServices = botServices;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // First, we use the dispatch model to determine which cognitive service (LUIS or QnA) to use.
            var recognizerResult = await _botServices.Dispatch.RecognizeAsync(turnContext, cancellationToken);

            // Top intent tell us which cognitive service to use.
            var topIntent = recognizerResult.GetTopScoringIntent();
            var topEntity = recognizerResult.Entities.ToObject<MyEntityLuis>();
            string value = topEntity.TipoConsulta?.FirstOrDefault().FirstOrDefault();

             // Next, we call the dispatcher with the top intent.
             await DispatchToTopIntentAsync(turnContext, topIntent.intent, recognizerResult, cancellationToken,  value );
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            const string WelcomeText = "Type a greeting, or a question about the weather to get started.";

            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Welcome to Dispatch bot {member.Name}. {WelcomeText}"), cancellationToken);
                }
            }
        }

        private async Task DispatchToTopIntentAsync(ITurnContext<IMessageActivity> turnContext, string intent, RecognizerResult recognizerResult, CancellationToken cancellationToken, string value)
        {
            switch (intent)
            {
                case "Saludar":
                    await ProcessHomeAutomationAsync(turnContext, recognizerResult.Properties["luisResult"] as LuisResult, cancellationToken);
                    break;
                case "None":
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Dispatch unrecognized intent: {intent}."), cancellationToken);
                    break;
                case "Agradecer":
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Dispatch unrecognized intent: {intent}."), cancellationToken);
                    break;
                default:
                    await ProcessSampleQnAAsync(turnContext, cancellationToken, intent, value);
                    break;
                               }
        }

        private async Task ProcessHomeAutomationAsync(ITurnContext<IMessageActivity> turnContext, LuisResult luisResult, CancellationToken cancellationToken)
        {
            _logger.LogInformation("ProcessHomeAutomationAsync");

            // Retrieve LUIS result for Process Automation.
            var result = luisResult.ConnectedServiceResult;
            var topIntent = result.TopScoringIntent.Intent;

            await turnContext.SendActivityAsync(MessageFactory.Text($"HomeAutomation top intent {topIntent}."), cancellationToken);
            await turnContext.SendActivityAsync(MessageFactory.Text($"HomeAutomation intents detected:\n\n{string.Join("\n\n", result.Intents.Select(i => i.Intent))}"), cancellationToken);
            if (luisResult.Entities.Count > 0)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text($"HomeAutomation entities were found in the message:\n\n{string.Join("\n\n", result.Entities.Select(i => i.Entity))}"), cancellationToken);
            }
        }

        private async Task ProcessWeatherAsync(ITurnContext<IMessageActivity> turnContext, LuisResult luisResult, CancellationToken cancellationToken)
        {
            _logger.LogInformation("ProcessWeatherAsync");

            // Retrieve LUIS results for Weather.
            var result = luisResult.ConnectedServiceResult;
            var topIntent = result.TopScoringIntent.Intent;
            await turnContext.SendActivityAsync(MessageFactory.Text($"ProcessWeather top intent {topIntent}."), cancellationToken);
            await turnContext.SendActivityAsync(MessageFactory.Text($"ProcessWeather Intents detected::\n\n{string.Join("\n\n", result.Intents.Select(i => i.Intent))}"), cancellationToken);
            if (luisResult.Entities.Count > 0)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text($"ProcessWeather entities were found in the message:\n\n{string.Join("\n\n", result.Entities.Select(i => i.Entity))}"), cancellationToken);
            }
        }

        private async Task ProcessSampleQnAAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken, string topIntent, string value)
        {
            QueryResult[] results;
            _logger.LogInformation("ProcessSampleQnAAsync");
            if(value != null)
            {
                var metadata = new Microsoft.Bot.Builder.AI.QnA.Metadata();
                var qnaOptions = new QnAMakerOptions();

                metadata.Name = topIntent;
                metadata.Value = value;
                qnaOptions.Top = 5;
                qnaOptions.StrictFilters = new Microsoft.Bot.Builder.AI.QnA.Metadata[] { metadata };
                qnaOptions.ScoreThreshold = 0.1F;
                results = await _botServices.SampleQnA.GetAnswersAsync(turnContext, qnaOptions);
            }
            else
            {
                results = await _botServices.SampleQnA.GetAnswersAsync(turnContext);
            }

            if (results.Any())
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(results.First().Answer), cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Lo siento, no te entendí. Podrias reformular tu pregunta?."), cancellationToken);
            }
        }
    }
}
