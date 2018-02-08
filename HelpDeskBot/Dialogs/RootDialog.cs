using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using HelpDeskBot.Util;
using System.Collections.Generic;
using AdaptiveCards;

namespace HelpDeskBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private string category;
        private string severity; // 厳しさ
        private string description;

        private bool isUserSatisfied;
        private string usersVoice; // ご意見をお聞かせください。

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            await context.PostAsync("Help Desk Bot です。サポートデスク受付チケットの発行を行います。");

            PromptDialog.Text(
                context: context
                , resume: this.DescriptionMessageReceivedAsync
                , prompt: "どんなことにお困りですか？"
            );
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
                    var message = context.MakeMessage();
                    message.Attachments = new List<Attachment>
                    {
                        new Attachment
                        {
                            ContentType = "application/vnd.microsoft.card.adaptive",
                            Content = CreateCard(ticketId, this.category, this.severity, this.description)
                        },
                    };
                    await context.PostAsync(message);
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
            PromptDialog.Confirm(context, this.HearingUsersVoice, "この bot の応対にご満足いただけましたか？？");
        }
        private async Task HearingUsersVoice(IDialogContext context, IAwaitable<bool> isUserSatisfied)
        {
            this.isUserSatisfied = await isUserSatisfied;

            var response = this.isUserSatisfied
                ? "ご満足いただけたようで嬉しいです。"
                : "至らずに申し訳ございません。精進します。";

            PromptDialog.Text(
                context: context
                , resume: this.HeardUsersVoice
                , prompt: response + "\n\nご意見をお聞かせください。"
            );
        }
        public async Task HeardUsersVoice(IDialogContext context, IAwaitable<string> usersVoice)
        {
            this.usersVoice = await usersVoice;
            // TODO: ここで result (はい/いいえ)とご意見を送る処理

            PromptDialog.Text(context, null, $"ご意見ありがとうございました。\n\n頂いたメッセージ: {this.usersVoice}");
        }



        private AdaptiveCard CreateCard(int ticketId, string category, string severity, string description)
        {
            var card = new AdaptiveCard();

            var headerBlock = new TextBlock()
            {
                Text = $"Tucket #{ticketId}",
                Weight = TextWeight.Bolder,
                Size = TextSize.Large,
                Speak = $"承知しました。チケット No.{ticketId}でサポートチケットを発行しました。担当者からの連絡をお待ちください。",
            };

            var columnBlock = new ColumnSet()
            {
                Separation = SeparationStyle.Strong,
                Columns = new List<Column>
                {
                    new Column
                    {
                        Size = "1",
                        Items = new List<CardElement>
                        {
                            new FactSet
                            {
                                Facts = new List<AdaptiveCards.Fact>
                                {
                                    new AdaptiveCards.Fact(title: "Severity: ", value: severity),
                                    new AdaptiveCards.Fact(title: "Category: ", value: category),
                                }
                            }
                        },
                    },
                    new Column
                    {
                        Size = "auto",
                        Items = new List<CardElement>
                        {
                            new Image
                            {
                                Url = "https://pbs.twimg.com/profile_images/947463663056650240/pvAbP-BI_400x400.jpg",
                                Size = ImageSize.Small,
                                HorizontalAlignment = HorizontalAlignment.Right,
                            }
                        }
                    },
                },
            };

            var descriptionBlock = new TextBlock
            {
                Text = description,
                Wrap = true,
            };

            card.Body.Add(item: headerBlock);
            card.Body.Add(item: columnBlock);
            card.Body.Add(item: descriptionBlock);

            return card;
        }
    }
}