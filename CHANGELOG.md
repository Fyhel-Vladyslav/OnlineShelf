# Changelog

Один рядок на кожну тематичну зміну в коді, нові записи зверху в поточній даті.
Формат рядка: `- <Added|Changed|Fixed|Removed> <що саме> (<сервіс/файл>)`.

## 2026-10-04
- Added offers API module with weather/generate types and Ukrainian enum labels (admin-portal/src/api/offers/offersApi.ts)
- Added authService.getUserId reading nameid claim from JWT (admin-portal/AuthService.ts)
- Added useWeather query and useGenerateOutfits mutation with 400/503 error notifications (admin-portal/src/hooks/offers)
- Added Outfit Offer wizard page /outfits with Context/Include/Exclude/Reference steps and results (admin-portal/src/pages/outfits)
- Added /outfits protected route and Outfits header link (admin-portal App.tsx, Header.tsx)

## 2026-09-27
- Added EmbedClothing rpc returning L2-normalized CLIP visual embedding (parse-image.proto, ParseImageService)
- Added VisualEmbedder reusing MaterialDetector's CLIP model (ParseImageService/app/models/visual_embedder)
- Changed image decoding into shared helper and removed per-request debug_yolo_input.webp dump (analyzer_service.py)
- Changed ClothingDetector to accept any class when attribute dictionary is not loaded (clothing_detector.py)
- Added VisualEmbedding (real[]) and EmbeddingModel columns to Item with migration AddItemVisualEmbedding (ShelfsService)
- Added visual embedding computation on item create/update via ParseImageService (CreateItem.cs, UpdateItem.cs)
- Added Wardrobe gRPC server GetWardrobe with embeddings on separate h2c port 8082 (wardrobe.proto, ShelfsService)
- Added ScoreOutfits batch rpc and removed unimplemented GenerateOutfits from network contract (offer-network.proto)
- Added node_categorical input to ONNX scorer contract and zero-padding for items without embedding (OnnxGraphCompatibilityScorer.cs)
- Changed OutfitNetworkService to gRPC-only: HTTP/2 Kestrel, ONNX scorer if model file exists else stub, VirtualItemPenaltyRule registered
- Removed unreachable REST ScoreOutfit endpoint, its DTOs and stale proto copy (OutfitNetworkService)
- Added IOutfitScorer interface with gRPC implementation NetworkOutfitScorer (OutfitOfferService)
- Added IWardrobeProvider with gRPC implementation ShelfsWardrobeProvider (OutfitOfferService)
- Added configurable AttributeType to outfit slot mapping (SlotResolver, OutfitGeneration:Slots)
- Added CSP constraint filter: include/exclude, season tolerance, weather-based outerwear rules (ConstraintFilter.cs)
- Added beam search outfit generator over top+bottom and full-body templates with batch scoring (BeamSearchOutfitGenerator.cs)
- Added outfit offer orchestrator and POST offers/generate endpoint (OutfitOfferService)
- Fixed weather: temperature in response, OpenWeather group mapping, configurable 30 min TTL, SemaphoreSlim against thundering herd (WeatherService.cs)
- Fixed get-weather request binding (fields to properties) and duplicate route from GenerateOutfit stub (OutfitOfferService)
- Fixed decimal query parsing on non-invariant locales by forcing invariant culture (OutfitOfferService/Program.cs)
- Changed EntityFrameworkCore.Tools 10.0.9 to 9.0.10 to match EF Core runtime (OutfitOfferService.csproj)
- Added OutfitOfferService.Tests with 60 unit tests for offer business logic
- Added offers routes to gateway (ocelot.json)
- Changed docker-compose: Shelfs gRPC port 8082, Offer gRPC settings and port 8083, fixed network image name, removed unused DB strings

## 2026-09-25
- Added CLAUDE.md with changelog rule for Claude Code

## 2026-09-24
- Added project research report (Claude outputs/REPORT_01_project_research.md)
