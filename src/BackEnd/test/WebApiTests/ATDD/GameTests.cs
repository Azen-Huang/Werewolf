using FastEndpoints;
using System.Net;
using System.Threading.Tasks.Dataflow;
using Was.Gaas.Werewolf.Application.Common;
using Was.Gaas.Werewolf.Application.UseCases;
using Wsa.Gaas.Werewolf.Domain.Events;
using Wsa.Gaas.Werewolf.WebApi.Endpoints;
using Wsa.Gaas.Werewolf.WebApiTests.ATDD.Common;

namespace Wsa.Gaas.Werewolf.WebApiTests.ATDD
{
    public class GameTests
    {
        private readonly WebApiTestServer _server = new();

        [SetUp]
        public async Task Setup()
        {
            await _server.StartAsync();
        }

        [Test]
        [Description("""
            Given: �w�g�b�y���W�D�}�F�@�ӹC��
            When: �b�P�@���W�D, �A�}�@�ӹC����
            Then: �ݨ���~�T��
            """)]
        public async Task CreateGameTest()
        {
            /* Arrange */
            _server.ListenOn<GameCreatedEvent>();

            var request = new CreateGameRequest()
            {
                DiscordVoiceChannelId = (ulong)new Random().Next(),
            };

            /* Act */
            var (_, result) = await _server.Client.POSTAsync<CreateGameEndpoint, CreateGameRequest, CreateGameResponse>(request);

            // 2nd Call should get 500 error
            var (response, _) = await _server.Client.POSTAsync<CreateGameEndpoint, CreateGameRequest, CreateGameResponse>(request);

            /* Assert */
            // Check Rest API Result
            result.Should().NotBeNull();
            result!.GameId.Should().NotBe(Guid.Empty);
            result.DiscordVoiceChannelId.Should().Be(request.DiscordVoiceChannelId);

            // Check SignalR Response
            var gameEvent = await _server.EventBuffer.ReceiveAsync();
            gameEvent.Should().NotBeNull();
            gameEvent!.GameId.Should().Be(result.GameId);
            gameEvent.DiscordVoiceChannelId.Should().Be(result.DiscordVoiceChannelId);

            // Check Database
            var repository = _server.GetRequiredService<IRepository>();
            var game = await repository.FindByIdAsync(result.GameId);
            game.Should().NotBeNull();
            game!.Id.Should().Be(result.GameId);
            game.DiscordVoiceChannelId.Should().Be(result.DiscordVoiceChannelId);

            // Check 2nd Call Response
            response.Should().NotBeNull();
            response!.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }
    }
}