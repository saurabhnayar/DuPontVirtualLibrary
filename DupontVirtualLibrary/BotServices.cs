using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;

namespace DupontVirtualLibrary
{
    public class BotServices
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="BotServices"/> class.
        /// </summary>
        /// <param name="botConfiguration">The <see cref="BotConfiguration"/> instance for the bot.</param>
        /// <param name="luisServices">A dictionary of named <see cref="LuisRecognizer"/> instances for usage within the bot.</param>
        public BotServices(BotConfiguration botConfiguration)
        {
            var qnaServices = new Dictionary<string, QnAMaker>();
            var luisServices = new Dictionary<string, LuisRecognizer>();

            foreach (var service in botConfiguration.Services)
            {
                switch (service.Type)
                {
                    case ServiceTypes.AppInsights:
                        {
                            var appInsights = service as AppInsightsService;
                            if (appInsights == null)
                            {
                                throw new InvalidOperationException("Application Insights is not configured correctly in the '.bot' file.");
                            }

                            if (string.IsNullOrWhiteSpace(appInsights.InstrumentationKey))
                            {
                                throw new InvalidOperationException("The Application Insights Instrumentation Key is required. Please update the '.bot' file.");
                            }
                            var telemetryConfig = new TelemetryConfiguration(appInsights.InstrumentationKey);
                            TelemetryClient = new TelemetryClient(telemetryConfig)
                            {
                                InstrumentationKey = appInsights.InstrumentationKey,
                            };
                            break;
                        }

                    case ServiceTypes.Luis:                       

                            var luis = (LuisService)service;                        
                            if (luis == null)
                            {
                                throw new InvalidOperationException("The LUIS service is not configured correctly in your '.bot' file.");
                            }                        
                          
                            var app = new LuisApplication(luis.AppId, luis.AuthoringKey, luis.GetEndpoint());
                            //new LuisApplication()
                            var app1 = new LuisPredictionOptions();
                            app1.SpellCheck = true;
                            app1.BingSpellCheckSubscriptionKey = "89e2089bd5654f2da377d4f7dc64b391";                            
                            var recognizer = new LuisRecognizer(app,app1);
                            this.LuisServices.Add(luis.Name, recognizer);
                            break;
                        
                    // var luis = (LuisService)service;
                    //   if (luis == null)
                    // {
                    //   throw new InvalidOperationException("The LUIS service is not configured correctly in your '.bot' file.");
                    //}

                    // var luisapp = new LuisApplication(luis.AppId, luis.AuthoringKey, luis.GetEndpoint());
                    //LuisServices.Add(service.Id, new Win10AppInsightsLUISRecognizer(luisapp));

                    //break;    

                    case ServiceTypes.Dispatch:
                        // ...
                        var dispatch = (DispatchService)service;
                        if (dispatch == null)
                            {
                                throw new InvalidOperationException("The Dispatch service is not configured correctly in your '.bot' file.");
                            }

                        var dispatchApp = new LuisApplication(dispatch.AppId, dispatch.AuthoringKey, dispatch.GetEndpoint());
                           
                        var app2 = new LuisPredictionOptions();
                        app2.SpellCheck = true;
                        app2.BingSpellCheckSubscriptionKey = "89e2089bd5654f2da377d4f7dc64b391";
                        // Since the Dispatch tool generates a LUIS model, we use the LuisRecognizer to resolve the
                        // dispatching of the incoming utterance.
                        var dispatchARecognizer = new LuisRecognizer(dispatchApp,app2);
                       this.LuisServices.Add(dispatch.Name, dispatchARecognizer);
                        break;

                    case ServiceTypes.QnA:
                        {
                            var qna = (QnAMakerService)service;
                            if (qna == null)
                            {
                                throw new InvalidOperationException("The QnA service is not configured correctly in your '.bot' file.");
                            }

                            var qnaOptions =
                                new QnAMakerOptions()
                                {
                                    ScoreThreshold = 0.7F,
                                    Top = 3
                                };

                            var qnaEndPoint = new QnAMakerEndpoint()
                            {
                                KnowledgeBaseId = qna.KbId,
                                EndpointKey = qna.EndpointKey,
                                Host = qna.Hostname,
                                
                            };

                            var qnaMaker = new QnAMaker(qnaEndPoint, qnaOptions);
                            // qnaServices.Add(qna.Name, qnaMaker);
                            // var qnaMaker = new Win10AppInsightsQnaMaker(qnaEndPoint);
                            
                            this.QnAServices.Add(qna.Name, qnaMaker);

                            break;
                        }
                }
            }
        }
        /// <summary>
        /// Gets the set of AppInsights Telemetry Client used.
        /// </summary>
        /// <remarks>The AppInsights Telemetry Client should not be modified while the bot is running.</remarks>
        /// <value>
        /// A <see cref="TelemetryClient"/> client instance created based on configuration in the .bot file.
        /// </value>
       public TelemetryClient TelemetryClient { get; set; }
       

    /// <summary>
    /// Gets the set of LUIS Services used.
    /// Given there can be multiple <see cref="LuisRecognizer"/> services used in a single bot,
    /// LuisServices is represented as a dictionary.  This is also modeled in the
    /// ".bot" file since the elements are named.
    /// </summary>
    /// <remarks>The LUIS services collection should not be modified while the bot is running.</remarks>
    /// <value>
    /// A <see cref="LuisRecognizer"/> client instance created based on configuration in the .bot file.
    /// </value>
    public Dictionary<string, LuisRecognizer> LuisServices { get; } = new Dictionary<string, LuisRecognizer>();

        public Dictionary<string, QnAMaker> QnAServices { get; } = new Dictionary<string, QnAMaker>();
    }
}

