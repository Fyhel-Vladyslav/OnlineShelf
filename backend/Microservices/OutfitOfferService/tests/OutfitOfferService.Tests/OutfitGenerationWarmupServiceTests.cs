using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OutfitOfferService.src.OutfitOfferService.Common.Interfaces;
using OutfitOfferService.src.OutfitOfferService.Common.Models;
using OutfitOfferService.src.OutfitOfferService.Host.Services.OutfitGeneration;
using static OutfitOfferService.Tests.TestData;

namespace OutfitOfferService.Tests;

public class OutfitGenerationWarmupServiceTests
{
    private sealed class FakeWardrobe(int failures = 0, StatusCode status = StatusCode.Unavailable) : IWardrobeProvider
    {
        private int _failuresLeft = failures;

        public int Calls { get; private set; }

        public Task<IReadOnlyList<WardrobeItem>> GetWardrobeAsync(Guid userId, bool onlyFavorite, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (_failuresLeft-- > 0)
            {
                throw new RpcException(new Status(status, "not ready"));
            }

            return Task.FromResult<IReadOnlyList<WardrobeItem>>([]);
        }
    }

    private static async Task RunAsync(FakeWardrobe wardrobe, FakeScorer scorer, OutfitGenerationOptions settings)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWardrobeProvider>(wardrobe);
        services.AddSingleton<IOutfitScorer>(scorer);
        services.AddSingleton<IConstraintFilter>(Filter(settings));
        services.AddSingleton(Wrap(settings));
        services.AddScoped<IOutfitGenerator, BeamSearchOutfitGenerator>();

        var warmup = new OutfitGenerationWarmupService(services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            Wrap(settings), NullLogger<OutfitGenerationWarmupService>.Instance);

        await warmup.StartAsync(CancellationToken.None);
        await warmup.ExecuteTask!;
    }

    [Fact]
    public async Task Warmup_OpensBothChannels_AndScoresSyntheticOutfits()
    {
        var wardrobe = new FakeWardrobe();
        var scorer = new FakeScorer();

        await RunAsync(wardrobe, scorer, Options());

        Assert.Equal(1, wardrobe.Calls);
        Assert.True(scorer.ScoredOutfits > 0);
    }

    [Fact]
    public async Task Warmup_RetriesUntilDownstreamIsReady()
    {
        var wardrobe = new FakeWardrobe(failures: 2);
        var scorer = new FakeScorer();

        await RunAsync(wardrobe, scorer, Options(o => o.WarmupRetryDelaySeconds = 0));

        Assert.Equal(3, wardrobe.Calls);
        Assert.True(scorer.ScoredOutfits > 0);
    }

    [Fact]
    public async Task Warmup_GivesUpAfterMaxAttempts_WithoutFailing()
    {
        var wardrobe = new FakeWardrobe(failures: 100);

        await RunAsync(wardrobe, new FakeScorer(), Options(o =>
        {
            o.WarmupRetryDelaySeconds = 0;
            o.WarmupMaxAttempts = 3;
        }));

        Assert.Equal(3, wardrobe.Calls);
    }

    [Fact]
    public async Task Warmup_DoesNotRetryNonTransientErrors()
    {
        var wardrobe = new FakeWardrobe(failures: 100, status: StatusCode.InvalidArgument);

        await RunAsync(wardrobe, new FakeScorer(), Options(o => o.WarmupRetryDelaySeconds = 0));

        Assert.Equal(1, wardrobe.Calls);
    }

    [Fact]
    public async Task Warmup_Disabled_DoesNothing()
    {
        var wardrobe = new FakeWardrobe();

        await RunAsync(wardrobe, new FakeScorer(), Options(o => o.WarmupOnStartup = false));

        Assert.Equal(0, wardrobe.Calls);
    }

    [Fact]
    public void SyntheticWardrobe_CoversBothTemplates()
    {
        var settings = Options();

        var pool = Filter(settings).Apply(OutfitGenerationWarmupService.BuildSyntheticWardrobe(settings), Context(seasonPhase: 1));

        foreach (var slot in new[] { OutfitSlot.Top, OutfitSlot.Bottom, OutfitSlot.Shoes, OutfitSlot.Outerwear, OutfitSlot.FullBody })
            Assert.Equal(2, pool.For(slot).Count);
    }
}
