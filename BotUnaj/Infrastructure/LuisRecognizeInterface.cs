using Microsoft.Bot.Builder.AI.Luis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotUnaj.Infrastructure
{
    public interface LuisRecognizeInterface
    {
        LuisRecognizer _recognizer { get; }
    }
}
