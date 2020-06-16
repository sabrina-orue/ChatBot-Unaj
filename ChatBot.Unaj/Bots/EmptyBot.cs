// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatBot.Unaj.Infrastructure;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace ChatBot.Unaj.Bots
{
    public class EmptyBot : ActivityHandler
    {
        protected readonly ILuisRecognizerService _luisRecognizerService;

        public EmptyBot(ILuisRecognizerService luisRecognizerService)
        {
            _luisRecognizerService = luisRecognizerService;
        }

        //MENSAJE DE BIENVENIDA A LOS NUEVOS USUARIOS
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Hello world!"), cancellationToken);
                }
            }
        }

        // MANEJA LAS RESPUESTAS AL USUARIO
        protected async override Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var recognizeResult = await _luisRecognizerService._recognizer.RecognizeAsync(turnContext, cancellationToken: cancellationToken);
            await ManageIntentions(turnContext, recognizeResult, cancellationToken);
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            base.OnTurnAsync(turnContext, cancellationToken);
        }

        private async Task ManageIntentions(ITurnContext turnContext, RecognizerResult recognizeResult, CancellationToken cancellationToken)
        {
            var topIntent = recognizeResult.GetTopScoringIntent();
            switch (topIntent.intent)
            {
                case "Saludar":
                    await IntentSaludar(turnContext, recognizeResult, cancellationToken);
                    break;
                case "Agradecer":
                    await IntentAgradecer(turnContext, recognizeResult, cancellationToken);
                    break;
                case "None":
                    await IntentNone(turnContext, recognizeResult, cancellationToken);
                    break;
                default:
                    break;
            }
        }

        private async Task IntentSaludar(ITurnContext turnContext, RecognizerResult recognizeResult, CancellationToken cancellationToken)
        {
            var userName = turnContext.Activity.From.Name;
            await turnContext.SendActivityAsync($"Hola ${userName}", cancellationToken: cancellationToken);
        }

        private async Task IntentAgradecer(ITurnContext turnContext, RecognizerResult recognizeResult, CancellationToken cancellationToken)
        {
            var userName = turnContext.Activity.From.Name;
            await turnContext.SendActivityAsync($"Gracias ${userName}", cancellationToken: cancellationToken);
        }

        private async Task IntentNone(ITurnContext turnContext, RecognizerResult recognizeResult, CancellationToken cancellationToken)
        {
            var userName = turnContext.Activity.From.Name;
            await turnContext.SendActivityAsync($"No hay nada ${userName}", cancellationToken: cancellationToken);
        }
    }
}
