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

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                //verificamos si el usuario ya ingreso antes
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    //mensaje de Bienvenida
                    await turnContext.SendActivityAsync(activity: WelcomeHeroCard(), cancellationToken);
                    await Task.Delay(500);
                    await turnContext.SendActivityAsync(MessageFactory.Text("Hola! Soy el asistente virtual de la Universidad. Estoy disponible las 24hs del dia para responder tus consultas. Tengo información en mi base de conocimientos relacionada a Siu Guarani, Campus Virtual, Inscripciones y más!"), cancellationToken);
                }
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            //MENSAJE DE BIENVENIDA PARA TELEGRAM
            if (turnContext.Activity.Text.Equals("/start"))
            {
                await turnContext.SendActivityAsync(activity: WelcomeHeroCard(), cancellationToken);
                await Task.Delay(500);
                await turnContext.SendActivityAsync(MessageFactory.Text("Hola! Soy Arturito tu asistente virtual de la UNAJ. Estoy disponible las 24hs del dia para responder tus consultas. Tengo información en mi base de conocimientos relacionada a Siu Guarani, Campus Virtual, Inscripciones, calendario académico y más!"), cancellationToken);
            }
            else
            {
                //turnContext contiene la pregunta del usuaio, se envía al servicio de Luis y se obtiene una respuesta.
                var recognizerResult = await _botServices.Dispatch.RecognizeAsync(turnContext, cancellationToken);

                //Tomamos la intencion con el porcentage mas alto de coincidencia
                var topIntent = recognizerResult.GetTopScoringIntent();

                //Tomamos la entidad con mayor coincidencia
                var topEntity = recognizerResult.Entities.ToObject<MyEntityLuis>();
                string valueEntity = topEntity.TipoConsulta?.FirstOrDefault().FirstOrDefault();

                //Llamamos al metodo para determinar que intencion tuvo el usuario segun LUIS
                await DispatchToTopIntentAsync(turnContext, topIntent.intent, cancellationToken, valueEntity);
            }
        }

    
        
        private async Task DispatchToTopIntentAsync(ITurnContext<IMessageActivity> turnContext, string intent, CancellationToken cancellationToken, string valueEntity)
        {
            switch (intent)
            {
                case "saludar":                    
                    await turnContext.SendActivityAsync(MessageFactory.Text("Hola ¿cómo estas? Dime en que puedo ayudarte?"), cancellationToken);
                    break;

                case "agradecer":
                    await turnContext.SendActivityAsync(MessageFactory.Text("Por nada! Siempre estaré aquí para ayudarte en lo que necesites!"), cancellationToken);            
                    break;

                case "molestar":
                    await turnContext.SendActivityAsync(MessageFactory.Text("Siento mucho no agradarte, o no serte útil. Te pido me trates con respeto, recuerda que estamos en un ámbito educativo y me programaron para ser muy amigable y servicial. Tenme paciencia, con el tiempo ire aprendiendo mas acerca de tus necesidades."), cancellationToken);
                    break;

               case "consultar":
                    //Creamos un metodo que nos conecta con QnA para obtener la respuesta
                    await ConnectQnAAsync(turnContext, cancellationToken, intent, valueEntity);
                    break;
               case "despedir":
                    await turnContext.SendActivityAsync(MessageFactory.Text("Hasta pronto, espero haberte ayudado! Recuerda que estoy aqui para cuando lo necesites"), cancellationToken);
                    break;

                default:
                    await turnContext.SendActivityAsync(MessageFactory.Text("Lo siento, no entendí. Podrías reformular  tu pregunta"), cancellationToken);
                    break;
            }
        }

      
        private async Task ConnectQnAAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken, string topIntent, string valueEntity)  {
            QueryResult[] results;
            _logger.LogInformation("ConnectQnAAsync");
            if (valueEntity != null)
            {  // Creamos el metadata de qna con la intension y la entidad que nos devolvio LUIS
                var metadata = new Microsoft.Bot.Builder.AI.QnA.Metadata();
                var qnaOptions = new QnAMakerOptions();
                metadata.Name = topIntent;
                metadata.Value = valueEntity;
                qnaOptions.Top = 10; //maximo de respuestas relacionadas a la entidad
                qnaOptions.StrictFilters = new Microsoft.Bot.Builder.AI.QnA.Metadata[] { metadata };
                qnaOptions.ScoreThreshold = 0.1F;// minima probabilidad de coincidencia
                //Hacemos la consulta a QnA enviandole el metaData y la pregunta del usuario
                results = await _botServices.SampleQnA.GetAnswersAsync(turnContext, qnaOptions);
            }
            else
            {  //Si la entidad que devuelve LUIS es nula hacemos el llamado a QnA solo para traer la respuesta.
                results = await _botServices.SampleQnA.GetAnswersAsync(turnContext);
            }
            //Obtenemos la respuesta a la pregunta que nos devolvio Qna
            var respuesta = results.First().Answer;
            if (results.Any() )
            { //Si hay mas de una respuesta, se generan los botones de sugerencia
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
                    //Se envia la respuesta al usuario, 
                    await turnContext.SendActivityAsync(MessageFactory.Text(respuesta), cancellationToken);
                    await Task.Delay(500);
                    //Se envia la tarjeta con botones de sugerencias relacionado al contexto de la entidad.
                    await turnContext.SendActivityAsync(activity: CreateHeroCard(preguntasSugeridas), cancellationToken);
                }
                else
                { //Si qna devuelve solo una respuesta, no se crean los botones de sugerencia.
                    await turnContext.SendActivityAsync(MessageFactory.Text(respuesta), cancellationToken);
                }
            }
            else
            {   //Si qna no devuelve ninguna respuesta.
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
            image.Url = "https://i.ibb.co/L5btykf/Photo-1593779850001.png";
            var Images = new List<CardImage>();
            Images.Add(image);
            var heroCard = new HeroCard();
            heroCard.Title = "";
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
