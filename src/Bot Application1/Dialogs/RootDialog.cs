using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;

namespace Bot_Application1.Dialogs
{
    [Serializable]
    [LuisModel("<your LUIS model id>", "<your subscription key>")]
    public class RootDialog : LuisDialog<object>
    {
        private List<string> ColorOptions = new List<string> { "Blond", "Amber", "Fruit", "Dark" };
        private List<string> TasteOptions = new List<string> { "Bitter", "Sweet", "Sour" };
        private List<string> OptionNotFound = new List<string>();
        private Random randomizer = new Random(DateTime.Now.Millisecond);
        private string AppPath;

        public RootDialog() : base()
        {
            OptionNotFound = "Shit Hoppens, Trouble is brewing, We don't have that option on tap, Tuns of beer are coming down".Split('|').ToList();
        }

        protected async override Task MessageReceived(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var activity = await item;

            // Check if we received an attachment
            if (activity.Attachments.Count() > 0)
            {
                await context.PostAsync($"Got your image...processing...");

                //HACK: hard-code the image recognition to Duvel
                await context.PostAsync($"I think you're drinking a **Duvel**!");

                var beerChoice = SelectedBeer("Blond", "Bitter", "duvel");

                await context.PostAsync($"If you're still thirsty, I'd recommend you now drink a **{beerChoice.Name}**");
                var replyBeerMessage = context.MakeMessage();
                replyBeerMessage.Attachments.Add(new Attachment
                {
                    ContentUrl = $"https://github.com/vermegi/TheBeerBot/raw/master/src/Bot%20Application1/photos/{beerChoice.Pic}",
                    ContentType = "image/png",
                    Name = "Beerpic.png"
                });
                await context.PostAsync(replyBeerMessage);
                await context.PostAsync(beerChoice.Explanation);


                context.Wait(MessageReceived);
            }
            else
            {
                await base.MessageReceived(context, item);
            }

        }

        [LuisIntent("None")]
        public async Task NoIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"I didn't quite get that. You can ask me to choose a beer based on your taste or by letting me recognize a beer bottle.");
            context.Wait(MessageReceived);
        }

        [LuisIntent("RecognizeBeer")]
        public async Task RecognizeBeer(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"Just send me a picture of the beer, and I'll try to identify it.");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Greeting")]
        public async Task Greeting(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"Hi, I am the Beer Pressure Bot. I can help you choose a beer based on your taste or based on what you're drinking right now.");
            await context.PostAsync($"Try asking me 'I\'m thirsty' or 'Do you know what I\'m drinking'.");

            context.Wait(MessageReceived);
        }


        [LuisIntent("ChooseBeer")]
        public async Task ShowOptions(IDialogContext context, LuisResult result)
        {
            PromptDialog.Choice(context, OnColorSelected, ColorOptions, "What color of beer do you like?", GetRandomOptionNotFound());
        }

        private async Task OnColorSelected(IDialogContext context, IAwaitable<string> result)
        {
            context.ConversationData.SetValue("SelectedColor", await result);
            await context.PostAsync("Nothing wrong with that choice.");
            PromptDialog.Choice(context, OnTasteSelected, TasteOptions, "And what is your taste?", GetRandomOptionNotFound());
        }

        private async Task OnTasteSelected(IDialogContext context, IAwaitable<string> result)
        {
            context.ConversationData.SetValue("SelectedTaste", await result);
            await context.PostAsync("Got that.");
            var color = context.ConversationData.GetValue<string>("SelectedColor");
            var taste = context.ConversationData.GetValue<string>("SelectedTaste");
            var beerChoice = SelectedBeer(color, taste);

            if (beerChoice == null)
            {
                await context.PostAsync("Sorry, we couldn't find a beer for your taste.");
                PromptDialog.Confirm(context, OnStartAgain, "Want to go for another hopportunity?");
            }
            else
            {
                await context.PostAsync($"Ok, so you like a {color} beer with a {taste} taste");
                var replyMessage = context.MakeMessage();
                replyMessage.Attachments.Add(new Attachment()
                {
                    ContentUrl = "https://media4.giphy.com/media/zrj0yPfw3kGTS/giphy.gif",
                    ContentType = "image/gif",
                    Name = "Processing.gif"
                });
                await context.PostAsync("Thinking ... ");
                await context.PostAsync(replyMessage);
                await context.PostAsync($"We suggest to you **{beerChoice.Name}**");
                var replyBeerMessage = context.MakeMessage();
                replyBeerMessage.Attachments.Add(new Attachment
                {
                    ContentUrl = $"https://github.com/vermegi/TheBeerBot/raw/master/src/Bot%20Application1/photos/{beerChoice.Pic}",
                    ContentType = "image/png",
                    Name = "Beerpic.png"
                });
                await context.PostAsync(replyBeerMessage);
                await context.PostAsync(beerChoice.Explanation);

                context.Wait(MessageReceived);
            }
        }

        private async Task OnStartAgain(IDialogContext context, IAwaitable<bool> result)
        {
            var choice = await result;
            if (choice)
                ShowOptions(context, null);
            else
            {
                await context.PostAsync("It was pouring drinks for you. Cheers!");
                context.EndConversation("200");
            }
        }

        private Beer SelectedBeer(string color, string taste, string currentBeer = null)
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
                return null;

            var results = new List<string>();
            foreach (var line in filteredColorList)
            {
                var splitted = line.Split('|');

                if (string.IsNullOrEmpty(currentBeer) ||
                    (!string.IsNullOrEmpty(currentBeer) && currentBeer != splitted[0])
                    )
                {
                    if (taste == "Bitter" && Int32.Parse(splitted[2]) > 20)
                        results.Add(line);
                    else if (taste == "Sweet" && Int32.Parse(splitted[3]) > 2)
                        results.Add(line);
                    else if (taste == "Sour" && Int32.Parse(splitted[4]) > 2)
                        results.Add(line);
                }
                else
                {

                }
            }

            if (results.Count() <= 0)
                return null;

            var randomFromList = results.ElementAt(randomizer.Next(results.Count() - 1));

            var randomBeer = randomFromList.Split('|')[0];
            var randomPic = randomFromList.Split('|')[6];
            var randomText = randomFromList.Split('|')[7];

            return new Beer { Name = randomBeer, Pic = randomPic, Explanation = randomText };
        }

        private string GetRandomOptionNotFound()
        {
            return OptionNotFound[randomizer.Next(OptionNotFound.Count - 1)];
        }
    }

    public class Beer
    {
        public string Name { get; set; }
        public string Pic { get; set; }

        public string Explanation { get; set; }
    }
}
