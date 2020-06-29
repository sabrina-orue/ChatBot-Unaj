// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChatBot.Unaj.Dialogs;
using ChatBot.Unaj.Entities;
using ChatBot.Unaj.Infrastructure;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChatBot.Unaj.Bots
{
    public class DispatchBot<T> : ActivityHandler where T : Dialog
    {
        private readonly ILogger<DispatchBot<T>> _logger;
        private readonly IBotServices _botServices;
        protected readonly Dialog _dialog;
        protected readonly BotState _conversationState;

        public DispatchBot(T dialog, ConversationState conversationState, IBotServices botServices, ILogger<DispatchBot<T>> logger)
        {
            _logger = logger;
            _botServices = botServices;
            _conversationState = conversationState;
            _dialog = dialog;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            //MENSAJE DE BIENVENIDA PARA TELEGRAM
            if (turnContext.Activity.Text.Equals("/start"))
            {
                await turnContext.SendActivityAsync(activity: WelcomeHeroCard(), cancellationToken);
                await Task.Delay(500);
                await turnContext.SendActivityAsync(MessageFactory.Text("Hola! Soy el asistente virtual de la Universidad. Estoy disponible las 24hs del dia para responder tus consultas. Tengo información en mi base de conocimientos relacionada a Siu Guarani, Campus Virtual, Inscripciones y más!"), cancellationToken);
            }
            else
            {
                //await _dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);

                // First, we use the dispatch model to determine which cognitive service (LUIS or QnA) to use.
                var recognizerResult = await _botServices.Dispatch.RecognizeAsync(turnContext, cancellationToken);

                // Top intent tell us which cognitive service to use.
                var topIntent = recognizerResult.GetTopScoringIntent();
                var topEntity = recognizerResult.Entities.ToObject<MyEntityLuis>();
                string value = topEntity.TipoConsulta?.FirstOrDefault().FirstOrDefault();

                // Next, we call the dispatcher with the top intent.
                await DispatchToTopIntentAsync(turnContext, topIntent.intent, recognizerResult, cancellationToken, value);
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await _dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);

                    //await turnContext.SendActivityAsync(MessageFactory.Text($"Welcome to Dispatch bot {member.Name}. {WelcomeText}"), cancellationToken);
                }
            }
        }

        //public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        //{
        //    await base.OnTurnAsync(turnContext, cancellationToken);
        //    await _conversationState.SaveChangesAsync(turnContext,false,cancellationToken);

        //}


        private async Task DispatchToTopIntentAsync(ITurnContext<IMessageActivity> turnContext, string intent, RecognizerResult recognizerResult, CancellationToken cancellationToken, string value)
        {
            switch (intent)
            {
                case "saludar":
                    //await ProcessHomeAutomationAsync(turnContext, recognizerResult.Properties["luisResult"] as LuisResult, cancellationToken); 
                    await ProcessSampleQnAAsync(turnContext, cancellationToken, intent, value);
                    break;
                case "None":
                    await turnContext.SendActivityAsync(MessageFactory.Text("Lo siento, no entendi. Podrias reformular  tu pregunta"), cancellationToken);
                    break;
                case "Agradecer":
                    //await turnContext.SendActivityAsync(MessageFactory.Text($"Dispatch unrecognized intent: {intent}."), cancellationToken);
                    await ProcessSampleQnAAsync(turnContext, cancellationToken, intent, value);
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
            if (value != null)
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


            var respuesta = results.First().Answer;
            if (results.Any() )
            {
                if(results.Count() > 1)
                {
                    List<PreguntasSugeridas> preguntasSugeridas = new List<PreguntasSugeridas>();
                    for (int i = 1; i < results.Count(); i++)
                    {
                        Random index = new Random();
                        var pregunta = results[i].Questions[index.Next(0, results[i].Questions.Count() - 1)];
                        PreguntasSugeridas sugerencias = new PreguntasSugeridas();
                        sugerencias.Question = pregunta;
                        sugerencias.Answer = results[i].Answer;
                        preguntasSugeridas.Add(sugerencias);
                    }

                    //  await _dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken, respuesta, preguntasSugeridas);
                    await turnContext.SendActivityAsync(MessageFactory.Text(respuesta), cancellationToken);
                    await Task.Delay(1000);
                    await turnContext.SendActivityAsync(activity: CreateHeroCard(preguntasSugeridas), cancellationToken);
                }
                else
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(respuesta), cancellationToken);
                }

            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Lo siento, no te entendí. Podrías reformular tu pregunta?."), cancellationToken);
            }
        }


        private static Activity CreateHeroCard(List<PreguntasSugeridas> preguntas)
        {
            var image = new CardImage();
            var heroCard = new HeroCard();
            heroCard.Title = "";
            heroCard.Text = "Algunas sugerencias para tí";
            
            List<CardAction> buttons = new List<CardAction>();
            foreach (var p in preguntas)
            {
                CardAction button = new CardAction();
                button.Title = p.Question;
                button.Value = p.Question;
                button.Type = ActionTypes.ImBack;
                buttons.Add(button);
            }
            heroCard.Buttons = buttons;

            return MessageFactory.Attachment(heroCard.ToAttachment()) as Activity;

        } // propio de botFramework

        private static Activity WelcomeHeroCard()
        {
            var image = new CardImage();
            image.Url = "https://www.universidades.com.ar/logos/original/logo-universidad-nacional-arturo-jauretche.png";
            var Images = new List<CardImage>();
            Images.Add(image);
            var heroCard = new HeroCard();
            heroCard.Title = "Asistente Virtual UNAJ";
            heroCard.Subtitle = "";
            heroCard.Text = "";
            //heroCard.Text = "Hola! Soy el asistente virtual de la Universidad y estare disponible las 24hs del dia para resonder a tus consultas. Si tienes alguna pregunta no dudes en hacerla,´contengo";
            heroCard.Images = Images;


            return MessageFactory.Attachment(heroCard.ToAttachment()) as Activity;

        } // propio de botFramework
    }


    public class PreguntasSugeridas{
        public string Question { get; set; }
        public string Answer { get; set; }
            }
}
