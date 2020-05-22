using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot;
using System.Collections.Generic;

namespace AlazanskiyBot
{
    public static class TelegBotHelloAlazanskiy
    {
        [FunctionName("TelegBotHelloAlazanskiy")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            try
            {
                Telegram.Bot.Types.Update upd =  JsonConvert.DeserializeObject<Telegram.Bot.Types.Update>(requestBody);

                TelegramBotClient cli = new TelegramBotClient("876359447:AAG66NQmwN0zsUwWH9wrRoTwpJtXo1WwElI");

                if (upd.Message.Type == Telegram.Bot.Types.Enums.MessageType.Text)
                {

                    log.LogInformation($"{upd.Message.From.FirstName} {upd.Message.From.LastName} [{upd.Message.From.Id}] ({upd.Message.From.Username}): {upd.Message.Text}");

                    QueryHistoryBase qhb = new QueryHistoryBase();
                    string res = await qhb.GetData(upd.Message.Text);

                    var chatID = upd.Message.Chat.Id;
                    var messageID = upd.Message.MessageId;

                    await cli.SendTextMessageAsync(chatID, res); 


                    /*
                    List<BotCommand> cmds = new List<BotCommand>();
                    cmds.Add(new HelloCommand());

                    foreach (BotCommand cmd in cmds)
                    {
                        if (cmd.Contains(upd.Message.Text))
                        {
                            cmd.Execute(upd.Message, cli);
                        }
                    }
                    */
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"Caugth :{ex.Message}");
                log.LogInformation($"Query for exception :{requestBody}");
            }

            return new OkResult();
        }
    }
}
