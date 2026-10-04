using Grpc.Core;
using OutfitNetworkService.Protos;
using OutfitNetworkService.src.OutfitNetworkService.Common.Interfaces;
using OutfitNetworkService.src.OutfitNetworkService.Host.Services.OutfitCompatibilityScorer;
using ItemNode = OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities.ItemNode;

namespace OutfitNetworkService.src.OutfitNetworkService.Host.Grpc
{
    public sealed class OutfitNetworkGrpcService : Protos.OutfitNetworkService.OutfitNetworkServiceBase
    {
        private readonly IScoringOrchestrator _scoringOrchestrator;

        public OutfitNetworkGrpcService(IScoringOrchestrator scoringOrchestrator)
        {
            _scoringOrchestrator = scoringOrchestrator;
        }

        public override Task<ScoreOutfitGrpcResponse> ScoreOutfit(
           ScoreOutfitGrpcRequest request, ServerCallContext context)
            => ScoreAsync(request, context.CancellationToken);

        public override async Task<ScoreOutfitsGrpcResponse> ScoreOutfits(
            ScoreOutfitsGrpcRequest request, ServerCallContext context)
        {
            var response = new ScoreOutfitsGrpcResponse();
            foreach (var outfit in request.Outfits)
            {
                response.Results.Add(await ScoreAsync(outfit, context.CancellationToken));
            }
            return response;
        }

        private async Task<ScoreOutfitGrpcResponse> ScoreAsync(ScoreOutfitGrpcRequest request, CancellationToken ct)
        {
            if (request.Items.Count < 2)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Outfit must contain at least 2 items"));
            }

            var nodes = request.Items.Select(ToNode).ToList();
            var result = await _scoringOrchestrator.EvaluateAsync(nodes, ct);
            return ToProto(result);
        }

        private static ItemNode ToNode(Protos.ItemNode i) => new()
        {
            ItemId = i.ItemId,
            AttributeType = i.AttributeType,
            IsVirtual = i.IsVirtual,
            IsPinned = i.IsPinned,
            FeatureVector = i.FeatureVector.ToArray(),
            AttributeColorMain = i.AttributeColorMain,
            AttributeColorSecond = i.AttributeColorSecond,
            AttributeSeason = i.AttributeSeason,
            AttributePattern = i.AttributePattern,
            AttributeMatterial = i.AttributeMatterial,
        };

        private static ScoreOutfitGrpcResponse ToProto(OutfitCompatibilityResult result)
        {
            var response = new ScoreOutfitGrpcResponse
            {
                FinalScore = result.FinalScore,
                GraphLevelScore = result.GraphLevelScore,
                PenaltyMultiplier = result.PenaltyMultiplier,
            };

            response.PairwiseScores.AddRange(result.PairwiseScores.Select(kv => new PairwiseScore
            {
                ItemIdA = kv.Key.SourceItemId,
                ItemIdB = kv.Key.TargetItemId,
                Score = kv.Value
            }));

            response.AppliedPenalties.Add(result.AppliedPenalties.ToDictionary(kv => kv.Key, kv => kv.Value));
            return response;
        }
    }
}
