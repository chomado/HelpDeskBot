using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using HelpDeskBot.Util;

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
            var severities = new [] { "high", "normal", "low" };

            PromptDialog.Choice(
                context: context
                , resume: this.SeverityMessageRecievedAsync
                , options: severities
                , prompt: "この問題の重要度を入力してください"
            );
        }

        public async Task SeverityMessageRecievedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            this.severity = await argument;
            const string prompt = "この問題のカテゴリーを以下から選んで入力してください\n\n"
                + "software, hardware, networking, security, other";
            PromptDialog.Text(context: context, resume: this.CategoryMessageReceivedAsync, prompt: prompt);
        }

        private async Task CategoryMessageReceivedAsync(IDialogContext context, IAwaitable<string> result)
        {
            this.category = await result;
            var text = "承知しました。\n\n"
                + $"重要度: \"{this.severity}\"、カテゴリー: \"{this.category}\" "
                + "でサポートチケットを発行します。\n\n"
                + $"詳細: \"{this.description}\" \n\n"
                + "以上の内容でよろしいでしょうか？";

            PromptDialog.Confirm(context: context, resume: this.IssueConformedMessageReceivedAsync, prompt: text);
        }

        private async Task IssueConformedMessageReceivedAsync(IDialogContext context, IAwaitable<bool> result)
        {
            var confirmed = await result;

            if (confirmed)
            {
                var api = new TicketAPIClient();
                var ticketId = await api.PostTicketAsync(this.category, this.severity, this.description);

                if (ticketId != -1)
                {
                    await context.PostAsync($"承知しました。チケットNo.{ticketId}でサポートチケットを発行しました。");
                }
                else
                {
                    await context.PostAsync("サポートチケット発行中にエラーが発生しました。恐れ入りますが、後ほど再度お試しください。");
                }
            }
            else
            {
                await context.PostAsync("サポートチケットの発行を中止しました。最初からやり直してください。");
            }
            context.Done<object>(null);
        }
    }
}