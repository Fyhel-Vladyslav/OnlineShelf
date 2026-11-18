using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OutfitOfferService.src.OutfitOfferService.Common.Interfaces;
using OutfitOfferService.src.OutfitOfferService.Common.Models;
using OutfitOfferService.src.OutfitOfferService.Common.Models.Reference;
using OutfitOfferService.src.OutfitOfferService.Host.Services.Reference;

namespace OutfitOfferService.Tests;

public class ReferencePhotoProcessorTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class FakeRepository(ClaimedReferencePhoto? next) : IReferencePhotoRepository
    {
        public int Claims { get; private set; }
        public List<(Guid Id, OutfitAnalysis Analysis, ReferenceOutfitStatus Status)> Completed { get; } = [];
        public List<(Guid Id, string Error, DateTime At)> Retried { get; } = [];
        public List<(Guid Id, string Error)> Failed { get; } = [];
        public int ExhaustedCalls { get; private set; }

        public Task<ClaimedReferencePhoto?> ClaimNextAsync(TimeSpan lockDuration, int maxAttempts, CancellationToken ct)
        {
            Claims++;
            return Task.FromResult(next);
        }

        public Task<int> FailExhaustedAsync(int maxAttempts, CancellationToken ct)
        {
            ExhaustedCalls++;
            return Task.FromResult(0);
        }

        public Task CompleteAsync(Guid photoId, OutfitAnalysis analysis, ReferenceOutfitStatus outfitStatus, CancellationToken ct)
        {
            Completed.Add((photoId, analysis, outfitStatus));
            return Task.CompletedTask;
        }

        public Task RetryLaterAsync(Guid photoId, string error, DateTime nextAttemptAt, CancellationToken ct)
        {
            Retried.Add((photoId, error, nextAttemptAt));
            return Task.CompletedTask;
        }

        public Task FailAsync(Guid photoId, string error, CancellationToken ct)
        {
            Failed.Add((photoId, error));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeImages(Exception? failure = null) : IReferenceImageStore
    {
        public Task<(string BigImage, string SmallImage)?> UploadAsync(byte[] imageBytes, string fileName, CancellationToken ct) =>
            Task.FromResult<(string, string)?>(("big", "small"));

        public Task<byte[]> DownloadOriginalAsync(string bigImage, CancellationToken ct) =>
            failure is null ? Task.FromResult(new byte[] { 1, 2, 3 }) : Task.FromException<byte[]>(failure);

        public Task DeleteAsync(string bigImage, string smallImage, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeAnalyzer(Func<OutfitAnalysis> result) : IOutfitPhotoAnalyzer
    {
        public Task<OutfitAnalysis> AnalyzeAsync(byte[] imageBytes, CancellationToken ct) => Task.FromResult(result());
    }

    private sealed class ThrowingAnalyzer(Exception exception) : IOutfitPhotoAnalyzer
    {
        public Task<OutfitAnalysis> AnalyzeAsync(byte[] imageBytes, CancellationToken ct) => Task.FromException<OutfitAnalysis>(exception);
    }

    private static AnalyzedItem Item(OutfitSlot slot) =>
        new(slot, 1, 0, 0, 0, "#ffffff", "#000000", 0.9f, new AnalyzedBox(1, 2, 30, 40), [0.1f, 0.2f]);

    private static OutfitAnalysis Analysis(params OutfitSlot[] slots) =>
        new(true, null, 800, 1200, "fashion-clip", slots.Select(Item).ToList());

    private static ReferencePhotoProcessor Processor(IReferencePhotoRepository repository, IReferenceImageStore images,
        IOutfitPhotoAnalyzer analyzer) =>
        new(repository, images, analyzer, Options.Create(new ReferenceProcessingOptions()), new FixedTime(),
            NullLogger<ReferencePhotoProcessor>.Instance);

    private static ClaimedReferencePhoto Photo(int attempts = 1) => new(Guid.NewGuid(), "x_big.webp", attempts);

    [Fact]
    public async Task EmptyQueue_ReturnsFalse_AndDoesNothing()
    {
        var repository = new FakeRepository(null);
        var processor = Processor(repository, new FakeImages(), new FakeAnalyzer(() => Analysis()));

        Assert.False(await processor.ProcessNextAsync(CancellationToken.None));
        Assert.Equal(1, repository.ExhaustedCalls);
        Assert.Empty(repository.Completed);
    }

    [Fact]
    public async Task CompleteOutfit_IsSavedAsReady()
    {
        var photo = Photo();
        var repository = new FakeRepository(photo);
        var processor = Processor(repository, new FakeImages(),
            new FakeAnalyzer(() => Analysis(OutfitSlot.Top, OutfitSlot.Bottom, OutfitSlot.Shoes)));

        Assert.True(await processor.ProcessNextAsync(CancellationToken.None));

        var saved = Assert.Single(repository.Completed);
        Assert.Equal(photo.Id, saved.Id);
        Assert.Equal(ReferenceOutfitStatus.Ready, saved.Status);
        Assert.Empty(repository.Failed);
        Assert.Empty(repository.Retried);
    }

    [Fact]
    public async Task OutfitWithoutShoes_NeedsReview()
    {
        var repository = new FakeRepository(Photo());
        var processor = Processor(repository, new FakeImages(),
            new FakeAnalyzer(() => Analysis(OutfitSlot.Top, OutfitSlot.Bottom)));

        await processor.ProcessNextAsync(CancellationToken.None);

        Assert.Equal(ReferenceOutfitStatus.NeedsReview, Assert.Single(repository.Completed).Status);
    }

    [Fact]
    public async Task NoItems_IsCompletedWithEmptyList()
    {
        var repository = new FakeRepository(Photo());
        var processor = Processor(repository, new FakeImages(), new FakeAnalyzer(() => Analysis()));

        await processor.ProcessNextAsync(CancellationToken.None);

        Assert.Empty(Assert.Single(repository.Completed).Analysis.Items);
        Assert.Empty(repository.Failed);
    }

    [Fact]
    public async Task ParseRefusal_FailsImmediately_WithoutRetry()
    {
        var repository = new FakeRepository(Photo());
        var refusal = new OutfitAnalysis(false, "Непідтримуваний формат фото", 0, 0, "", []);
        var processor = Processor(repository, new FakeImages(), new FakeAnalyzer(() => refusal));

        await processor.ProcessNextAsync(CancellationToken.None);

        Assert.Equal("Непідтримуваний формат фото", Assert.Single(repository.Failed).Error);
        Assert.Empty(repository.Retried);
        Assert.Empty(repository.Completed);
    }

    [Theory]
    [InlineData(StatusCode.Unavailable)]
    [InlineData(StatusCode.DeadlineExceeded)]
    public async Task TemporaryParseFailure_ReturnsPhotoToQueueWithFirstDelay(StatusCode code)
    {
        var photo = Photo(attempts: 1);
        var repository = new FakeRepository(photo);
        var processor = Processor(repository, new FakeImages(),
            new ThrowingAnalyzer(new RpcException(new Status(code, "parse is starting"))));

        await processor.ProcessNextAsync(CancellationToken.None);

        var retry = Assert.Single(repository.Retried);
        Assert.Equal(Now.UtcDateTime.AddSeconds(30), retry.At);
        Assert.Contains(code.ToString(), retry.Error);
        Assert.Empty(repository.Failed);
    }

    [Theory]
    [InlineData(2, 120)]
    [InlineData(3, 600)]
    public async Task RepeatedFailures_UseGrowingDelays(int attempts, int expectedSeconds)
    {
        var repository = new FakeRepository(Photo(attempts));
        var processor = Processor(repository, new FakeImages(),
            new ThrowingAnalyzer(new RpcException(new Status(StatusCode.Unavailable, "down"))));

        await processor.ProcessNextAsync(CancellationToken.None);

        Assert.Equal(Now.UtcDateTime.AddSeconds(expectedSeconds), Assert.Single(repository.Retried).At);
    }

    [Fact]
    public async Task LastAttempt_FailsInsteadOfRetrying()
    {
        var repository = new FakeRepository(Photo(attempts: 4));
        var processor = Processor(repository, new FakeImages(),
            new ThrowingAnalyzer(new RpcException(new Status(StatusCode.Unavailable, "down"))));

        await processor.ProcessNextAsync(CancellationToken.None);

        Assert.Single(repository.Failed);
        Assert.Empty(repository.Retried);
    }

    [Fact]
    public async Task MissingFileInImageService_FailsWithoutRetry()
    {
        var repository = new FakeRepository(Photo());
        var processor = Processor(repository, new FakeImages(new RpcException(new Status(StatusCode.NotFound, "no file"))),
            new FakeAnalyzer(() => Analysis(OutfitSlot.Top)));

        await processor.ProcessNextAsync(CancellationToken.None);

        Assert.Contains("not found", Assert.Single(repository.Failed).Error);
        Assert.Empty(repository.Retried);
    }

    [Fact]
    public async Task ImageServiceUnavailable_IsRetried()
    {
        var repository = new FakeRepository(Photo());
        var processor = Processor(repository, new FakeImages(new RpcException(new Status(StatusCode.Unavailable, "down"))),
            new FakeAnalyzer(() => Analysis(OutfitSlot.Top)));

        await processor.ProcessNextAsync(CancellationToken.None);

        Assert.Single(repository.Retried);
        Assert.Empty(repository.Failed);
    }

    [Fact]
    public async Task ServiceShutdown_LeavesPhotoLocked_InsteadOfRecordingFailure()
    {
        using var cts = new CancellationTokenSource();
        var repository = new FakeRepository(Photo());
        var processor = Processor(repository, new FakeImages(),
            new ThrowingAnalyzer(new OperationCanceledException(cts.Token)));
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => processor.ProcessNextAsync(cts.Token));

        Assert.Empty(repository.Failed);
        Assert.Empty(repository.Retried);
    }

    [Theory]
    [InlineData(new[] { OutfitSlot.Shoes, OutfitSlot.FullBody }, true)]
    [InlineData(new[] { OutfitSlot.Shoes, OutfitSlot.Top, OutfitSlot.Bottom }, true)]
    [InlineData(new[] { OutfitSlot.Shoes, OutfitSlot.Top }, false)]
    [InlineData(new[] { OutfitSlot.Top, OutfitSlot.Bottom }, false)]
    [InlineData(new[] { OutfitSlot.Shoes, OutfitSlot.Outerwear, OutfitSlot.Accessory }, false)]
    public void Classifier_RequiresShoesAndDressOrTopWithBottom(OutfitSlot[] slots, bool complete) =>
        Assert.Equal(complete, ReferenceOutfitClassifier.IsComplete(slots));

    [Fact]
    public void UploadedImageFormat_AcceptsJpegPngWebp_AndRejectsOthers()
    {
        Assert.True(UploadedImageFormat.IsSupported([0xFF, 0xD8, 0xFF, 0xE0, 0x00]));
        Assert.True(UploadedImageFormat.IsSupported([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00]));
        Assert.True(UploadedImageFormat.IsSupported("RIFF\0\0\0\0WEBPVP8 "u8));

        Assert.False(UploadedImageFormat.IsSupported("RIFF\0\0\0\0WAVEfmt "u8));
        Assert.False(UploadedImageFormat.IsSupported("GIF89a"u8));
        Assert.False(UploadedImageFormat.IsSupported("data:image/png;base64,AAAA"u8));
        // HEIC: ISO base media, ftyp на зсуві 4
        Assert.False(UploadedImageFormat.IsSupported([0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x68, 0x65, 0x69, 0x63]));
        Assert.False(UploadedImageFormat.IsSupported([]));
    }
}
