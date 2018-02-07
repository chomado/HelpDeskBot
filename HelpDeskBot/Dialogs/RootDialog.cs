using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace HelpDeskBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private string category;
        private string severity; // 厳しさ
        private string description;

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            await context.PostAsync("Help Desk Bot です。サポートデスク受付チケットの発行を行います。");

            PromptDialog.Text(context: context, resume: this.DescriptionMessageReceivedAsync, prompt: "どんなことにお困りですか？");
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            // calculate something for us to return
            int length = (activity.Text ?? string.Empty).Length;

            // return our reply to the user
            await context.PostAsync($"You sent {activity.Text} which was {length} characters");

            context.Wait(MessageReceivedAsync);
        }

        public async Task DescriptionMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            this.description = await argument;
            await context.PostAsync($"承知しました。内容は \"{this.description}\" ですね。");
            context.Done<object>(value: null);
        }
    }
}