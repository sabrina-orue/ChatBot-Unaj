using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotUnaj.Infrastructure
{
    public class LuisRecognizeClass: LuisRecognizeInterface
    {
        public LuisRecognizer _recognizer { get; private set; }

        public LuisRecognizeClass(IConfiguration configuration) //Constructor
        {
            var luisApplication = new LuisApplication( //Creo la aplicacion Luis
                //Llamo a las claves de congiguracion creadas en appsettings
                configuration["LuisAppId"],
                configuration["LuisAPIKey"],
                configuration["LuisHostName"]
                );
            var recognizerOptions = new LuisRecognizerOptionsV3(luisApplication) //Aca obtenemos las respuestas del servicio
            {
                PredictionOptions = new Microsoft.Bot.Builder.AI.LuisV3.LuisPredictionOptions()
                {
                    IncludeInstanceData = true
                }
            };

            _recognizer = new LuisRecognizer(recognizerOptions); //Asigno las respuestas o resultados a recognizer
       }
    }
}
