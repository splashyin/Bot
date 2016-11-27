using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Bot_ApplicationBank.Model;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;

namespace Bot_ApplicationBank
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                ExchangeObject.RootObject rootObject;
                 

                HttpClient client = new HttpClient();

              
                Activity reply;

                string temp = "";

                var dictionary = new Dictionary<string, object> { };

                if (activity.Text.ToLower().Contains("show") & activity.Text.ToLower().Contains("base"))
                {
                    string basicRate = await client.GetStringAsync(new Uri("http://api.fixer.io/latest"));
                    rootObject = JsonConvert.DeserializeObject<ExchangeObject.RootObject>(basicRate);

                    Type myType = rootObject.rates.GetType();
                    IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());

                    List<string> rateList = new List<string>();

                    foreach (PropertyInfo prop in props)
                    {
                        object propValue = prop.GetValue(rootObject.rates, null);
                        dictionary.Add(prop.Name, propValue);
                    }
                    
                    foreach (KeyValuePair<string, object> kvp in dictionary)
                    {
                        temp = string.Format("{0}, {1}", kvp.Key, kvp.Value);
                        rateList.Add(temp);
                    }
                    reply = activity.CreateReply($"At the date: {rootObject.date}, base currency is {rootObject.@base}, rate is\r\n{String.Join(Environment.NewLine +"", rateList)}");
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }

                else if(activity.Text.Length == 3)
                {
                    string specifiedRate = await client.GetStringAsync(new Uri("http://api.fixer.io/latest?base=" + activity.Text));
                    rootObject = JsonConvert.DeserializeObject<ExchangeObject.RootObject>(specifiedRate);
                    Type myType = rootObject.rates.GetType();
                    IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());

                    List<string> rateList = new List<string>();
                   
                    foreach (PropertyInfo prop in props)
                    {
                        object propValue = prop.GetValue(rootObject.rates, null);
                        dictionary.Add(prop.Name, propValue);
                    }

                    foreach (KeyValuePair<string, object> kvp in dictionary)
                    {
                        temp = string.Format("{0}, {1}", kvp.Key, kvp.Value);
                        rateList.Add(temp);
                    }
                    reply = activity.CreateReply($"At the date: {rootObject.date}, base currency is {rootObject.@base}, rate is\r\n{String.Join(Environment.NewLine + "", rateList)}");
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