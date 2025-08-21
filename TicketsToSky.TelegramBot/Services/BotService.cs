using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TicketsToSky.TelegramBot.Models;
using TicketsToSky.TelegramBot.Services;

namespace TicketsToSky.TelegramBot.Services
{
    public class BotService(
        ITelegramBotService botService,
        IAirportService airportService,
        ISubscriptionService subscriptionService,
        UserStateStorageService userStateStorageService,
        SubscriptionStorageService subscriptionStorageService) : IHostedService, IUpdateHandler
    {
        private readonly ITelegramBotService _botService = botService;
        private readonly IAirportService _airportService = airportService;
        private readonly ISubscriptionService _subscriptionService = subscriptionService;
        private readonly UserStateStorageService _userStateStorageService = userStateStorageService;
        private readonly SubscriptionStorageService _subscriptionStorageService = subscriptionStorageService;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _botService.Client.StartReceiving(this, cancellationToken: cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message?.Text != null)
            {
                await HandleMessageAsync(botClient, update.Message, cancellationToken);
            }
            else if (update.CallbackQuery != null)
            {
                await HandleCallbackQueryAsync(botClient, update.CallbackQuery, cancellationToken);
            }
        }

        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Polling error: {exception.Message}");
            return Task.CompletedTask;
        }

        private async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var text = message.Text?.Trim() ?? string.Empty;

            var state = await _userStateStorageService.GetStateAsync(chatId) ?? new SubscriptionState();

            switch (text)
            {
                case "/start":
                    await SendMainMenuAsync(botClient, chatId, cancellationToken);
                    break;
                case "Назад":
                    await HandleBackStepAsync(botClient, chatId, state, cancellationToken);
                    break;
                default:
                    switch (state.CurrentStep)
                    {
                        case SubscriptionStep.WaitingForDepartureCity:
                            state.DepartureSearchTerm = text;
                            await SendAirportOptionsAsync(botClient, chatId, text, true, cancellationToken, withBack: true);
                            break;
                        case SubscriptionStep.WaitingForArrivalCity:
                            state.ArrivalSearchTerm = text;
                            await SendAirportOptionsAsync(botClient, chatId, text, false, cancellationToken, withBack: true);
                            break;
                        case SubscriptionStep.WaitingForMaxPrice:
                            if (int.TryParse(text, out var maxPrice))
                            {
                                state.Subscription.MaxPrice = maxPrice;
                                state.CurrentStep = SubscriptionStep.WaitingForMaxTransfers;
                                await SendMaxTransfersOptionsAsync(botClient, chatId, cancellationToken: cancellationToken);
                            }
                            else
                            {
                                await botClient.SendMessage(chatId, "Пожалуйста, введите число для максимальной цены.", replyMarkup: GetBackReplyKeyboard(), cancellationToken: cancellationToken);
                            }
                            break;
                        case SubscriptionStep.WaitingForMinBaggage:
                            if (int.TryParse(text, out var minBaggage))
                            {
                                state.Subscription.MinBaggageAmount = minBaggage;
                                state.CurrentStep = SubscriptionStep.WaitingForMinHandbags;
                                await SendMinHandbagsOptionsAsync(botClient, chatId, cancellationToken);
                            }
                            else
                            {
                                await botClient.SendMessage(chatId, "Пожалуйста, введите число для минимального количества багажа.", replyMarkup: GetBackReplyKeyboard(), cancellationToken: cancellationToken);
                            }
                            break;
                        case SubscriptionStep.WaitingForMinHandbags:
                            if (int.TryParse(text, out var minHandbags))
                            {
                                state.Subscription.MinHandbagsAmount = minHandbags;
                                state.Subscription.ChatId = chatId;
                                // Сохраняем подписку в локальную БД
                                var entity = new SubscriptionEntity
                                {
                                    Id = string.IsNullOrEmpty(state.Subscription.Id) ? Guid.NewGuid().ToString() : state.Subscription.Id,
                                    ChatId = state.Subscription.ChatId,
                                    DepartureAirport = state.Subscription.DepartureAirport,
                                    ArrivalAirport = state.Subscription.ArrivalAirport,
                                    DepartureDate = state.Subscription.DepartureDate,
                                    MaxPrice = state.Subscription.MaxPrice,
                                    MaxTransfersCount = state.Subscription.MaxTransfersCount,
                                    MinBaggageAmount = state.Subscription.MinBaggageAmount,
                                    MinHandbagsAmount = state.Subscription.MinHandbagsAmount,
                                    CreatedAt = DateTime.UtcNow
                                };
                                await _subscriptionStorageService.AddOrUpdateAsync(entity);
                                await botClient.SendMessage(chatId, "Подписка успешно создана!", replyMarkup: GetMainMenuKeyboard(), cancellationToken: cancellationToken);
                                state.Reset();
                            }
                            else
                            {
                                await botClient.SendMessage(chatId, "Пожалуйста, введите число для минимального количества ручной клади.", replyMarkup: GetBackReplyKeyboard(), cancellationToken: cancellationToken);
                            }
                            break;
                    }
                    break;
            }

            // Сохраняем состояние пользователя после обработки сообщения
            await _userStateStorageService.SaveStateAsync(chatId, state);
        }
        // Обработка возврата на предыдущий этап
        private async Task HandleBackStepAsync(ITelegramBotClient botClient, long chatId, SubscriptionState state, CancellationToken cancellationToken)
        {
            switch (state.CurrentStep)
            {
                case SubscriptionStep.WaitingForArrivalCity:
                    state.CurrentStep = SubscriptionStep.WaitingForDepartureCity;
                    await botClient.SendMessage(chatId, "Введите город или аэропорт отправления:", replyMarkup: GetBackReplyKeyboard(), cancellationToken: cancellationToken);
                    break;
                case SubscriptionStep.WaitingForDepartureDate:
                    state.CurrentStep = SubscriptionStep.WaitingForArrivalCity;
                    await botClient.SendMessage(chatId, "Введите город или аэропорт назначения:", replyMarkup: GetBackReplyKeyboard(), cancellationToken: cancellationToken);
                    break;
                case SubscriptionStep.WaitingForMaxPrice:
                    state.CurrentStep = SubscriptionStep.WaitingForDepartureDate;
                    await SendCalendarAsync(botClient, chatId, 0, DateTime.Now, cancellationToken);
                    break;
                case SubscriptionStep.WaitingForMaxTransfers:
                    state.CurrentStep = SubscriptionStep.WaitingForMaxPrice;
                    await botClient.SendMessage(chatId, "Введите максимальную цену:", replyMarkup: GetBackReplyKeyboard(), cancellationToken: cancellationToken);
                    break;
                case SubscriptionStep.WaitingForMinBaggage:
                    state.CurrentStep = SubscriptionStep.WaitingForMaxTransfers;
                    await SendMaxTransfersOptionsAsync(botClient, chatId, cancellationToken: cancellationToken);
                    break;
                case SubscriptionStep.WaitingForMinHandbags:
                    state.CurrentStep = SubscriptionStep.WaitingForMinBaggage;
                    await botClient.SendMessage(chatId, "Введите минимальное количество багажа:", replyMarkup: GetBackReplyKeyboard(), cancellationToken: cancellationToken);
                    break;
                default:
                    await SendMainMenuAsync(botClient, chatId, cancellationToken);
                    break;
            }
        }

        private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var chatId = callbackQuery.Message!.Chat.Id;
            var data = callbackQuery.Data ?? string.Empty;
            var messageId = callbackQuery.Message.MessageId;

            var state = await _userStateStorageService.GetStateAsync(chatId) ?? new SubscriptionState();
            // Сохраняем состояние пользователя после обработки callback
            await _userStateStorageService.SaveStateAsync(chatId, state);

            // Обработка кнопки "Назад" для этапов выбора города
            if (data == "back_to_start")
            {
                state.CurrentStep = SubscriptionStep.None;
                await SendMainMenuAsync(botClient, chatId, cancellationToken);
                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
                return;
            }
            else if (data == "back_to_departure")
            {
                state.CurrentStep = SubscriptionStep.WaitingForDepartureCity;
                await botClient.SendMessage(chatId, "Введите город или аэропорт отправления:", replyMarkup: GetBackKeyboard("back_to_start"), cancellationToken: cancellationToken);
                await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
                return;
            }

            if (data.StartsWith("departure_"))
            {
                if (data == "departure_none")
                {
                    state.CurrentStep = SubscriptionStep.WaitingForDepartureCity;
                    await botClient.EditMessageText(chatId, messageId, "Введите город или аэропорт отправления:", replyMarkup: GetBackKeyboard("back_to_start"), cancellationToken: cancellationToken);
                }
                else
                {
                    state.Subscription.DepartureAirport = data.Replace("departure_", "");
                    state.CurrentStep = SubscriptionStep.WaitingForArrivalCity;
                    await botClient.SendMessage(chatId, "Введите город или аэропорт назначения:", replyMarkup: GetBackKeyboard("back_to_departure"), cancellationToken: cancellationToken);
                }
            }
            else if (data.StartsWith("arrival_"))
            {
                if (data == "arrival_none")
                {
                    state.CurrentStep = SubscriptionStep.WaitingForArrivalCity;
                    await botClient.EditMessageText(chatId, messageId, "Введите город или аэропорт назначения:", replyMarkup: GetBackKeyboard("back_to_departure"), cancellationToken: cancellationToken);
                }
                else
                {
                    state.Subscription.ArrivalAirport = data.Replace("arrival_", "");
                    state.CurrentStep = SubscriptionStep.WaitingForDepartureDate;
                    await SendCalendarAsync(botClient, chatId, 0, DateTime.Now, cancellationToken);
                }
            }
            else if (data.StartsWith("date_"))
            {
                if (data == "date_back")
                {
                    state.CurrentStep = SubscriptionStep.WaitingForArrivalCity;
                    await botClient.SendMessage(chatId, "Введите город или аэропорт назначения:", replyMarkup: GetBackKeyboard("back_to_departure"), cancellationToken: cancellationToken);
                }
                else if (data.StartsWith("date_month_"))
                {
                    var newMonth = DateTime.Parse(data.Replace("date_month_", ""));
                    await SendCalendarAsync(botClient, chatId, messageId, newMonth, cancellationToken);
                }
                else
                {
                    state.Subscription.DepartureDate = data.Replace("date_", "");
                    state.CurrentStep = SubscriptionStep.WaitingForMaxPrice;
                    await botClient.SendMessage(chatId, "Введите максимальную цену:", replyMarkup: GetBackKeyboard("back_to_departure_date"), cancellationToken: cancellationToken);
                }
            }
            else if (data == "max_transfers_back")
            {
                state.CurrentStep = SubscriptionStep.WaitingForDepartureDate;
                await SendCalendarAsync(botClient, chatId, 0, DateTime.Parse(state.Subscription.DepartureDate), cancellationToken);
            }
            else if (data.StartsWith("transfers_"))
            {
                state.Subscription.MaxTransfersCount = int.Parse(data.Replace("transfers_", ""));
                state.CurrentStep = SubscriptionStep.WaitingForMinBaggage;
                await botClient.SendMessage(chatId, "Введите минимальное количество багажа:", replyMarkup: GetBackKeyboard("back_to_max_transfers"), cancellationToken: cancellationToken);
            }
            else if (data == "baggage_back")
            {
                state.CurrentStep = SubscriptionStep.WaitingForMaxTransfers;
                await SendMaxTransfersOptionsAsync(botClient, chatId, 0, cancellationToken);
            }
            else if (data == "handbags_back")
            {
                state.CurrentStep = SubscriptionStep.WaitingForMinBaggage;
                await botClient.SendMessage(chatId, "Введите минимальное количество багажа:", replyMarkup: GetBackKeyboard("back_to_max_transfers"), cancellationToken: cancellationToken);
            }
            else if (data == "create_subscription")
            {
                state.Reset();
                state.CurrentStep = SubscriptionStep.WaitingForDepartureCity;
                await botClient.SendMessage(chatId, "Введите город или аэропорт отправления:", cancellationToken: cancellationToken, replyMarkup: GetBackKeyboard("back_to_start"));
            }
            else if (data == "edit_subscription")
            {
                await SendSubscriptionListAsync(botClient, chatId, true, cancellationToken);
            }
            else if (data.StartsWith("edit_"))
            {
                var subscriptionId = data.Replace("edit_", "");
                var entity = await _subscriptionStorageService.GetSubscriptionAsync(subscriptionId);
                if (entity != null)
                {
                    state.Subscription = new Subscription
                    {
                        Id = entity.Id,
                        ChatId = entity.ChatId,
                        DepartureAirport = entity.DepartureAirport,
                        ArrivalAirport = entity.ArrivalAirport,
                        DepartureDate = entity.DepartureDate,
                        MaxPrice = entity.MaxPrice ?? 0,
                        MaxTransfersCount = entity.MaxTransfersCount ?? 0,
                        MinBaggageAmount = entity.MinBaggageAmount ?? 0,
                        MinHandbagsAmount = entity.MinHandbagsAmount ?? 0
                    };
                }
                await SendEditOptionsAsync(botClient, chatId, cancellationToken);
            }
            else if (data.StartsWith("edit_field_"))
            {
                var field = data.Replace("edit_field_", "");
                state.EditField = field;
                switch (field)
                {
                    case "DepartureAirport":
                        state.CurrentStep = SubscriptionStep.WaitingForDepartureCity;
                        await botClient.SendMessage(chatId, "Введите город или аэропорт отправления:", cancellationToken: cancellationToken);
                        break;
                    case "ArrivalAirport":
                        state.CurrentStep = SubscriptionStep.WaitingForArrivalCity;
                        await botClient.SendMessage(chatId, "Введите город или аэропорт назначения:", cancellationToken: cancellationToken);
                        break;
                    case "DepartureDate":
                        state.CurrentStep = SubscriptionStep.WaitingForDepartureDate;
                        await SendCalendarAsync(botClient, chatId, messageId, DateTime.Parse(state.Subscription.DepartureDate), cancellationToken);
                        break;
                    case "MaxPrice":
                        state.CurrentStep = SubscriptionStep.WaitingForMaxPrice;
                        await botClient.SendMessage(chatId, "Введите максимальную цену:", cancellationToken: cancellationToken);
                        break;
                    case "MaxTransfersCount":
                        state.CurrentStep = SubscriptionStep.WaitingForMaxTransfers;
                        await SendMaxTransfersOptionsAsync(botClient, chatId, cancellationToken: cancellationToken);
                        break;
                    case "MinBaggageAmount":
                        state.CurrentStep = SubscriptionStep.WaitingForMinBaggage;
                        await botClient.SendMessage(chatId, "Введите минимальное количество багажа:", cancellationToken: cancellationToken);
                        break;
                    case "MinHandbagsAmount":
                        state.CurrentStep = SubscriptionStep.WaitingForMinHandbags;
                        await botClient.SendMessage(chatId, "Введите минимальное количество ручной клади:", cancellationToken: cancellationToken);
                        break;
                }
            }
            else if (data == "delete_subscription")
            {
                await SendSubscriptionListAsync(botClient, chatId, false, cancellationToken);
            }
            else if (data.StartsWith("delete_"))
            {
                var subscriptionId = data.Replace("delete_", "");
                await _subscriptionStorageService.DeleteAsync(subscriptionId);
                await botClient.SendMessage(chatId, "Подписка удалена!", replyMarkup: GetMainMenuKeyboard(), cancellationToken: cancellationToken);
            }

            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
        }

        private async Task SendMainMenuAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var keyboard = GetMainMenuKeyboard();
            await botClient.SendMessage(chatId, "Выберите действие:", replyMarkup: keyboard, cancellationToken: cancellationToken);
        }

        private InlineKeyboardMarkup GetMainMenuKeyboard()
        {
            return new InlineKeyboardMarkup(
            [
                [InlineKeyboardButton.WithCallbackData("Создать подписку", "create_subscription")],
                [InlineKeyboardButton.WithCallbackData("Изменить подписку", "edit_subscription")],
                [InlineKeyboardButton.WithCallbackData("Удалить подписку", "delete_subscription")]
            ]);
        }

        private async Task SendAirportOptionsAsync(ITelegramBotClient botClient, long chatId, string term, bool isDeparture, CancellationToken cancellationToken, bool withBack = false)
        {
            var airports = await _airportService.SearchAirportsAsync(term);
            var prefix = isDeparture ? "departure_" : "arrival_";
            var buttons = airports.Select(a =>
            {
                var displayText = a.Type == "city" ? $"{a.Name} ({a.CountryName})" : $"{a.Name} ({a.CityName}, {a.CountryName})";
                return InlineKeyboardButton.WithCallbackData(displayText, $"{prefix}{a.Code}");
            }).ToList();

            buttons.Add(InlineKeyboardButton.WithCallbackData("Нет моего варианта", $"{prefix}none"));
            if (withBack) buttons.Add(InlineKeyboardButton.WithCallbackData("Назад", isDeparture ? "back_to_start" : "back_to_departure"));

            var keyboard = new InlineKeyboardMarkup(buttons.Chunk(1).ToArray());
            var message = isDeparture ? "Выберите город или аэропорт отправления:" : "Выберите город или аэропорт назначения:";
            await botClient.SendMessage(chatId, message, replyMarkup: keyboard, cancellationToken: cancellationToken);
        }

        // Клавиатура для возврата назад (ReplyKeyboard)
        private InlineKeyboardMarkup GetBackReplyKeyboard() => new([[InlineKeyboardButton.WithCallbackData("Назад", "")]]);

        private async Task SendCalendarAsync(ITelegramBotClient botClient, long chatId, int messageId, DateTime date, CancellationToken cancellationToken)
        {
            var keyboard = GenerateCalendarKeyboard(date);
            var messageText = $"Выберите дату вылета ({date:MMMM yyyy}):";
            if (messageId == 0)
            {
                await botClient.SendMessage(chatId, messageText, replyMarkup: keyboard, cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.EditMessageText(chatId, messageId, messageText, replyMarkup: keyboard, cancellationToken: cancellationToken);
            }
        }

        private InlineKeyboardMarkup GenerateCalendarKeyboard(DateTime date)
        {
            var buttons = new List<InlineKeyboardButton[]>();
            var firstDay = new DateTime(date.Year, date.Month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            buttons.Add(
            [
                InlineKeyboardButton.WithCallbackData("◄", $"date_month_{firstDay.AddMonths(-1):yyyy-MM-dd}"),
                InlineKeyboardButton.WithCallbackData($"{date:MMMM yyyy}", "noop"),
                InlineKeyboardButton.WithCallbackData("►", $"date_month_{firstDay.AddMonths(1):yyyy-MM-dd}")
            ]);

            var daysOfWeek = new[] { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
            buttons.Add(daysOfWeek.Select(d => InlineKeyboardButton.WithCallbackData(d, "noop")).ToArray());

            var currentDate = firstDay;
            var week = new List<InlineKeyboardButton>();

            int dayOfWeekIndex = (int)firstDay.DayOfWeek == 0 ? 6 : (int)firstDay.DayOfWeek - 1;
            for (int i = 0; i < dayOfWeekIndex; i++)
            {
                week.Add(InlineKeyboardButton.WithCallbackData(" ", "noop"));
            }

            while (currentDate <= lastDay)
            {
                week.Add(InlineKeyboardButton.WithCallbackData(currentDate.Day.ToString(), $"date_{currentDate:yyyy-MM-dd}"));
                if (week.Count == 7)
                {
                    buttons.Add([.. week]);
                    week = [];
                }
                currentDate = currentDate.AddDays(1);
            }

            // Добавляем последний ряд только если есть хотя бы одна дата в нем
            if (week.Any(b => b.Text.Trim() != "" && b.CallbackData != "noop"))
            {
                while (week.Count < 7)
                {
                    week.Add(InlineKeyboardButton.WithCallbackData(" ", "noop"));
                }
                buttons.Add([.. week]);
            }

            buttons.Add([InlineKeyboardButton.WithCallbackData("Назад", "date_back")]);

            return new InlineKeyboardMarkup(buttons);
        }

        private async Task SendMaxTransfersOptionsAsync(ITelegramBotClient botClient, long chatId, int messageId = 0, CancellationToken cancellationToken = default)
        {
            var keyboard = new InlineKeyboardMarkup(
            [
                [
                    InlineKeyboardButton.WithCallbackData("0", "transfers_0"),
                    InlineKeyboardButton.WithCallbackData("1", "transfers_1"),
                    InlineKeyboardButton.WithCallbackData("2", "transfers_2")
                ],
                [InlineKeyboardButton.WithCallbackData("Назад", "max_transfers_back")]
            ]);

            var message = "Выберите максимальное количество пересадок:";
            if (messageId == 0)
            {
                await botClient.SendMessage(chatId, message, replyMarkup: keyboard, cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.EditMessageText(chatId, messageId, message, replyMarkup: keyboard, cancellationToken: cancellationToken);
            }
        }

        private async Task SendMinHandbagsOptionsAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var keyboard = GetBackKeyboard("handbags_back");
            await botClient.SendMessage(chatId, "Введите минимальное количество ручной клади:", replyMarkup: keyboard, cancellationToken: cancellationToken);
        }

        private InlineKeyboardMarkup GetBackKeyboard(string callbackData)
        {
            return new InlineKeyboardMarkup(new[] { InlineKeyboardButton.WithCallbackData("Назад", callbackData) });
        }

        private async Task SendSubscriptionListAsync(ITelegramBotClient botClient, long chatId, bool isEdit, CancellationToken cancellationToken)
        {
            var subscriptions = await _subscriptionStorageService.GetUserSubscriptionsAsync(chatId);
            if (!subscriptions.Any())
            {
                await botClient.SendMessage(chatId, "У вас нет активных подписок.", replyMarkup: GetMainMenuKeyboard(), cancellationToken: cancellationToken);
                return;
            }

            var buttons = subscriptions.Select(s => InlineKeyboardButton.WithCallbackData(
                $"{s.DepartureAirport} → {s.ArrivalAirport} ({s.DepartureDate})",
                isEdit ? $"edit_{s.Id}" : $"delete_{s.Id}")).ToList();

            var keyboard = new InlineKeyboardMarkup(buttons.Chunk(1).ToArray());
            var message = isEdit ? "Выберите подписку для изменения:" : "Выберите подписку для удаления:";
            await botClient.SendMessage(chatId, message, replyMarkup: keyboard, cancellationToken: cancellationToken);
        }

        private async Task SendEditOptionsAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var keyboard = new InlineKeyboardMarkup(
            [
                [InlineKeyboardButton.WithCallbackData("Аэропорт отправления", "edit_field_DepartureAirport")],
                [InlineKeyboardButton.WithCallbackData("Аэропорт назначения", "edit_field_ArrivalAirport")],
                [InlineKeyboardButton.WithCallbackData("Дата вылета", "edit_field_DepartureDate")],
                [InlineKeyboardButton.WithCallbackData("Максимальная цена", "edit_field_MaxPrice")],
                [InlineKeyboardButton.WithCallbackData("Количество пересадок", "edit_field_MaxTransfersCount")],
                [InlineKeyboardButton.WithCallbackData("Количество багажа", "edit_field_MinBaggageAmount")],
                [InlineKeyboardButton.WithCallbackData("Количество ручной клади", "edit_field_MinHandbagsAmount")]
            ]);

            await botClient.SendMessage(chatId, "Выберите параметр для изменения:", replyMarkup: keyboard, cancellationToken: cancellationToken);
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            if(exception is Telegram.Bot.Exceptions.RequestException) return Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            Console.WriteLine(exception);
            return Task.CompletedTask;
        }
    }

    public class SubscriptionState
    {
        public Subscription Subscription { get; set; } = new Subscription();
        public SubscriptionStep CurrentStep { get; set; }
        public string? DepartureSearchTerm { get; set; }
        public string? ArrivalSearchTerm { get; set; }
        public string? EditField { get; set; }

        public void Reset()
        {
            Subscription = new Subscription();
            CurrentStep = SubscriptionStep.None;
            DepartureSearchTerm = null;
            ArrivalSearchTerm = null;
            EditField = null;
        }
    }

    public enum SubscriptionStep
    {
        None,
        WaitingForDepartureCity,
        WaitingForArrivalCity,
        WaitingForDepartureDate,
        WaitingForMaxPrice,
        WaitingForMaxTransfers,
        WaitingForMinBaggage,
        WaitingForMinHandbags
    }
}