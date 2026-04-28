using Akka.Actor;
using Akka.Configuration;
using Kanelson.Domain.Questions;
using Kanelson.Domain.Rooms;
using Kanelson.Domain.Rooms.Models;
using Kanelson.Domain.Templates;
using Kanelson.Domain.Templates.Models;
using Kanelson.Domain.Users;
using Xunit;

namespace Kanelson.Tests;

public class SerializationSpecs : IDisposable
{
    private readonly ActorSystem _system;

    public SerializationSpecs()
    {
        _system = ActorSystem.Create("SerializationTest", ConfigurationFactory.ParseString(@"
            akka.actor {
              serializers {
                messagepack = ""Akka.Serialization.MessagePack.MsgPackSerializer, Akka.Serialization.MessagePack""
              }
              serialization-bindings {
                ""System.Object"" = messagepack
              }
            }
        "));
    }

    private T Roundtrip<T>(T obj)
    {
        var serializer = _system.Serialization.FindSerializerForType(typeof(T));
        var bytes = serializer.ToBinary(obj);
        return (T)serializer.FromBinary(bytes, typeof(T));
    }

    [Fact]
    public void UserInfo_roundtrips()
    {
        var original = new UserInfo("u1") { Name = "Alice" };
        var result = Roundtrip(original);
        Assert.Equal(original.Id, result.Id);
        Assert.Equal(original.Name, result.Name);
    }

    [Fact]
    public void UpsertUser_roundtrips()
    {
        var original = new UserCommands.UpsertUser("u1", "Alice");
        var result = Roundtrip(original);
        Assert.Equal(original.UserId, result.UserId);
        Assert.Equal(original.Name, result.Name);
    }

    [Fact]
    public void RoomPlacement_roundtrips()
    {
        var original = new RoomPlacement("room-1", "Room", 1, 10, 1000, new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc));
        var result = Roundtrip(original);
        Assert.Equal(original, result);
    }

    [Fact]
    public void Question_roundtrips()
    {
        var original = new Question
        {
            Name = "What is 2+2?",
            TimeLimit = 10,
            Points = 500,
            Type = QuestionType.Quiz,
            Alternatives =
            [
                new Alternative { Description = "Four", Correct = true },
                new Alternative { Description = "Five", Correct = false },
            ]
        };
        var result = Roundtrip(original);
        Assert.Equal(original.Id, result.Id);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Alternatives.Count, result.Alternatives.Count);
        Assert.Equal(original.Alternatives[0].Correct, result.Alternatives[0].Correct);
    }

    [Fact]
    public void Template_roundtrips()
    {
        var original = new Template
        {
            Name = "My Quiz",
            Questions =
            [
                new TemplateQuestion { Name = "Q1", Type = QuestionType.TrueFalse, Order = 0,
                    Alternatives = [new Alternative { Description = "True", Correct = true }, new Alternative { Description = "False", Correct = false }] }
            ]
        };
        var result = Roundtrip(original);
        Assert.Equal(original.Id, result.Id);
        Assert.Equal(original.Name, result.Name);
        Assert.Single(result.Questions);
        Assert.Equal(0, result.Questions[0].Order);
    }

    [Fact]
    public void RoomState_roundtrips()
    {
        var original = new RoomState
        {
            OwnerId = "u1",
            Name = "Test Room",
            CurrentState = RoomStatus.Started,
            CurrentQuestionIdx = 1,
            MaxQuestionIdx = 5,
        };
        var result = Roundtrip(original);
        Assert.Equal(original.OwnerId, result.OwnerId);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.CurrentState, result.CurrentState);
        Assert.Equal(original.CurrentQuestionIdx, result.CurrentQuestionIdx);
    }

    [Fact]
    public void RoomTemplateState_roundtrips()
    {
        var original = new RoomTemplateState { Template = new Template { Name = "T1" } };
        var result = Roundtrip(original);
        Assert.Equal(original.Template.Name, result.Template.Name);
    }

    [Fact]
    public void UserQuestionsState_roundtrips()
    {
        var q = new Question { Name = "Q?", Type = QuestionType.TrueFalse,
            Alternatives = [new Alternative { Description = "Yes", Correct = true }, new Alternative { Description = "No", Correct = false }] };
        var original = new UserQuestionsState { Questions = new Dictionary<Guid, Question> { [q.Id] = q } };
        var result = Roundtrip(original);
        Assert.Single(result.Questions);
        Assert.Equal(q.Name, result.Questions[q.Id].Name);
    }

    [Fact]
    public void AlternativeVoteSummary_roundtrips()
    {
        var original = new AlternativeVoteSummary(Guid.NewGuid(), "Option A", 7, true);
        var result = Roundtrip(original);
        Assert.Equal(original.AlternativeId, result.AlternativeId);
        Assert.Equal(original.Description, result.Description);
        Assert.Equal(original.VoteCount, result.VoteCount);
        Assert.Equal(original.Correct, result.Correct);
    }

    public void Dispose() => _system.Terminate().Wait(TimeSpan.FromSeconds(5));
}
