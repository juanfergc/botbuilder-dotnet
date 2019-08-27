﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder
{
    /// <summary>
    /// An implementation of the <see cref="IBot"/> interface, intended for further subclassing.
    /// </summary>
    /// <remarks>
    /// Derive from this class to plug in code to handle particular activity types.
    /// Pre- and post-processing of <see cref="Activity"/> objects can be added by calling
    /// the base class implementation from the derived class.
    /// </remarks>
    public class ActivityHandler : IBot
    {
        /// <summary>
        /// Called by the adapter (for example, a <see cref="BotFrameworkAdapter"/>)
        /// at runtime in order to process an inbound <see cref="Activity"/>.
        /// </summary>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// This method calls other methods in this class based on the type of the activity to
        /// process, which allows a derived class to provide type-specific logic in a controlled way.
        ///
        /// In a derived class, override this method to add logic that applies to all activity types.
        /// Add logic to apply before the type-specific logic before the call to the base class
        /// <see cref="OnTurnAsync(ITurnContext, CancellationToken)"/> method.
        /// Add logic to apply after the type-specific logic after the call to the base class
        /// <see cref="OnTurnAsync(ITurnContext, CancellationToken)"/> method.
        /// </remarks>
        /// <seealso cref="OnMessageActivityAsync(ITurnContext{IMessageActivity}, CancellationToken)"/>
        /// <seealso cref="OnConversationUpdateActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        /// <seealso cref="OnMessageReactionActivityAsync(ITurnContext{IMessageReactionActivity}, CancellationToken)"/>
        /// <seealso cref="OnEventActivityAsync(ITurnContext{IEventActivity}, CancellationToken)"/>
        /// <seealso cref="OnUnrecognizedActivityTypeAsync(ITurnContext, CancellationToken)"/>
        /// <seealso cref="Activity.Type"/>
        /// <seealso cref="ActivityTypes"/>
        public virtual Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (turnContext.Activity == null)
            {
                throw new ArgumentException($"{nameof(turnContext)} must have non-null Activity.");
            }

            if (turnContext.Activity.Type == null)
            {
                throw new ArgumentException($"{nameof(turnContext)}.Activity must have non-null Type.");
            }

            switch (turnContext.Activity.Type)
            {
                case ActivityTypes.Message:
                    return OnMessageActivityAsync(new DelegatingTurnContext<IMessageActivity>(turnContext), cancellationToken);

                case ActivityTypes.ConversationUpdate:
                    return OnConversationUpdateActivityAsync(new DelegatingTurnContext<IConversationUpdateActivity>(turnContext), cancellationToken);

                case ActivityTypes.MessageReaction:
                    return OnMessageReactionActivityAsync(new DelegatingTurnContext<IMessageReactionActivity>(turnContext), cancellationToken);

                case ActivityTypes.Event:
                    return OnEventActivityAsync(new DelegatingTurnContext<IEventActivity>(turnContext), cancellationToken);

                default:
                    return OnUnrecognizedActivityTypeAsync(turnContext, cancellationToken);
            }
        }

        /// <summary>
        /// Override this in a derived class to provide logic specific to
        /// <see cref="ActivityTypes.Message"/> activities, such as your bot's conversational logic.
        /// </summary>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// When the <see cref="OnTurnAsync(ITurnContext, CancellationToken)"/>
        /// method receives a message activity, it calls this method.
        /// </remarks>
        /// <seealso cref="OnTurnAsync(ITurnContext, CancellationToken)"/>
        protected virtual Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Processes an inbound <see cref="ActivityTypes.ConversationUpdate"/> activity.
        /// </summary>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// When the <see cref="OnTurnAsync(ITurnContext, CancellationToken)"/>
        /// method receives a conversation update activity, it calls this method.
        /// If the conversation update activity indicates that members other than the bot joined the conversation, it calls
        /// <see cref="OnMembersAddedAsync(IList{ChannelAccount}, ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>.
        /// If the conversation update activity indicates that members other than the bot left the conversation, it calls
        /// <see cref="OnMembersRemovedAsync(IList{ChannelAccount}, ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>.
        ///
        /// In a derived class, override this method to add logic that applies to all conversation update activities.
        /// Add logic to apply before the member added or removed logic before the call to the base class
        /// <see cref="OnConversationUpdateActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/> method.
        /// Add logic to apply after the member added or removed logic after the call to the base class
        /// <see cref="OnConversationUpdateActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/> method.
        /// </remarks>
        /// <seealso cref="OnTurnAsync(ITurnContext, CancellationToken)"/>
        /// <seealso cref="OnMembersAddedAsync(IList{ChannelAccount}, ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        /// <seealso cref="OnMembersRemovedAsync(IList{ChannelAccount}, ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        protected virtual Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.MembersAdded != null)
            {
                if (turnContext.Activity.MembersAdded.Any(m => m.Id != turnContext.Activity.Recipient?.Id))
                {
                    return OnMembersAddedAsync(turnContext.Activity.MembersAdded, turnContext, cancellationToken);
                }
            }
            else if (turnContext.Activity.MembersRemoved != null)
            {
                if (turnContext.Activity.MembersRemoved.Any(m => m.Id != turnContext.Activity.Recipient?.Id))
                {
                    return OnMembersRemovedAsync(turnContext.Activity.MembersRemoved, turnContext, cancellationToken);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Override this in a derived class to provide logic for when members other than the bot
        /// join the conversation, such as your bot's welcome logic.
        /// </summary>
        /// <param name="membersAdded">A list of all the members added to the conversation, as
        /// described by the conversation update activity.</param>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// When the <see cref="OnConversationUpdateActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        /// method receives a conversation update activity that indicates one or more users other than the bot
        /// are joining the conversation, it calls this method.
        /// </remarks>
        /// <seealso cref="OnConversationUpdateActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        protected virtual Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Override this in a derived class to provide logic for when members other than the bot
        /// leave the conversation, such as your bot's good-bye logic.
        /// </summary>
        /// <param name="membersRemoved">A list of all the members removed from the conversation, as
        /// described by the conversation update activity.</param>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// When the <see cref="OnConversationUpdateActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        /// method receives a conversation update activity that indicates one or more users other than the bot
        /// are leaving the conversation, it calls this method.
        /// </remarks>
        /// <seealso cref="OnConversationUpdateActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        protected virtual Task OnMembersRemovedAsync(IList<ChannelAccount> membersRemoved, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Processes an inbound <see cref="ActivityTypes.MessageReaction"/> activity.
        /// </summary>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// When the <see cref="OnTurnAsync(ITurnContext, CancellationToken)"/>
        /// method receives a message reaction activity, it calls this method.
        /// If the message reaction indicates that reactions were added to a message, it calls
        /// <see cref="OnReactionsAddedAsync(IList{MessageReaction}, ITurnContext{IMessageReactionActivity}, CancellationToken)"/>.
        /// If the message reaction indicates that reactions were removed from a message, it calls
        /// <see cref="OnReactionsRemovedAsync(IList{MessageReaction}, ITurnContext{IMessageReactionActivity}, CancellationToken)"/>.
        ///
        /// In a derived class, override this method to add logic that applies to all message reaction activities.
        /// Add logic to apply before the reactions added or removed logic before the call to the base class
        /// <see cref="OnMessageReactionActivityAsync(ITurnContext{IMessageReactionActivity}, CancellationToken)"/> method.
        /// Add logic to apply after the reactions added or removed logic after the call to the base class
        /// <see cref="OnMessageReactionActivityAsync(ITurnContext{IMessageReactionActivity}, CancellationToken)"/> method.
        ///
        /// </remarks>
        /// <seealso cref="OnTurnAsync(ITurnContext, CancellationToken)"/>
        /// <seealso cref="OnReactionsAddedAsync(IList{MessageReaction}, ITurnContext{IMessageReactionActivity}, CancellationToken)"/>
        /// <seealso cref="OnReactionsRemovedAsync(IList{MessageReaction}, ITurnContext{IMessageReactionActivity}, CancellationToken)"/>
        protected virtual async Task OnMessageReactionActivityAsync(ITurnContext<IMessageReactionActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.ReactionsAdded != null)
            {
                await OnReactionsAddedAsync(turnContext.Activity.ReactionsAdded, turnContext, cancellationToken).ConfigureAwait(false);
            }

            if (turnContext.Activity.ReactionsRemoved != null)
            {
                await OnReactionsRemovedAsync(turnContext.Activity.ReactionsRemoved, turnContext, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Override this in a derived class to provide logic for when reactions to a previous activity
        /// are added to the conversation.
        /// </summary>
        /// <param name="messageReactions">The list of reactions added.</param>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// Message reactions correspond to the user adding a 'like' or 'sad' etc. (often an emoji) to a
        /// previously sent message on the conversation. Message reactions are supported by only a few channels.
        /// The activity that the message is in reaction to is identified by the activity's
        /// <see cref="Activity.ReplyToId"/> property. The value of this property is the activity ID
        /// of a previously sent activity. When the bot sends an activity, the channel assigns an ID to it,
        /// which is available in the <see cref="ResourceResponse.Id"/> of the result.
        /// </remarks>
        /// <seealso cref="OnMessageReactionActivityAsync(ITurnContext{IMessageReactionActivity}, CancellationToken)"/>
        /// <seealso cref="Activity.Id"/>
        /// <seealso cref="ITurnContext.SendActivityAsync(IActivity, CancellationToken)"/>
        /// <seealso cref="ResourceResponse.Id"/>
        protected virtual Task OnReactionsAddedAsync(IList<MessageReaction> messageReactions, ITurnContext<IMessageReactionActivity> turnContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Override this in a derived class to provide logic for when reactions to a previous activity
        /// are removed from the conversation.
        /// </summary>
        /// <param name="messageReactions">The list of reactions removed.</param>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// Message reactions correspond to the user adding a 'like' or 'sad' etc. (often an emoji) to a
        /// previously sent message on the conversation. Message reactions are supported by only a few channels.
        /// The activity that the message is in reaction to is identified by the activity's
        /// <see cref="Activity.ReplyToId"/> property. The value of this property is the activity ID
        /// of a previously sent activity. When the bot sends an activity, the channel assigns an ID to it,
        /// which is available in the <see cref="ResourceResponse.Id"/> of the result.
        /// </remarks>
        /// <seealso cref="OnMessageReactionActivityAsync(ITurnContext{IMessageReactionActivity}, CancellationToken)"/>
        /// <seealso cref="Activity.Id"/>
        /// <seealso cref="ITurnContext.SendActivityAsync(IActivity, CancellationToken)"/>
        /// <seealso cref="ResourceResponse.Id"/>
        protected virtual Task OnReactionsRemovedAsync(IList<MessageReaction> messageReactions, ITurnContext<IMessageReactionActivity> turnContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Override this in a derived class to provide logic specific to
        /// <see cref="ActivityTypes.Event"/> activities.
        /// </summary>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// When the <see cref="OnTurnAsync(ITurnContext, CancellationToken)"/>
        /// method receives an event activity, it calls this method.
        /// If the event <see cref="IEventActivity.Name"/> is `tokens/response`, it calls
        /// <see cref="OnTokenResponseEventAsync(ITurnContext{IEventActivity}, CancellationToken)"/>;
        /// otherwise, it calls <see cref="OnEventAsync(ITurnContext{IEventActivity}, CancellationToken)"/>.
        ///
        /// In a derived class, override this method to add logic that applies to all event activities.
        /// Add logic to apply before the specific event-handling logic before the call to the base class
        /// <see cref="OnEventActivityAsync(ITurnContext{IEventActivity}, CancellationToken)"/> method.
        /// Add logic to apply after the specific event-handling logic after the call to the base class
        /// <see cref="OnEventActivityAsync(ITurnContext{IEventActivity}, CancellationToken)"/> method.
        ///
        /// Event activities communicate programmatic information from a client or channel to a bot.
        /// The meaning of an event activity is defined by the <see cref="IEventActivity.Name"/> property,
        /// which is meaningful within the scope of a channel.
        /// A `tokens/response` event can be triggered by an <see cref="OAuthCard"/> or an OAuth prompt.
        /// </remarks>
        /// <seealso cref="OnTurnAsync(ITurnContext, CancellationToken)"/>
        /// <seealso cref="OnTokenResponseEventAsync(ITurnContext{IEventActivity}, CancellationToken)"/>
        /// <seealso cref="OnEventAsync(ITurnContext{IEventActivity}, CancellationToken)"/>
        protected virtual Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.Name == "tokens/response")
            {
                return OnTokenResponseEventAsync(turnContext, cancellationToken);
            }

            return OnEventAsync(turnContext, cancellationToken);
        }

        /// <summary>
        /// Override this in a derived class to provide logic for when the bot receives a
        /// <c>tokens/response</c> event.
        /// </summary>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// When the <see cref="OnEventActivityAsync(ITurnContext{IEventActivity}, CancellationToken)"/>
        /// method receives an event with a <see cref="IEventActivity.Name"/> of `tokens/response`,
        /// it calls this method.
        ///
        /// If your bot uses the <c>OAuthPrompt</c>, forward the incoming <see cref="Activity"/> to
        /// the current dialog.
        /// </remarks>
        /// <seealso cref="OnEventActivityAsync(ITurnContext{IEventActivity}, CancellationToken)"/>
        /// <seealso cref="OnEventAsync(ITurnContext{IEventActivity}, CancellationToken)"/>
        protected virtual Task OnTokenResponseEventAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Override this in a derived class to provide logic for when the bot receives an event
        /// that is not a <c>tokens/response</c> event.
        /// </summary>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// When the <see cref="OnEventActivityAsync(ITurnContext{IEventActivity}, CancellationToken)"/>
        /// method receives an event with a <see cref="IEventActivity.Name"/> other than `tokens/response`,
        /// it calls this method.
        /// </remarks>
        /// <seealso cref="OnEventActivityAsync(ITurnContext{IEventActivity}, CancellationToken)"/>
        /// <seealso cref="OnTokenResponseEventAsync(ITurnContext{IEventActivity}, CancellationToken)"/>
        protected virtual Task OnEventAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Override this in a derived class to provide logic for when the bot receives an activity
        /// that is not a message, conversation update, message reaction, or event activity, such as
        /// a contact relation update or end of conversation activity.
        /// </summary>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// When the <see cref="OnTurnAsync(ITurnContext, CancellationToken)"/>
        /// method receives an activity that is not a message, conversation update, message reaction,
        /// or event activity, it calls this method.
        /// </remarks>
        /// <seealso cref="OnTurnAsync(ITurnContext, CancellationToken)"/>
        /// <seealso cref="OnMessageActivityAsync(ITurnContext{IMessageActivity}, CancellationToken)"/>
        /// <seealso cref="OnConversationUpdateActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        /// <seealso cref="OnMessageReactionActivityAsync(ITurnContext{IMessageReactionActivity}, CancellationToken)"/>
        /// <seealso cref="OnEventActivityAsync(ITurnContext{IEventActivity}, CancellationToken)"/>
        /// <seealso cref="Activity.Type"/>
        /// <seealso cref="ActivityTypes"/>
        protected virtual Task OnUnrecognizedActivityTypeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
