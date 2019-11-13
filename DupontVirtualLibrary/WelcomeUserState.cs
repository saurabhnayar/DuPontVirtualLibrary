using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DupontVirtualLibrary
{
    public class WelcomeUserState
    {

        /// <summary>
        /// Gets or sets whether the user has been welcomed in the conversation.
        /// </summary>
        /// <value>The user has been welcomed in the conversation.</value>
        public bool DidBotWelcomeUser { get; set; } = false;
    }
}
