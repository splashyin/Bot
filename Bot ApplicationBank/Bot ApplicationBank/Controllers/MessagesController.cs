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


                var userMessage = activity.Text;

                string endOutput = "Hello, how can I help you today?";
               

                // calculate something for us to return
                if (userData.GetProperty<bool>("SentGreeting"))
                {
                    endOutput = "Hello again. Do you know now you can check the exchange rate for any currency by entering the 3-digit currency code?" +
                        " You can also set your base currency like this 'Set base to NZD'. Try it out now:)";
                }
                else
                {
                    userData.SetProperty<bool>("SentGreeting", true);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                }

                
                if (userMessage.ToLower().Contains("clear"))
                {
                    endOutput = "User data cleared";
                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                   
                }

                if (userMessage.Length == 3 & currenList.Contains(userMessage))
                {
                    endOutput = "Getting details...";
                }
                else if(userMessage.Length == 3 & !currenList.Contains(userMessage))
                {
                    endOutput = "Sorry, I'm afraid I don't understand this currency code:(";
                }

                //user message like "set base to NZD"
                if (userMessage.Length > 11)
                {
                    if (userMessage.ToLower().Substring(0, userMessage.Length-3).Equals("set base to "))
                    {
                        string baseRate = userMessage.Substring(userMessage.Length-3);
                        if (currenList.Contains(baseRate)) {
                            userData.SetProperty<string>("BaseRate", baseRate);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                            endOutput = "Base currency is set to " + baseRate + ". You can now check exchange rate for base currency by 'Base currency'.";
                            
                        }else
                        {
                            endOutput = ("Sorry, I'm afraid I don't understand this currency code:(");
                           
                        }
                        
                    }
                    else
                    {
                        endOutput = "Base currency not assigned, please set base currency, for example 'Set base to NZD'.";
                    }
                }

                if (userMessage.ToLower().Equals("base currency"))
                {
                    string baseRate = userData.GetProperty<string>("BaseRate");
                    if (baseRate == null)
                    {
                        endOutput = "Base currency not assigned, please set base currency, for example 'Set base to NZD'";
                        
                    }
                    else
                    {
                        activity.Text = baseRate;
                        endOutput = "Getting details for base currency...";
                    }

                }

                // return our reply to the user
                Activity infoReply = activity.CreateReply(endOutput);

                await connector.Conversations.ReplyToActivityAsync(infoReply);

                

                ExchangeObject.RootObject rootObject;
                 
                HttpClient client = new HttpClient();
      
                Activity reply;

                if (activity.Text.ToLower().Contains("show") & activity.Text.ToLower().Contains("base"))
                {
                    string basicRate = await client.GetStringAsync(new Uri("http://api.fixer.io/latest"));
                    rootObject = JsonConvert.DeserializeObject<ExchangeObject.RootObject>(basicRate);                  
                    reply = activity.CreateReply($"At time: {activity.Timestamp}, base currency is {rootObject.@base}, rate is\r\n{String.Join(Environment.NewLine +"", getRates(rootObject))}");
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }

                else if(activity.Text.Length == 3)
                {
                    string specifiedRate = await client.GetStringAsync(new Uri("http://api.fixer.io/latest?base=" + activity.Text));
                    rootObject = JsonConvert.DeserializeObject<ExchangeObject.RootObject>(specifiedRate);  
                    reply = activity.CreateReply($"At time {activity.Timestamp}, base currency is {rootObject.@base}, rate is\r\n{String.Join(Environment.NewLine + "", getRates(rootObject))}");
                    await connector.Conversations.ReplyToActivityAsync(reply);
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