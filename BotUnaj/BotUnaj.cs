// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BotUnaj.Infrastructure;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace BotUnaj
{
    public class BotUnaj : ActivityHandler 
    {
        protected readonly LuisRecognizeInterface _luisRecognize;

        public BotUnaj(LuisRecognizeInterface luisRecognize)
        {
            _luisRecognize = luisRecognize;
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Hola soy Arturito, tu ayudante en la Unaj"), cancellationToken);
                }
            }
        }

        public override Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            return base.OnTurnAsync(turnContext, cancellationToken);
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var recognizeResult = await _luisRecognize._recognizer.RecognizeAsync(turnContext, cancellationToken);
            await ManageIntentions(turnContext, recognizeResult, cancellationToken);
           
        }

        private async Task ManageIntentions(ITurnContext<IMessageActivity> turnContext, RecognizerResult recognizerResult, CancellationToken cancellationToken)
        {
            var topIntent = recognizerResult.GetTopScoringIntent();
            switch (topIntent.intent)
            {
                case "Agradecer":
                    await IntentAgradecer(turnContext, recognizerResult, cancellationToken);
                    break;
                case "Saludar":
                    await IntentSaludar(turnContext, recognizerResult, cancellationToken);
                    break;
                case "None":
                    await IntentNone(turnContext, recognizerResult, cancellationToken);
                    break;
                default:
                    break;

            }
        }

        private async Task IntentNone(ITurnContext<IMessageActivity> turnContext, RecognizerResult recognizerResult, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync($"No entiendo que queres, explicate mejor chango", cancellationToken: cancellationToken);
        }

        private async Task IntentAgradecer(ITurnContext<IMessageActivity> turnContext, RecognizerResult recognizerResult, CancellationToken cancellationToken)
        {
            
            await turnContext.SendActivityAsync($"No te preocupes, me gusta ayudar gil", cancellationToken: cancellationToken);
        }

        private async Task IntentSaludar(ITurnContext<IMessageActivity> turnContext, RecognizerResult recognizerResult, CancellationToken cancellationToken)
        {
            var UserName = turnContext.Activity.From.Name;
            await turnContext.SendActivityAsync($"Hola {UserName}, que carajo queres?",cancellationToken: cancellationToken);
        }
    }
}
