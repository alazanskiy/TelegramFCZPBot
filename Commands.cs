using Telegram.Bot;
using Telegram.Bot.Types;
using System;

namespace AlazanskiyBot
{
    public abstract class BotCommand
    {
        public abstract string Name { get;}
        public abstract void Execute(Telegram.Bot.Types.Message message, TelegramBotClient cli);
        public bool Contains(string command)
        {
            return command.Contains(this.Name); //&& command.Contains("botname"); //для чатов
        }
    }

    public class HelloCommand : BotCommand
    {
        public override string Name => "hello";

        public override async void Execute(Telegram.Bot.Types.Message message, TelegramBotClient cli)
        {
            var chatID = message.Chat.Id;
            var messageID = message.MessageId;

            await cli.SendTextMessageAsync(chatID, "Зиганчики", replyToMessageId:messageID); //третий параметр - если нужно цитирование
        }
    }
}