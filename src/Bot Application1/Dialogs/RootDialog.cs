using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Linq;
using System.Collections.Generic;

namespace Bot_Application1.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private List<string> ColorOptions = new List<string> { "Blonde", "Amber", "Fruit", "Dark" };
        private List<string> TasteOptions = new List<string> { "Bitter", "Sweet", "Sour" };

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            if (activity.Type == ActivityTypes.Message)
            {
                // calculate something for us to return
                int length = (activity.Text ?? string.Empty).Length;

                // return our reply to the user
                await context.PostAsync($"You sent {activity.Text} which was {length} characters");

                context.Wait(MessageReceivedAsync);
            }
            else //ConversationUpdated
            {
                if (activity.MembersAdded.Any(mem => mem.Name == activity.Recipient.Name))
                    context.Wait(MessageReceivedAsync);
                else
                {
                    await context.PostAsync($"Hi, {activity.MembersAdded.First().Name} I am the beer bot.");
                    ShowOptions(context);
                }
            }

        }

        private void ShowOptions(IDialogContext context)
        {
            PromptDialog.Choice(context, OnColorSelected, ColorOptions, "What color of beer do you like?", "Not a valid option");
        }

        private async Task OnColorSelected(IDialogContext context, IAwaitable<string> result)
        {
            await context.PostAsync("Excellent choice!");
            PromptDialog.Choice(context, OnTasteSelected, TasteOptions, "And what is your taste?", "Sorry, that's not a valid choice");
        }

        private async Task OnTasteSelected(IDialogContext context, IAwaitable<string> result)
        {
            await context.PostAsync("Excellent choice!");
        }

    }
}