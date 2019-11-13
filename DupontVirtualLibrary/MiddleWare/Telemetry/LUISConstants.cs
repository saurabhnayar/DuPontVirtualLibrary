using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BasicBot.Middleware.Telemetry
{
    public static class LUISConstants
    {
        public const string IntentPrefix = "LuisIntent";  // Application Insights Custom Event name (with Intent)
        public const string IntentProperty = "Intent";
        public const string IntentScoreProperty = "IntentScore";
        public const string ConversationIdProperty = "ConversationId";
        public const string QuestionProperty = "Question";
        public const string ActivityIdProperty = "ActivityId";
        public const string SentimentLabelProperty = "SentimentLabel";
        public const string SentimentScoreProperty = "SentimentScore";

    }
}
