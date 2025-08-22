using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TicketsToSky.TelegramBot.States;
using TicketsToSky.TelegramBot.Models;

namespace TicketsToSky.TelegramBot.Services;

public class BotService(IUserStateService userStateService, IAirportService airportService, ISubscriptionService subscriptionService) : IBotService
{
    private readonly TelegramBotClient _botClient = new("7208238793:AAHucASIYY9F0m6a-Ux7ScYfKBRgQupWDvo");
    private readonly IUserStateService _userStateService = userStateService;
    private readonly IAirportService _airportService = airportService;
    private readonly ISubscriptionService _subscriptionService = subscriptionService;

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Message != null && update.Message.Text != null)
        {
            await HandleMessageAsync(update.Message, cancellationToken);
        }
        else if (update.CallbackQuery != null)
        {
            await HandleCallbackQueryAsync(update.CallbackQuery, cancellationToken);
        }
    }

    private async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var text = message.Text;

        if (text == "/start")
        {
            await _userStateService.ClearStateAsync(chatId);
            await ShowMainMenuAsync(chatId, cancellationToken);
        }
        else
        {
            await HandleStateMachineAsync(chatId, text, cancellationToken);
        }
    }

    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message.Chat.Id;
        var data = callbackQuery.Data;

        if (data == "back")
        {
            var state = await _userStateService.GetStateAsync(chatId);
            if (state.CurrentStep == BotState.DepartureCity || state.CurrentStep == BotState.ArrivalCity)
            {
                await _userStateService.ClearStateAsync(chatId);
                await ShowMainMenuAsync(chatId, cancellationToken);
            }
            else
            {
                await _userStateService.MoveToPreviousStateAsync(chatId);
                await ProcessCurrentStateAsync(chatId, cancellationToken);
            }
        }
        else if (data.StartsWith("airport_"))
        {
            var parts = data.Split('_');
            var airportCode = parts[1];
            var state = await _userStateService.GetStateAsync(chatId);

            if (state.CurrentStep == BotState.DepartureCity)
            {
                state.Subscription.DepartureAirport = airportCode;
                await _userStateService.MoveToNextStateAsync(chatId, BotState.ArrivalCity);
            }
            else if (state.CurrentStep == BotState.ArrivalCity)
            {
                state.Subscription.ArrivalAirport = airportCode;
                await _userStateService.MoveToNextStateAsync(chatId, BotState.DepartureDate);
            }

            await ProcessCurrentStateAsync(chatId, cancellationToken);
        }
        else
        {
            await HandleMainMenuSelectionAsync(chatId, data, cancellationToken);
        }

        await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
    }

    private async Task ShowMainMenuAsync(long chatId, CancellationToken cancellationToken)
    {
        var keyboard = new InlineKeyboardMarkup(
        [
            [InlineKeyboardButton.WithCallbackData("Создать подписку", "create")],
            [InlineKeyboardButton.WithCallbackData("Редактировать подписку", "edit")],
            [InlineKeyboardButton.WithCallbackData("Удалить подписку", "delete")]
        ]);

        await _botClient.SendMessage(
            chatId: chatId,
            text: "Выберите действие:",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    private async Task HandleMainMenuSelectionAsync(long chatId, string selection, CancellationToken cancellationToken)
    {
        switch (selection)
        {
            case "create":
                await _userStateService.SetStateAsync(chatId, new UserState { CurrentStep = BotState.DepartureCity, Subscription = new Subscription() });
                await ProcessCurrentStateAsync(chatId, cancellationToken);
                break;
            case "edit":
                await _botClient.SendMessage(chatId, "Редактирование подписки пока не поддерживается", cancellationToken: cancellationToken);
                break;
            case "delete":
                await _botClient.SendMessage(chatId, "Удаление подписки пока не поддерживается", cancellationToken: cancellationToken);
                break;
        }
    }

    private async Task HandleStateMachineAsync(long chatId, string input, CancellationToken cancellationToken)
    {
        var state = await _userStateService.GetStateAsync(chatId);

        switch (state.CurrentStep)
        {
            case BotState.DepartureCity:
            case BotState.ArrivalCity:
                var airports = await _airportService.SearchAirportsAsync(input);
                await ShowAirportOptionsAsync(chatId, airports, cancellationToken);
                break;
            case BotState.DepartureDate:
                if (DateTime.TryParse(input, out var date))
                {
                    state.Subscription.DepartureDate = date.ToString("yyyy-MM-dd");
                    await _userStateService.MoveToNextStateAsync(chatId, BotState.MaxPrice);
                    await ProcessCurrentStateAsync(chatId, cancellationToken);
                }
                else
                {
                    await _botClient.SendMessage(chatId, "Пожалуйста, введите дату в формате ДД.ММ.ГГГГ", cancellationToken: cancellationToken);
                }
                break;
            case BotState.MaxPrice:
                if (int.TryParse(input, out var maxPrice))
                {
                    state.Subscription.MaxPrice = maxPrice;
                    await _userStateService.MoveToNextStateAsync(chatId, BotState.MaxTransfers);
                    await ProcessCurrentStateAsync(chatId, cancellationToken);
                }
                else
                {
                    await _botClient.SendMessage(chatId, "Пожалуйста, введите число", cancellationToken: cancellationToken);
                }
                break;
            case BotState.MaxTransfers:
                if (int.TryParse(input, out var maxTransfers))
                {
                    state.Subscription.MaxTransfersCount = maxTransfers;
                    await _userStateService.MoveToNextStateAsync(chatId, BotState.MinBaggage);
                    await ProcessCurrentStateAsync(chatId, cancellationToken);
                }
                else
                {
                    await _botClient.SendMessage(chatId, "Пожалуйста, введите число", cancellationToken: cancellationToken);
                }
                break;
            case BotState.MinBaggage:
                if (int.TryParse(input, out var minBaggage))
                {
                    state.Subscription.MinBaggageAmount = minBaggage;
                    await _userStateService.MoveToNextStateAsync(chatId, BotState.MinHandbags);
                    await ProcessCurrentStateAsync(chatId, cancellationToken);
                }
                else
                {
                    await _botClient.SendMessage(chatId, "Пожалуйста, введите число", cancellationToken: cancellationToken);
                }
                break;
            case BotState.MinHandbags:
                if (int.TryParse(input, out var minHandbags))
                {
                    state.Subscription.MinHandbagsAmount = minHandbags;
                    state.Subscription.ChatId = chatId;
                    await _subscriptionService.CreateSubscriptionAsync(state.Subscription);
                    await _userStateService.ClearStateAsync(chatId);
                    await _botClient.SendMessage(chatId, "Подписка создана!", replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton[0]), cancellationToken: cancellationToken);
                    await ShowMainMenuAsync(chatId, cancellationToken);
                }
                else
                {
                    await _botClient.SendMessage(chatId, "Пожалуйста, введите число", cancellationToken: cancellationToken);
                }
                break;
        }
    }

    private async Task ProcessCurrentStateAsync(long chatId, CancellationToken cancellationToken)
    {
        var state = await _userStateService.GetStateAsync(chatId);
        var keyboard = new InlineKeyboardMarkup(new[] { InlineKeyboardButton.WithCallbackData("Назад", "back") });

        string message = state.CurrentStep switch
        {
            BotState.DepartureCity => "Введите город вылета:",
            BotState.ArrivalCity => "Введите город прилета:",
            BotState.DepartureDate => "Введите дату вылета (ДД.ММ.ГГГГ):",
            BotState.MaxPrice => "Введите максимальную цену:",
            BotState.MaxTransfers => "Введите максимальное количество пересадок:",
            BotState.MinBaggage => "Введите минимальное количество багажа:",
            BotState.MinHandbags => "Введите минимальное количество ручной клади:",
            _ => "Неизвестное состояние"
        };

        await _botClient.SendMessage(
            chatId: chatId,
            text: message,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    private async Task ShowAirportOptionsAsync(long chatId, List<Airport> airports, CancellationToken cancellationToken)
    {
        var keyboardButtons = airports.Take(5).Select(airport =>
            new[] { InlineKeyboardButton.WithCallbackData($"{airport.Title} ({airport.Subtitle})", $"airport_{airport.Slug}") })
            .ToList();
        keyboardButtons.Add([InlineKeyboardButton.WithCallbackData("Назад", "back")]);

        var keyboard = new InlineKeyboardMarkup(keyboardButtons);

        await _botClient.SendMessage(
            chatId: chatId,
            text: "Выберите аэропорт:",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
}