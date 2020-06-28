using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChatBot.Unaj.Dialogs
{
    public class RootDialog : ComponentDialog
    {
        public RootDialog()
        {
            var waterfall = new WaterfallStep[] //invoca a los metodos que se van a ejecutar secuencialmente
            {
                ShowHeroCardsOptions,
                ResponseOptions
            };
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfall));
            AddDialog(new TextPrompt(nameof(TextPrompt)));

        }
        private Task<DialogTurnResult> ShowHeroCardsOptions(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //var hero = new HeroCardsDialog();
            return Common.HeroCardsDialog.ShowOptions(stepContext, cancellationToken);
        }

        private async Task<DialogTurnResult> ResponseOptions(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var options = stepContext.Context.Activity.Text;
            await stepContext.Context.SendActivityAsync("Hola", cancellationToken: cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken);
        }
    }
}
