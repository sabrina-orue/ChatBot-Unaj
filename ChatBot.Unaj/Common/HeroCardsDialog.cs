using AdaptiveExpressions;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChatBot.Unaj.Common
{
    public class HeroCardsDialog
    {
        
        public HeroCardsDialog()
        {
        }

        public static async Task<DialogTurnResult> ShowOptions(WaterfallStepContext stepContext, CancellationToken cancellationToken) {
            //CONTIENE LA CARDS
            var options = await stepContext.PromptAsync(
                nameof(TextPrompt),
                new PromptOptions
                {
                    Prompt = CreateHeroCard()
                },
                cancellationToken
            );
            return options;

        }

        private static Activity CreateHeroCard()
        {
            var image = new CardImage();
            image.Url = "https://upload.wikimedia.org/wikipedia/commons/2/24/Unaj.jpg";
            var Images = new List<CardImage>();
            Images.Add(image);
            var heroCard = new HeroCard();
            heroCard.Title = "Asistente Virtual UNAJ";
            heroCard.Subtitle = "";
            heroCard.Text ="Hola! Soy el asistente virtual de la Universidad y estare disponible las 24hs del dia para resonder a tus consultas. Si tienes alguna pregunta no dudes en hacerla,´contengo un monton de información en mi base de conocimientos relacionadas con el Siu Guarani, Campus Virtual, Calendario, Inscripciones y más! ";
            heroCard.Images = Images;
         

            return MessageFactory.Attachment(heroCard.ToAttachment()) as Activity;

        } // propio de botFramework
    };

}
