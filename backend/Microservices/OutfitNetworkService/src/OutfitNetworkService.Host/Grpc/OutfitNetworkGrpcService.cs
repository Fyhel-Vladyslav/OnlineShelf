using Grpc.Core;
using OutfitNetworkService.Protos;
using OutfitNetworkService.src.OutfitNetworkService.Common.Interfaces;
using OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;
using ItemNode = OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities.ItemNode;

namespace OutfitNetworkService.src.OutfitNetworkService.Host.Grpc
{
    public sealed class OutfitNetworkGrpcService : Protos.OutfitNetworkService.OutfitNetworkServiceBase
    {
        private readonly IScoringOrchestrator _scoringOrchestrator; // або перейменований orchestrator

        public OutfitNetworkGrpcService(IScoringOrchestrator scoringOrchestrator)
        {
            _scoringOrchestrator = scoringOrchestrator;
        }

        public override async Task<ScoreOutfitGrpcResponse> ScoreOutfit(
           ScoreOutfitGrpcRequest request, ServerCallContext context)
        {
            var nodes = request.Items.Select(i => new ItemNode
            {
                ItemId = i.ItemId,
                AttributeType = i.AttributeType,
                IsVirtual = i.IsVirtual,
                IsPinned = i.IsPinned,
                FeatureVector = i.FeatureVector.ToArray(), // repeated float у proto -> array  
                AttributeColorMain = i.AttributeColorMain,
                AttributeColorSecond = i.AttributeColorSecond,
                AttributeSeason = i.AttributeSeason,
                AttributePattern = i.AttributePattern,
                AttributeMatterial = i.AttributeMatterial,
            }).ToList();

            var result = await _scoringOrchestrator.EvaluateAsync(nodes, context.CancellationToken);

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
