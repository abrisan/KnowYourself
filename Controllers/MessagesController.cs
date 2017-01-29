using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using KnowYourself.Controllers;
 
namespace KnowYourself
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        /// 

        DBConnection conn = new DBConnection();
        List<String> answers = SetupAnswerStrings();

        public static List<String> SetupAnswerStrings()
        {
            List<String> ans = new List<String>();

            ans.Add("Hmmm...");
            ans.Add("I understand");
            ans.Add("I feel the same");
            ans.Add("How does that make you feel?");


            return ans;
        }

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {



                
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                StateClient state = activity.GetStateClient();

                BotData currentState = await state.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);

                const int talkLimitRecom = 3;



                /*
                 * We define the following state properties:
                 *  
                 *  
                 *  awaitDiseases 
                 *  talkAboutHealth
                 *  answerQuestion (associate with the questionID)
                 *  
                 * /
               
                */

               
                if (currentState.GetProperty<bool>("awaitsDiscussion"))
                {
                    var tempMes = activity.Text;
                    if(tempMes.Equals("Yes") || tempMes.Equals("Yeah") || tempMes.Equals("Sure"))
                    {
                        currentState.SetProperty<bool>("awaitsDiscussion", false);
                        await state.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, currentState);
                        Dictionary<int, String> questions = conn.GetQuestionsForUser(activity.From.Id);
                        try
                        {
                            List<int> ids = questions.Keys.OrderBy(i => i).ToList();
                            string question ;
                            bool succ = questions.TryGetValue(ids.First(), out question);
                            currentState.SetProperty<int>("currentQuestionID", ids.First());
                            //currentState.SetProperty<int>("questionListIndex", 0);
                            currentState.SetProperty<bool>("answerQuestion", true);
                            await state.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, currentState);
                            Activity resp = activity.CreateReply(question);
                            await connector.Conversations.ReplyToActivityAsync(resp);
                        }catch(Exception e)
                        {
                            Activity debug2 = activity.CreateReply(e.Source);
                            Activity debug3 = activity.CreateReply(e.Message);
                            await connector.Conversations.ReplyToActivityAsync(debug2);
                            await connector.Conversations.ReplyToActivityAsync(debug3);
                        }
                        
                    }
                    else
                    {
                        currentState.SetProperty<bool>("awaitsDiscussion", false);
                        currentState.SetProperty<bool>("talkAboutLife", true);
                        currentState.SetProperty<int>("factCounter", 0);
                        await state.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, currentState);
                        Activity resp = activity.CreateReply("So what would you like to talk about?");
                        await connector.Conversations.ReplyToActivityAsync(resp);
                    }
                }
                else if (currentState.GetProperty<bool>("awaitDiseases"))
                {
                    String slack_id = activity.From.Id;
                    List<String> diseases = activity.Text.Split(',').ToList();
                    foreach (String disease in diseases)
                    {
                        conn.AddUserDisease(slack_id, disease);
                    }
                    currentState.SetProperty<bool>("awaitDiseases", false);
                    currentState.SetProperty<bool>("awaitsDiscussion", true);
                    await state.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, currentState);
                    Activity resp = activity.CreateReply("Would you like to answer some questions now, or later?");
                    await connector.Conversations.ReplyToActivityAsync(resp);
                }
                else if (currentState.GetProperty<bool>("talkAboutLife"))
                {
                    if(currentState.GetProperty<int>("factCounter") == talkLimitRecom)
                    {
                        currentState.SetProperty<bool>("talkAboutLife", false);
                        currentState.SetProperty<bool>("awaitsDiscussion", true);
                        await state.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, currentState);
                        Activity resp = activity.CreateReply("Do you feel ready to talk about your health?");
                        await connector.Conversations.ReplyToActivityAsync(resp);
                    }
                    else
                    {
                        int ans = new Random().Next(answers.Capacity);
                        int curr = currentState.GetProperty<int>("factCounter") + 1;
                        currentState.SetProperty<int>("factCounter", curr);
                        await state.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, currentState);
                        Activity resp = activity.CreateReply(answers.ElementAt(ans));
                        await connector.Conversations.ReplyToActivityAsync(resp);
                    }
                }
                else if (currentState.GetProperty<bool>("answerQuestion"))
                {
                    conn.StoreAnswerToDB(activity.Text, activity.From.Id, currentState.GetProperty<int>("currentQuestionID"));
                    Dictionary<int, String> questions = conn.GetQuestionsForUser(activity.From.Id);
                    int nextIndex = currentState.GetProperty<int>("questionListIndex") + 1;
                    if(nextIndex > questions.Count)
                    {
                        currentState.SetProperty<bool>("answerQuestion", false);
                        await state.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, currentState);
                        Activity reply = activity.CreateReply("Thanks for taking the time. Talk tomorrow");
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                    else
                    {
                        try
                        {
                            List<int> ids = questions.Keys.OrderBy(i => i).ToList();
                            string question;
                            questions.TryGetValue(ids.ElementAt(nextIndex), out question);
                            currentState.SetProperty<int>("currentQuestionID", ids.ElementAt(nextIndex));
                            currentState.SetProperty<int>("questionListIndex", nextIndex);
                            await state.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, currentState);
                            Activity resp = activity.CreateReply(question);
                            await connector.Conversations.ReplyToActivityAsync(resp);
                        }catch(Exception e)
                        {

                            Activity debug2 = activity.CreateReply(e.Source);
                            Activity debug3 = activity.CreateReply(e.Message);
                            await connector.Conversations.ReplyToActivityAsync(debug2);
                            await connector.Conversations.ReplyToActivityAsync(debug3);
                        }
                        
                    }
                }
                else
                {
                    if (!conn.UserExists(activity.From.Id))
                    {
                        conn.InsertUser(activity.From.Id);
                        Activity welcomeToUs = activity.CreateReply($"Welcome to KnowYourself, {activity.From.Name}");
                        await connector.Conversations.ReplyToActivityAsync(welcomeToUs);
                        Activity setUp = activity.CreateReply(@"In order to start using KnowYourself, please start
                                        by selecting the diseases for which you would like to track your health. 
                                         You will receive a list of diseases from which you can choose. To select,
                                           respond to this message with a comma separated list of diseases. Thanks
                                            for using KnowYourself");
                        await connector.Conversations.ReplyToActivityAsync(setUp);
                        currentState.SetProperty<bool>("awaitDiseases", true);
                        await state.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, currentState);
                        foreach (String disease in conn.GetDiseases())
                        {
                            Activity resp = activity.CreateReply(disease);
                            await connector.Conversations.ReplyToActivityAsync(resp);
                        }
                    }
                    else
                    {
                        Activity welcomeBack = activity.CreateReply($"Welcome back, {activity.From.Name}");
                        await connector.Conversations.ReplyToActivityAsync(welcomeBack);
                        Activity talkAboutHealth = activity.CreateReply($"Would you like to talk about your health, {activity.From.Name}?");
                        currentState.SetProperty<bool>("awaitsDiscussion", true);
                        await state.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, currentState);
                        await connector.Conversations.ReplyToActivityAsync(talkAboutHealth);

                    }

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