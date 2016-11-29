using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Bot_ApplicationBank.Model;
using System.Collections.Generic;
using System.Reflection;
using Bot_ApplicationBank.DataModels;
using Microsoft.WindowsAzure.MobileServices;
using Microsoft.Bot.Builder.Luis;
using static System.Net.Mime.MediaTypeNames;

namespace Bot_ApplicationBank
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        /// 

        //Seperate exchange rates for different currency, put each rate in a list, return a list of exchange rates
        public List<string> getRates(ExchangeObject.RootObject root)
        {
            string temp = "";
            var dictionary = new Dictionary<string, object> { };

            Type myType = root.rates.GetType();

            IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());

            List<string> rateList = new List<string>();

            foreach (PropertyInfo prop in props)
            {
                object propValue = prop.GetValue(root.rates, null);
                dictionary.Add(prop.Name, propValue);
            }

            foreach (KeyValuePair<string, object> kvp in dictionary)
            {
                temp = string.Format("{0}, {1}", kvp.Key, kvp.Value);
                rateList.Add(temp);
            }

            return rateList;
        }
        
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {

            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                StateClient stateClient = activity.GetStateClient();

                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

                List<string> currenList = new List<string> { "nzd", "usd", "aud", "bng", "brl", "cad", "chf", "cny", "czk", "dkk", "gbp", "hkd", "hrk", "huf", "idr", "ils", "inr", "jpy", "krw", "mxn", "myr", "nok", "php", "pln", "ron", "rub", "sek", "sgd", "thb", "try", "zar", "eur" };

                ExchangeObject.RootObject rootObject;

                HttpClient client = new HttpClient();



                var userMessage = activity.Text;

                string endOutput = "";
                
                Activity greating = activity.CreateReply(endOutput);
                bool isrequest = true;
                bool isgreeting = true;


                // calculate something for us to return
                if (userData.GetProperty<bool>("SentGreeting"))
                {
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "http://icons.iconarchive.com/icons/graphicloads/flat-finance/128/bank-icon.png"));
                    
                    greating.Recipient = activity.From;
                    greating.Type = "message";
                    greating.Attachments = new List<Attachment>();


                    HeroCard plCard = new HeroCard()
                    {
                        Title = "Hello again",
                        Text = "Check the exchange rate for any currency by entering the 3-digit currency code.\n\n Set your base currency, try 'Set base to NZD'. :)",
                        Images = cardImages

                    };

                    Attachment plAttachment = plCard.ToAttachment();
                    greating.Attachments.Add(plAttachment);
                }
                else
                {
                    userData.SetProperty<bool>("SentGreeting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: "http://icons.iconarchive.com/icons/graphicloads/flat-finance/128/bank-icon.png"));

                    greating.Type = "message";
                    greating.Attachments = new List<Attachment>();


                    HeroCard plCard = new HeroCard()
                    {
                        Title = "Hello, Welcome to Contoso Bank",
                        Text = "Check the exchange rate for any currency by entering the 3-digit currency code.\n\n Set your base currency, try 'Set base to NZD'. :)",
                        Images = cardImages

                    };

                    Attachment plAttachment = plCard.ToAttachment();
                    greating.Attachments.Add(plAttachment);

                }
                



                //if user type "clear"
                if (userMessage.ToLower().Contains("clear"))
                {
                    endOutput = "User data cleared";
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    isrequest = false;
                    isgreeting = false;
                }



                //if user type a 3-digit code
                if (userMessage.Length == 3 & currenList.Contains(userMessage.ToLower()))
                {
                    endOutput = "";
                    isrequest = true;
                    isgreeting = false;
                }
                else if (userMessage.Length == 3 & !currenList.Contains(userMessage.ToLower()))
                {
                    endOutput = "Sorry, I'm afraid I don't understand this currency code:(";
                    isrequest = false;
                    isgreeting = false;
                }



                //user message like "set base to NZD"
                if (userMessage.Length > 11 & userMessage.ToLower().Contains("set base"))
                {
                    endOutput = "";
                    if (userMessage.ToLower().Substring(0, userMessage.Length - 3).Equals("set base to "))
                    {
                        string baseRate = userMessage.Substring(userMessage.Length - 3);
                        if (currenList.Contains(baseRate)) {
                            userData.SetProperty<string>("BaseRate", baseRate);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            endOutput = "Base currency is set to " + baseRate + ". You can now check exchange rate for base currency by 'Base currency'.";

                        } else
                        {
                            endOutput = ("Sorry, I'm afraid I don't understand this currency code:(");

                        }

                    }
                    else
                    {
                        endOutput = "Base currency not assigned, please set base currency, for example 'Set base to NZD'.";

                    }
                    isrequest = false;
                    isgreeting = false;
                }



                if (userMessage.ToLower().Equals("base currency"))
                {
                    endOutput = "";
                    string baseRate = userData.GetProperty<string>("BaseRate");
                    if (baseRate == null)
                    {
                        endOutput = "Base currency not assigned, please set base currency, for example 'Set base to NZD'";
                        isrequest = false;
                        isgreeting = false;
                    }
                    else
                    {
                        activity.Text = baseRate;
                        isrequest = true;
                        isgreeting = false;
                    }

                }

                if (userMessage.ToLower().Contains("tran"))
                {

                    string message = "";
                    message = userMessage.ToLower().Replace(" ", "%20");
                    string luisURL = "https://api.projectoxford.ai/luis/v2.0/apps/ec611017-3246-4647-acbc-d96e94d34ea4?subscription-key=aa138cfe431f40dea3460b06fd713712&q=" + message + "&timezoneOffset=12.0";

                    endOutput = "";
                    string temp = "";
                    string luisReply = await client.GetStringAsync(new Uri(luisURL));
                    Microsoft.Bot.Builder.Luis.Models.LuisResult luisresult = JsonConvert.DeserializeObject<Microsoft.Bot.Builder.Luis.Models.LuisResult>(luisReply);

                    if (luisresult.Entities.Count >= 2)
                    {
                        List<Timeline> timelines = await AzureManager.AzureManagerInstance.GetTimelines();
                        
                        string month = "";
                        foreach (var e in luisresult.Entities)
                        {
                            if (e.Type != "transaction")
                            {
                                month = e.Entity;

                            }
                        }
                        foreach (Timeline t in timelines)
                        {
                            string tranMonth = t.Date.ToLower();
                            if ((month.Contains(tranMonth.Substring(0, 3))))
                            {
                                temp += t.Date + ", " + t.Transaction + "\n\n";
                            }
                        }
                        if (temp == "")
                        {
                            endOutput = "No transaction found.";
                        }
                        else
                        {
                            endOutput = temp;
                        }

                    }
                    else
                    {
                        endOutput = "Oops, I'm afraid I don't understand, please try again.";
                    }
                    isrequest = true;
                    isgreeting = false;
                }



                if (isrequest & activity.Text.ToLower().Contains("show") & activity.Text.ToLower().Contains("base"))
                {
                    string basicRate = await client.GetStringAsync(new Uri("http://api.fixer.io/latest"));
                    rootObject = JsonConvert.DeserializeObject<ExchangeObject.RootObject>(basicRate);
                    endOutput = $"Base currency is {rootObject.@base}, at {activity.Timestamp}\n\nExchange rate:\n\n{String.Join("\n\n", getRates(rootObject))}";
                    //isrequest = true;
                    isgreeting = false;
                }

                else if (isrequest & activity.Text.Length == 3)
                {
                    string specifiedRate = await client.GetStringAsync(new Uri("http://api.fixer.io/latest?base=" + activity.Text));
                    rootObject = JsonConvert.DeserializeObject<ExchangeObject.RootObject>(specifiedRate);
                    endOutput = $"Base currency is {rootObject.@base}, at {activity.Timestamp}\n\nExchange rate:\n\n{String.Join("\n\n", getRates(rootObject))}";
                    isgreeting = false;
                }

                /////////////////////////////////////////////////////////////////
                if (isgreeting)
                {
                    await connector.Conversations.SendToConversationAsync(greating);
                }
                else if(isrequest)
                {
                    Activity infoReply = activity.CreateReply(endOutput);
                    await connector.Conversations.ReplyToActivityAsync(infoReply);
                }else
                {
                    Activity infoReply = activity.CreateReply(endOutput);
                    await connector.Conversations.ReplyToActivityAsync(infoReply);
                }



            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            { 
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}