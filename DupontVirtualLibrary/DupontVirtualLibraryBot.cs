using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Search.Query; 


namespace DupontVirtualLibrary
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service. Transient lifetime services are created
    /// each time they're requested. Objects that are expensive to construct, or have a lifetime
    /// beyond a single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?vaiew=aspnetcore-2.1"/>
    public class DupontVirtualLibraryBot : IBot
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>                        
        public static readonly string QnAConfiguration = "VirtualLib-kb";
        public static readonly string LUISConfiguration = "virtuallib-luis";
        public static readonly string DispatchConfiguration = "nlp-with-dispatchDispatch";

        private readonly BotServices _services;
        private readonly WelcomeUserStateAccessors _welcomeUserStateAccessors;

        public DupontVirtualLibraryBot(BotServices services, WelcomeUserStateAccessors statePropertyAccessor, ILoggerFactory loggerFactory)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _welcomeUserStateAccessors = statePropertyAccessor ?? throw new System.ArgumentNullException("state accessor can't be null");
            if (!_services.QnAServices.ContainsKey(QnAConfiguration))
            {
                throw new System.ArgumentException($"Invalid configuration. Please check your '.bot' file for a QnA service named '{QnAConfiguration}'.");
            }

            if (!_services.LuisServices.ContainsKey(LUISConfiguration))
            {
                throw new System.ArgumentException($"Invalid configuration. Please check your '.bot' file for a Luis service named '{LUISConfiguration}'.");
            }

            if (!_services.LuisServices.ContainsKey(DispatchConfiguration))
            {
                throw new System.ArgumentException($"Invalid configuration. Please check your '.bot' file for a Luis service named '{DispatchConfiguration}'.");
            }

        }

        /// <summary>
        /// Every conversation turn calls this method.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // use state accessor to extract the didBotWelcomeUser flag
            var didBotWelcomeUser = await _welcomeUserStateAccessors.WelcomeUserState.GetAsync(turnContext, () => new WelcomeUserState());
            //await turnContext.SendActivityAsync($"OnTurnAsync { didBotWelcomeUser.DidBotWelcomeUser}", cancellationToken: cancellationToken);

            switch (turnContext.Activity.Type)
            {
                case ActivityTypes.Message:
                    // Get the intent recognition result                    
                    var journal_types = "";
                    var accessRelated = "";
                    String[] journal_types_array = null;
                    ConfBotLuisModel luisResults = null;
                    ConfBotLuisModel.Intent topIntent = ConfBotLuisModel.Intent.None;
                    // Perform a call to LUIS to retrieve results for the current activity message.

                    luisResults = await _services.LuisServices[DispatchConfiguration].RecognizeAsync<ConfBotLuisModel>(turnContext, cancellationToken).ConfigureAwait(false);
                    //var topIntent = luisResults?.GetTopScoringIntent();
                    // var recognizerResult = await _services.LuisServices[DispatchConfiguration].RecognizeAsync(turnContext, cancellationToken);
                    var (topScoringIntent, topScore) = luisResults.TopIntent();

                    topIntent = topScoringIntent;
                    var topIntentScore = topScore;
                    var entities = luisResults.Entities;                    
                    var alteredText = luisResults.AlteredText;

                    if (entities._instance.journal_types != null)
                    {
                        //entities.journal_types.All
                        //logger.LogInformation("Received roomType from LUIS: {0}", entities._instance.roomType[0]);
                        journal_types = (string)entities.journal_types[0];
                        journal_types_array = entities.journal_types;
                    }
                    if (entities._instance.accessrelated != null)
                    {
                        //entities.journal_types.All
                        //logger.LogInformation("Received roomType from LUIS: {0}", entities._instance.roomType[0]);
                        accessRelated = (string)entities.accessrelated[0];
                        if (journal_types_array != null)
                        {
                            journal_types_array = journal_types_array.Append("Contact Library").ToArray();
                        }
                        else
                        {
                            journal_types_array = new string[] { "Contact Library" };
                        }


                    }
                    if (topIntent == null)
                    {
                        await turnContext.SendActivityAsync("Unable to get the top intent.");
                    }
                    else
                    {
                        switch (topIntent)
                        {
                            case ConfBotLuisModel.Intent.Journals:
                                await DispatchToQnAMakerAsync(turnContext, journal_types_array, topIntentScore, alteredText);
                                // Here, you can add code for calling the hypothetical home automation service, passing in any entity information that you need
                                break;
                            case ConfBotLuisModel.Intent.None:
                                await DispatchToQnAMakerAsyncNone(turnContext, journal_types, alteredText);
                                break;
                            // You can provide logic here to handle the known None intent (none of the above).
                            // In this example we fall through to the QnA intent.
                            case ConfBotLuisModel.Intent.General:
                                await SendGreetingMessageAsync(turnContext, cancellationToken);
                                break;
                            default:
                                // The intent didn't match any case, so just display the recognition results.
                                // await turnContext.SendActivityAsync($"Dispatch intent: {topIntent.Value.intent} ({topIntent.Value.score}).");
                                await DispatchToQnAMakerAsyncNone(turnContext, journal_types, alteredText);
                                break;
                        }

                        //await DispatchToTopIntentAsync(turnContext, topIntent, cancellationToken);
                    }
                    //}
                    break;
                case ActivityTypes.ConversationUpdate:
                    if (turnContext.Activity.MembersAdded != null)
                    {
                        foreach (var member in turnContext.Activity.MembersAdded)
                        {
                            if (member.Id == turnContext.Activity.Recipient.Id)
                            {
                                if (!didBotWelcomeUser.DidBotWelcomeUser)
                                {
                                    //await turnContext.SendActivityAsync($"from conversation update before { didBotWelcomeUser.DidBotWelcomeUser}", cancellationToken: cancellationToken);

                                    didBotWelcomeUser.DidBotWelcomeUser = true;
                                    //await turnContext.SendActivityAsync($"from conversation update { didBotWelcomeUser.DidBotWelcomeUser}", cancellationToken: cancellationToken);

                                    await SendWelcomeMessageAsync(turnContext, cancellationToken);
                                    await _welcomeUserStateAccessors.WelcomeUserState.SetAsync(turnContext, didBotWelcomeUser);
                                    await _welcomeUserStateAccessors.UserState.SaveChangesAsync(turnContext);
                                    //await _welcomeUserStateAccessors.UserState.SaveChangesAsync(turnContext);
                                    //await turnContext.SendActivityAsync($"from conversation update state change { didBotWelcomeUser.DidBotWelcomeUser}", cancellationToken: cancellationToken);
                                }
                            }
                        }

                    }

                    break;
                case ActivityTypes.Event:

                    break;

            }
            await _welcomeUserStateAccessors.UserState.SaveChangesAsync(turnContext);
        }

        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = turnContext.Activity.CreateReply("Welcome to the DuPont Virtual Library. How can I help you today.");
            // reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            //reply.Attachments.Add(CreateMigrationQnACard().ToAttachment());
            //reply.Attachments.Add(CreateMyMigrationCard().ToAttachment());
            var heroCard = new HeroCard()
            {
                //Title = "DuPont Virtual Library FAQ",
                //Subtitle = "Virtual Library FAQ",
                Text = "You can choose from the common options or can type-in your specific question.",
                Buttons = new List<CardAction>
                {
                    new CardAction(ActionTypes.ImBack, "Need a Journal?", value: "Need a Journal"),
                    new CardAction(ActionTypes.ImBack, "Looking for a Book?", value: "Looking for a Book"),
                    new CardAction(ActionTypes.ImBack, "Standards?", value: "Standards"),
                },
            };
            Microsoft.Bot.Schema.Attachment attach = heroCard.ToAttachment();
            reply.Attachments.Add(attach);
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
        private static async Task SendGreetingMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = turnContext.Activity.CreateReply("Is there something I can help you with today? If none of these options fit, just type your question.");
            var heroCard = new HeroCard()
            {
                Title = "DuPont Virtual Library",
                //Subtitle = "Virtual Library FAQ",
                //Text = "You can either type or select from the following options",
                Buttons = new List<CardAction>
                {
                    new CardAction(ActionTypes.ImBack, "Need a Journal?", value: "Need a Journal"),
                    new CardAction(ActionTypes.ImBack, "Looking for a Book?", value: "Looking for a Book"),
                },
            };
            Microsoft.Bot.Schema.Attachment attach = heroCard.ToAttachment();
            reply.Attachments.Add(attach);
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }





        private async Task DispatchToQnAMakerAsync(ITurnContext context, String[] journal_types, double topscore, string altertext, CancellationToken cancellationToken = default(CancellationToken))
        {
            bool isHeroCard = false;
            if (!string.IsNullOrEmpty(context.Activity.Text))
            {
                if (journal_types != null && journal_types.Length == 1)
                {

                    var reply = context.Activity.CreateReply("I think this link should help you !!");
                    reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    string appName = journal_types[0];
                    if (appName != null && appName != "")
                    {
                        context.Activity.Text = appName;
                    }
                    else if (altertext != null && altertext != "")
                    {
                        context.Activity.Text = altertext;
                    }
                    var results = await _services.QnAServices[QnAConfiguration].GetAnswersAsync(context).ConfigureAwait(false);

                    if (results.Any())
                    {
                        if (results.Length > 1)
                        {

                            List<string> myCollection = new List<string>();
                            for (int jour = 0; jour < results.Length; jour++)
                            {
                                myCollection.Add(results[jour].Answer);
                            }
                            await SendMulitpleAnswersQNAAsync(context, myCollection.ToArray(), cancellationToken: cancellationToken);

                        }
                        else
                        {


                            for (int jour = 0; jour < results.Length; jour++)
                            {
                                var ans = results[jour].Answer;
                                if (ans.Contains(";"))
                                {

                                    isHeroCard = true;
                                    reply.Attachments.Add(SendJournalMessageAsync(ans));
                                    //await context.SendActivityAsync($"Score:  ({topscore}).");

                                }
                                else
                                {
                                    isHeroCard = false;
                                    await context.SendActivityAsync(ans, cancellationToken: cancellationToken);
                                    break;


                                }
                            }
                        }
                        if (isHeroCard)
                        {
                            await context.SendActivityAsync(reply, cancellationToken: cancellationToken);
                            //await context.SendActivityAsync("", cancellationToken: cancellationToken);
                            await Task.Delay(2000);
                            await AskFeedback(context, cancellationToken: cancellationToken);
                        }

                    }
                    else
                    {
                        // await context.SendActivityAsync($"Sorry about that, please contact Librarian at Library@dupont.com.");
                        await context.SendActivityAsync($"Hang tight we are searching Virtual Library Site...");
                        String uname = "yogita.malik@dupont.com";
                        Boolean searchFlag = false;
                        String pwd = "Dupont@123";
                        if (appName != null && appName != "")
                        {
                            context.Activity.Text = appName;
                        }
                        else if (altertext != null && altertext != "")
                        {
                            context.Activity.Text = altertext;
                        }
                        String searchTerm = context.Activity.Text;
                        String siteURL = "https://dupont.sharepoint.com/teams/teams_library/";
                        ClientResult<ResultTableCollection> searchResults = Auth(uname, pwd, siteURL, searchTerm);
                        List<string> myCollection = new List<string>();

                        if (searchResults != null)
                        {
                            foreach (var resultRow in searchResults.Value[0].ResultRows)
                            {
                                //Console.WriteLine("{0}: {1} ({2})", resultRow["Title"], resultRow["Path"], resultRow["Write"]);
                                searchFlag = true;
                                String title = resultRow["Title"].ToString();
                                String path = resultRow["Path"].ToString();
                                String finalresult = title + ";" + title + ";" + path + ";" + "ActionTypes.OpenUrl";
                                myCollection.Add(finalresult);


                            }
                            if (searchFlag == true)
                            {
                                await SendSuggestedActionsAsync(context, myCollection.ToArray(), cancellationToken);

                            }
                            else
                            {
                                await context.SendActivityAsync("Could not find in Virtual Library.Please contact Librarian at Library@dupont.com");
                            }

                        }
                        else
                        {
                            await context.SendActivityAsync("Could not find in Virtual Library.Please contact Librarian at Library@dupont.com");
                        }
                    }

                    isHeroCard = false;
                }
                else if (journal_types != null && journal_types.Length > 1)
                {

                    //List<string> multipleAnswerCollection = new List<string>();
                    List<string> myCollection = new List<string>();

                    for (int jour1 = 0; jour1 < journal_types.Length; jour1++)
                    {
                        string appName = journal_types[jour1];
                        if (appName != null && appName != "")
                        {
                            context.Activity.Text = appName;
                        }
                        else if (altertext != null && altertext != "")
                        {
                            context.Activity.Text = altertext;
                        }
                        var results = await _services.QnAServices[QnAConfiguration].GetAnswersAsync(context).ConfigureAwait(false);

                        if (results.Any())
                        {
                            if (results.Length == 1)
                            {
                                myCollection.Add(results.First().Answer + ";ActionTypes.OpenUrl");
                            }
                            else
                            {
                                myCollection.Add(results.First().Answer + ";ActionTypes.ImBack");
                            }
                        }
                    }

                    await SendSuggestedActionsAsync(context, myCollection.ToArray(), cancellationToken: cancellationToken);


                }
                else
                {

                    var results = await _services.QnAServices[QnAConfiguration].GetAnswersAsync(context).ConfigureAwait(false);
                    if (results.Any())
                    {
                        if (results.Length > 1)
                        {
                            List<string> myCollection = new List<string>();
                            for (int jour = 0; jour < results.Length; jour++)
                            {
                                myCollection.Add(results[jour].Answer);
                            }
                            await SendMulitpleAnswersQNAAsync(context, myCollection.ToArray(), cancellationToken: cancellationToken);
                        }
                        else
                        {
                            var ans = results.First().Answer;

                            if (ans.Contains(";"))
                            {
                                //  var attach = SendJournalMessageAsyn();
                                var reply = context.Activity.CreateReply("Options I could find :");
                                reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                reply.Attachments.Add(SendJournalMessageAsync(ans));
                                await context.SendActivityAsync(reply, cancellationToken: cancellationToken);
                                //await context.SendActivityAsync($"Score:  ({topscore}).");                           
                                await Task.Delay(2000);
                                await AskFeedback(context, cancellationToken: cancellationToken);

                            }

                            else
                            {

                                await context.SendActivityAsync(results.First().Answer, cancellationToken: cancellationToken);
                                //await context.SendActivityAsync($"Score:  ({topscore}).");

                            }
                        }

                    } 
                    else
                    {
                        //await context.SendActivityAsync($"Sorry about that, please contact Librarian at Library@dupont.com.");

                        await context.SendActivityAsync($"Hang tight we are searching Virtual Library Site...");
                        String uname = "yogita.malik@dupont.com";
                        Boolean searchFlag = false;
                        String pwd = "Dupont@123";
                        if (altertext != null && altertext != "")
                        {
                            context.Activity.Text = altertext;
                        }
                        String searchTerm = context.Activity.Text;
                        String siteURL = "https://dupont.sharepoint.com/teams/teams_library/";
                        ClientResult<ResultTableCollection> searchResults = Auth(uname, pwd, siteURL, searchTerm);
                        List<string> myCollection = new List<string>();

                        if (searchResults != null)
                        {
                            foreach (var resultRow in searchResults.Value[0].ResultRows)
                            {
                                //Console.WriteLine("{0}: {1} ({2})", resultRow["Title"], resultRow["Path"], resultRow["Write"]);
                                searchFlag = true;
                                String title = resultRow["Title"].ToString();
                                String path = resultRow["Path"].ToString();
                                String finalresult = title + ";" + title + ";" + path + ";" + "ActionTypes.OpenUrl";
                                myCollection.Add(finalresult);


                            }
                            if (searchFlag == true)
                            {
                                await SendSuggestedActionsAsync(context, myCollection.ToArray(), cancellationToken);

                            }
                            else
                            {
                                await context.SendActivityAsync("Could not find in Virtual Library.Please contact Librarian at Library@dupont.com");
                            }
                            
                        }
                        else
                        {
                            await context.SendActivityAsync("Could not find in Virtual Library.Please contact Librarian at Library@dupont.com");
                        }
                    }



                    


                }
            }
        }

        private async Task DispatchToQnAMakerAsyncNone(ITurnContext context, string appName, string altertext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!string.IsNullOrEmpty(context.Activity.Text))
            {
                
                if (altertext != null && altertext != "")
                {
                    context.Activity.Text = altertext;
                }

                // var results = await _services.QnAServices[appName].GetAnswersAsync(context);
                var results = await _services.QnAServices[QnAConfiguration].GetAnswersAsync(context).ConfigureAwait(false);
                if (results.Any())
                {
                    bool isHeroCardNone = false;
                    var reply = context.Activity.CreateReply("I think this might help you !!");
                    reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    for (int jour = 0; jour < results.Length; jour++)
                    {
                        var ans = results[jour].Answer;
                        if (ans.Contains(";"))
                        {

                            isHeroCardNone = true;
                            reply.Attachments.Add(SendJournalMessageAsync(ans));


                            //await context.SendActivityAsync($"Score:  ({topscore}).");

                        }
                        else
                        {
                            isHeroCardNone = false;
                            await context.SendActivityAsync(ans, cancellationToken: cancellationToken);
                            break;
                        }
                       
                    }
                    if (isHeroCardNone)
                    {
                        await context.SendActivityAsync(reply, cancellationToken: cancellationToken);
                        await Task.Delay(2000);
                        await AskFeedback(context, cancellationToken: cancellationToken);
                    }
                    isHeroCardNone = false;
                }
                else
                {
                    await context.SendActivityAsync($"Hang tight we are searching Virtual Library Site...");
                    String uname = "yogita.malik@dupont.com";
                    Boolean searchFlag = false;
                    String pwd = "Dupont@123";
                    String searchTerm = context.Activity.Text;
                    String siteURL = "https://dupont.sharepoint.com/teams/teams_library/";                                    
                    ClientResult<ResultTableCollection> searchResults = Auth(uname, pwd, siteURL, searchTerm);
                    List<string> myCollection = new List<string>();

                    if (searchResults != null)
                    {
                        foreach (var resultRow in searchResults.Value[0].ResultRows)
                        {
                            //Console.WriteLine("{0}: {1} ({2})", resultRow["Title"], resultRow["Path"], resultRow["Write"]);
                            searchFlag = true;
                            String title = resultRow["Title"].ToString();
                            String path = resultRow["Path"].ToString();
                            String finalresult = title + ";" + title + ";" + path + ";" + "ActionTypes.OpenUrl";
                            myCollection.Add(finalresult);


                        }
                        if (searchFlag == true)
                        {
                            await SendSuggestedActionsAsync(context, myCollection.ToArray(), cancellationToken);


                        }
                        else
                        {
                            await context.SendActivityAsync("Could not find in Virtual Library.Please contact Librarian at Library@dupont.com");
                        }
                        
                    }
                    else
                    {
                        await context.SendActivityAsync("Could not find in Virtual Library.Please contact Librarian at Library@dupont.com");
                    }

                }


                //await context.SendActivityAsync(results.First().Answer, cancellationToken: cancellationToken);
            }
            else
            {
                await context.SendActivityAsync($"Couldn't find an answer in the {appName}.");
            }
        }

        private Microsoft.Bot.Schema.Attachment SendJournalMessageAsync(string answer)
        {
            string[] Qnadata = answer.Split(";");
            var heroCard = new HeroCard()
            {
                //Title = Qnadata[0],
                 //Subtitle = Qnadata[0],
                //Text = Qnadata[2],
                Buttons = new List<CardAction>
                {
                    new CardAction(ActionTypes.OpenUrl, Qnadata[1], value: Qnadata[2]),
                    // new CardAction(ActionTypes.OpenUrl, "contact lib", value: "jai - prakash.yadav@dupont.com"),

                },
            };
            Microsoft.Bot.Schema.Attachment attach = heroCard.ToAttachment();
            return heroCard.ToAttachment();
            //reply.Attachments.Add(attach);
            //await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        private async Task SendSuggestedActionsAsync(ITurnContext turnContext, string[] answers, CancellationToken cancellationToken)
        {
            var reply = turnContext.Activity.CreateReply("Here is what I could find or you can contact librarian if this link doesn't help.");
            //TextInfo ti = CultureInfo.CurrentCulture.TextInfo;
            //String abc= ti.ToTitleCase("DV");
            var heroCard = new HeroCard();
            for (int i = 0; i < answers.Length; i++)
            {

                string[] Qnadata = answers[i].Split(";");
                if (Qnadata[3].Equals("ActionTypes.OpenUrl"))
                {

                    heroCard = new HeroCard()
                    {
                        //Title = "DuPont Virtual Library",
                        //Subtitle = "Virtual Library FAQ",
                        //Text = "You can either type or select from the following options",                 
                        Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.OpenUrl, Qnadata[1], value: Qnadata[2].Trim()),
                         //new CardAction(ActionTypes.OpenUrl, Qnadata[1], value: Qnadata[2].Trim()),
                    },

                    };
                }
                else
                {
                    heroCard = new HeroCard()
                    {
                        //Title = "DuPont Virtual Library",
                        //Subtitle = "Virtual Library FAQ",
                        //Text = "You can either type or select from the following options",                 
                        Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.ImBack, Qnadata[1], value: Qnadata[1]),
                         //new CardAction(ActionTypes.OpenUrl, Qnadata[1], value: Qnadata[2].Trim()),
                    },

                    };

                }
                Microsoft.Bot.Schema.Attachment attach = heroCard.ToAttachment();
                reply.Attachments.Add(attach);
            }

            await turnContext.SendActivityAsync(reply, cancellationToken);
            await Task.Delay(2000);
            await AskFeedback(turnContext, cancellationToken: cancellationToken);
        }

        private async Task SendMulitpleAnswersQNAAsync(ITurnContext turnContext, string[] answers, CancellationToken cancellationToken)
        {
            var reply = turnContext.Activity.CreateReply("Options I could find : ");
            for (int i = 0; i < answers.Length; i++)
            {
                string[] Qnadata = answers[i].Split(";");
                var heroCard = new HeroCard()
                {
                    //Title = "DuPont Virtual Library",
                    //Subtitle = "Virtual Library FAQ",
                    //Text = "You can either type or select from the following options",

                    Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.OpenUrl, Qnadata[1], value: Qnadata[2].Trim()),
                    // new CardAction(ActionTypes.OpenUrl, Qnadata[1], value: "jai - prakash.yadav@dupont.com" ),

                    },

                };

                Microsoft.Bot.Schema.Attachment attach = heroCard.ToAttachment();
                reply.Attachments.Add(attach);
            }


            await turnContext.SendActivityAsync(reply, cancellationToken);
            await Task.Delay(2000);
            await AskFeedback(turnContext, cancellationToken: cancellationToken);
        }



        private async Task AskFeedback(ITurnContext context, CancellationToken cancellationToken = default(CancellationToken))
        {

            var feedback = ((Activity)context.Activity).CreateReply("Was that helpful?");
            feedback.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction(){ Title = "👍", Type=ActionTypes.PostBack, Value="yesfeedback" },
                    new CardAction(){ Title = "👎", Type=ActionTypes.PostBack, Value="nofeedback" }
                }
            };
            await context.SendActivityAsync(feedback, cancellationToken: cancellationToken);
            //context.Wait(this.MessageReceivedAsync);
        }

        private static ClientResult<ResultTableCollection> Auth(String uname, String pwd, string siteURL, String searchTerm)
        {
            ClientContext context = new ClientContext(siteURL);
            //String title = "";
            //String path = "";
            ClientResult<ResultTableCollection> results = null;
            Web web = context.Web;
            SecureString passWord = new SecureString();
            foreach (char c in pwd.ToCharArray()) passWord.AppendChar(c);
            context.Credentials = new SharePointOnlineCredentials(uname, pwd);
            try
            {
                context.Load(web);
                //context.ExecuteQuery();
                // Console.WriteLine("Olla! from " + web.Title + " site");
                KeywordQuery keywordQuery = new KeywordQuery(context);
                //keywordQuery.QueryText = "SharePoint";
                keywordQuery.QueryText = searchTerm + " site:" + siteURL;
                keywordQuery.StartRow = 0;
                keywordQuery.RowLimit = 3;
                SearchExecutor searchExecutor = new SearchExecutor(context);
                results = searchExecutor.ExecuteQuery(keywordQuery);
                //context.ExecuteQueryAsync();
                if (context.HasPendingRequest)
                {
                    context.ExecuteQueryAsync()
                       .Wait();
                }

                //foreach (var resultRow in results.Value[0].ResultRows)
                //    foreach (var resultRow in results.Value[0].ResultRows)
                //    {
                //        Console.WriteLine("resultRow=", resultRow);
                //    }
                //Console.ReadLine();
                //Console.WriteLine("After running the query ");
                return results;
            }
            catch (Exception e)
            {
                //title = "jai";
                //Console.WriteLine("Something went wrong in Auth Module" + e);
                //Console.ReadKey();
                return results;
            }
        }

    }


}