﻿using Kanelson.Domain.Questions;
using Kanelson.Domain.Users;

namespace Kanelson.Domain.Rooms.Models;

public record RoomSummary(string Id, string Name, UserInfo Owner);

public enum RoomStatus
{
    Created,
    Started,
    DisplayingQuestion,
    AwaitingForNextQuestion,
    Finished,
    Abandoned
}

public record CurrentQuestionInfo(Question Question, int CurrentNumber, int MaxNumber);

public record UserAnswerSummary(Question Question, IEnumerable<Guid> Answered);