// using Akka.Actor;
// using Akka.Persistence.TestKit;
// using Akka.TestKit;
// using FluentAssertions;
// using Kanelson.Actors.Questions;
//
// namespace Kanelson.Tests;
//
// public class QuestionIndexSpecs : PersistenceTestKit
// {
//     
//     private const string UserId = "12345";
//     private const string UserId2 = "123456";
//     private readonly TestActorRef<QuestionIndex> _testActor;
//     public QuestionIndexSpecs()
//     {
//
//         var props = Props.Create<QuestionIndex>("question-index");
//         _testActor = new TestActorRef<QuestionIndex>(Sys, props);
//         
//     }
//     
//     
//     [Fact]
//     public async Task Getting_Reference_should_return_valid_child()
//     {
//         // act
//         var child = await _testActor.Ask<IActorRef>(new GetRef(UserId));
//         
//         // assert
//         child.Should().NotBe(ActorRefs.Nobody);
//     }
//
//
//     [Fact]
//     public async Task Getting_already_used_reference_should_return_valid_child()
//     {
//         // arrange
//         _testActor.Tell(new GetRef(UserId));
//         
//         // act
//         var child = await _testActor.Ask<IActorRef>(new GetRef(UserId));
//         
//         // assert
//         child.Should().NotBe(ActorRefs.Nobody);
//     }
//     
//     [Fact]
//     public async Task Getting_different_userId_should_return_different_child()
//     {
//         // arrange
//         var child1 = await _testActor.Ask<IActorRef>(new GetRef(UserId));
//         
//         // act
//         var child2 = await _testActor.Ask<IActorRef>(new GetRef(UserId2));
//         
//         // assert
//         child1.Should().NotBe(ActorRefs.Nobody);
//         child2.Should().NotBe(ActorRefs.Nobody);
//         child1.Should().NotBe(child2);
//
//     }
// }