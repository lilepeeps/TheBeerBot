using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Bot_Application1.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private List<string> ColorOptions = new List<string> { "Blonde", "Amber", "Fruit", "Dark" };
        private List<string> TasteOptions = new List<string> { "Bitter", "Sweet", "Sour" };
        private List<string> OptionNotFound = new List<string>();
        private Random randomizer = new Random(System.DateTime.Now.Millisecond);
        private string AppPath;

        public RootDialog(string appPath)
        {
            AppPath = appPath;
        }

        public Task StartAsync(IDialogContext context)
        {
            OptionNotFound = AllStrings.OptionNotFound.Split(',').ToList();
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
            PromptDialog.Choice(context, OnColorSelected, ColorOptions, "What color of beer do you like?", GetRandomOptionNotFound());
        }

        private async Task OnColorSelected(IDialogContext context, IAwaitable<string> result)
        {
            context.ConversationData.SetValue("SelectedColor", await result);
            await context.PostAsync("Excellent choice!");
            PromptDialog.Choice(context, OnTasteSelected, TasteOptions, "And what is your taste?", GetRandomOptionNotFound());
        }

        private async Task OnTasteSelected(IDialogContext context, IAwaitable<string> result)
        {
            context.ConversationData.SetValue("SelectedTaste", await result);
            await context.PostAsync("Excellent choice!");
            var color = context.ConversationData.GetValue<string>("SelectedColor");
            var taste = context.ConversationData.GetValue<string>("SelectedTaste");
            var beerChoice = SelectedBeer(color, taste);

            await context.PostAsync($"Ok, so you like a {color} beer with a {taste} taste");
            await context.PostAsync($"We suggest to you {beerChoice}");
        }

        private string SelectedBeer(string color, string taste)
        {
            var possiblebeers = File.ReadLines("./beerlist.csv");

            var filteredList = possiblebeers.Where(pb => pb.Contains(color) && pb.Contains(taste));

            var randomFromList = filteredList.ElementAt(randomizer.Next(filteredList.Count() - 1));

            var randomBeer = randomFromList.Split(',')[0];

            return randomBeer;
        }

        private string GetRandomOptionNotFound()
        {
            return OptionNotFound[randomizer.Next(OptionNotFound.Count -1)];
        }
    }
}