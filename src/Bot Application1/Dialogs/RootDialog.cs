using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Bot_Application1.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private List<string> ColorOptions = new List<string> { "Blond", "Amber", "Fruit", "Dark" };
        private List<string> TasteOptions = new List<string> { "Bitter", "Sweet", "Sour" };
        private List<string> OptionNotFound = new List<string>();
        private Random randomizer = new Random(DateTime.Now.Millisecond);
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
                await context.PostAsync($"Hi, {activity.MembersAdded.First().Name} I am the beer bot.");
                ShowOptions(context);
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

            if (beerChoice == string.Empty)
            {
                await context.PostAsync("Sorry, we couldn't find a beer for your taste.");
            }
            else
            {
                await context.PostAsync($"Ok, so you like a {color} beer with a {taste} taste");
                await context.PostAsync($"We suggest to you {beerChoice}");
            }
            PromptDialog.Confirm(context, OnStartAgain, "You still thirsty?");
        }

        private async Task OnStartAgain(IDialogContext context, IAwaitable<bool> result)
        {
            var choice = await result;
            if (choice)
                ShowOptions(context);
            else
            {
                await context.PostAsync("It was nice choosing beer with you. Goodbye!");
                context.EndConversation("200");
            }
        }

        private string SelectedBeer(string color, string taste)
        {
            var filepath = "https://raw.githubusercontent.com/vermegi/TheBeerBot/master/src/Bot%20Application1/beerlist.csv";
            var webrequest = WebRequest.Create(filepath);
            var strContent = string.Empty;
            using (var response = webrequest.GetResponse())
            {
                using (var content = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(content))
                    {
                        strContent = reader.ReadToEnd();
                    }
                }
            }
            var possiblebeers = strContent.Split('\n');

            var filteredColorList = possiblebeers.Where(pb => pb.ToLower().Contains(color.ToLower()));
            if (filteredColorList.Count() <= 0)
                return string.Empty;

            var results = new List<string>();
            foreach(var line in filteredColorList)
            {
                var splitted = line.Split(',');
                if (taste == "Bitter" && Int32.Parse(splitted[2]) > 20)
                    results.Add(line);
                else if (taste == "Sweet" && Int32.Parse(splitted[3]) > 2)
                    results.Add(line);
                else if (taste == "Sour" && Int32.Parse(splitted[4]) > 2)
                    results.Add(line);
            }

            if (results.Count() <= 0)
                return string.Empty;

            var randomFromList = results.ElementAt(randomizer.Next(results.Count() - 1));

            var randomBeer = randomFromList.Split(',')[0];

            return randomBeer;
        }

        private string GetRandomOptionNotFound()
        {
            return OptionNotFound[randomizer.Next(OptionNotFound.Count -1)];
        }
    }
}