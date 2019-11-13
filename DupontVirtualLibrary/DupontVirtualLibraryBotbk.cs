// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

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
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class DupontVirtualLibraryBotbk : IBot
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>                        
        public static readonly string QnAConfiguration = "Acroynm-kb";
        private readonly BotServices _services;
        public DupontVirtualLibraryBotbk(BotServices services, ILoggerFactory loggerFactory)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
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
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            switch (turnContext.Activity.Type)
            {
                case ActivityTypes.Message:
                   
                    var results = await _services.QnAServices[QnAConfiguration].GetAnswersAsync(turnContext).ConfigureAwait(false);
               
                    if (results.Any())
                    {
                        var ans=results.First().Answer;
                        if (ans.Contains(";"))
                        {
                            var reply = turnContext.Activity.CreateReply("Please type in your question or select from the following:");
                             reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                          //  var attach = SendJournalMessageAsyn();
                            reply.Attachments.Add(SendJournalMessageAsync(ans));
                            await turnContext.SendActivityAsync(reply, cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await turnContext.SendActivityAsync(results.First().Answer, cancellationToken: cancellationToken);
                        }
                    }
                    else
                    {
                        await turnContext.SendActivityAsync($"Sorry, I don't have an answer for that. Please try asking something else related to Virtual Library.");
                    }

                    break;

                case ActivityTypes.ConversationUpdate:
                    foreach (var member in turnContext.Activity.MembersAdded)
                    {
                        if (member.Id != turnContext.Activity.Recipient.Id)
                        {
                            await SendWelcomeMessageAsync(turnContext, cancellationToken);
                        }
                    }

                    break;
               
            }
        }

        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = turnContext.Activity.CreateReply("Great! Hello, I am a Virtual Library digital assistant. I can help you with your questions today");
            // reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            //reply.Attachments.Add(CreateMigrationQnACard().ToAttachment());
            //reply.Attachments.Add(CreateMyMigrationCard().ToAttachment());
            var heroCard = new HeroCard()
            {
                Title = "DuPont Virtual Library",
                Subtitle = "Virtual Library FAQ",
                Text = "You can either type or select from the following options",
                Buttons = new List<CardAction>
                {
                    new CardAction(ActionTypes.ImBack, "Journals?", value: "Online Journals"),
                    new CardAction(ActionTypes.ImBack, "Access?", value: "Access Related"),
                },
            };
            Attachment attach = heroCard.ToAttachment();
            reply.Attachments.Add(attach);
            await turnContext.SendActivityAsync(reply, cancellationToken);          
        }


        private Attachment SendJournalMessageAsync(string answer)
        {
            //var reply = turnContext.Activity.CreateReply("Great! Please type in your question or select from the following:");
            // reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            //reply.Attachments.Add(CreateMigrationQnACard().ToAttachment());
            //reply.Attachments.Add(CreateMyMigrationCard().ToAttachment());
            string[] Qnadata=answer.Split(";");
            var heroCard = new HeroCard()
            {
                Title = Qnadata[0],
                Subtitle = Qnadata[1],
                Text = Qnadata[2],
                Buttons = new List<CardAction>
                {
                    new CardAction(ActionTypes.ImBack, Qnadata[3], value: Qnadata[3]),
                    new CardAction(ActionTypes.ImBack, Qnadata[4], value: Qnadata[4]),
                },
            };
            Attachment attach = heroCard.ToAttachment();
            return heroCard.ToAttachment();
            //reply.Attachments.Add(attach);
            //await turnContext.SendActivityAsync(reply, cancellationToken);
        }
    }
}
