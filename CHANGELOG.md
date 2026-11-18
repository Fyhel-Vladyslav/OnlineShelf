# Changelog

Один рядок на кожну тематичну зміну в коді, нові записи зверху в поточній даті.
Формат рядка: `- <Added|Changed|Fixed|Removed> <що саме> (<сервіс/файл>)`.

## 2026-11-18
- Added designer page /designer with upload (4 parallel requests), batches and review tabs (webui pages/designer)
- Added Designer link in Header for users with the Designer role (webui Header)
- Added API client and react-query hooks for reference batches, photos and review actions (webui api/offers/referenceApi.ts, hooks/offers/useReference.ts)
- Added outfit status filter and photo status filter for batch photos (OutfitOfferService Features/Reference)
- Added GET batches with photo and outfit status counts and GET batch photos with outfit status filter and paging (OutfitOfferService Features/Reference)
- Added approve-ready for all Ready outfits of a batch and retry for Failed photos (OutfitOfferService Features/Reference)
- Added PATCH outfit status and PATCH/DELETE reference item with outfit status recomputed while pending (OutfitOfferService Features/Reference)
- Added unit tests of outfit status recomputation after review (OutfitOfferService.Tests ReviewStatusTests)
- Fixed docker-compose dependency cycle by removing parseimageservice from outfitofferservice depends_on (docker-compose.yml)
- Added Docker check of the reference photo queue with duplicate, invalid file, EXIF and status summary (scripts/test_reference_queue.ps1)
- Added unit tests of the reference photo queue processor, outfit completeness and upload format check (OutfitOfferService.Tests ReferencePhotoProcessorTests)
- Added Gateway route /offers/reference/{everything} and compose settings for OutfitOfferService DB, ImageService and Parse (ocelot.json, docker-compose.yml)
- Added POST /offers/reference/batches and /batches/{BatchId}/photos with SHA-256 duplicate check and JPEG/PNG/WebP validation (OutfitOfferService Features/Reference, ReferencePhotoIntake)
- Added gRPC clients for ImageService photo storage and ParseImageService AnalyzeOutfit (OutfitOfferService GrpcReferenceImageStore, GrpcOutfitPhotoAnalyzer)
- Added Postgres queue of reference photos with FOR UPDATE SKIP LOCKED claim, lock expiry, retries 30 s/2 min/10 min and BackgroundService worker (OutfitOfferService ReferencePhotoRepository, ReferencePhotoProcessor, ReferencePhotoWorker)
- Added reference outfit entities, schema outfit_offer_service and InitialReferenceOutfits migration applied on startup (OutfitOfferService DataContext, Migrations)
- Removed unused OutfitGenerationRequest DbSet and TrainingCollection/TrainingItemsSet entities replaced by reference entities (OutfitOfferService)
- Changed gRPC receive limit to 32 MB for designer photos (ImageService Program.cs)
- Changed AnalyzeOutfit to accept only JPEG/PNG/WebP and apply EXIF orientation (ParseImageService analyzer_service)
- Changed proto to return image_width and image_height in AnalyzeOutfitResponse (parse-image.proto, regenerated pb2)
- Added evaluation of the compatibility scorer on crops from photos of people with domain-shift controls (ml/outfit_scorer/src/eval_reference.py)

## 2026-11-01
- Added AnalyzeOutfit gRPC method returning every item of a whole-outfit photo (ParseImageService parse-image.proto, analyzer_service)
- Added OutfitDetector on Conditional DETR and FashionCLIP TypeClassifier for items on people (ParseImageService)
- Added conversion of the detector weights to a local transformers format and ignored the 174 MB result in git (ml/outfit_scorer/src/convert_outfit_detector.py, .gitignore)
- Added Type accuracy evaluation of the outfit pipeline against YOLO classes (ml/outfit_scorer/src/eval_type.py)
- Changed gRPC receive limit to 32 MB for designer photos (ParseImageService main.py)
- Removed temporary YOLO debug output and unused import (ParseImageService ClothingDetector)
- Added ATR photos, Conditional DETR detector, timing and best-box-per-slot mode to detection evaluation (ml/outfit_scorer/src/eval_detection.py)
- Added whole-outfit detection evaluation on labelled Fashionpedia photos of people (ml/outfit_scorer/src/eval_detection.py)

## 2026-10-31
- Added evaluation of multi-item detection on whole-outfit collages with known boxes (ml/outfit_scorer/src/eval_detection.py)
- Added Corduroy material with migration that also syncs the AttributesValues identity sequence (ShelfsService DatabaseSeeder, AddCorduroyMaterial migration)
- Added background warmup of gRPC channels, beam search and the generate endpoint with retries while dependencies start (OutfitOfferService OutfitGenerationWarmupService, appsettings.json)
- Changed shared embedding and material model from CLIP to FashionCLIP (ParseImageService MaterialDetector, analyzer_service)
- Fixed polyester mapped to missing "synthetic" material, leaving the attribute empty (ParseImageService MaterialDetector)
- Changed embedding backfill to recompute vectors of other models and reject results from a wrong model (ShelfsService EmbeddingBackfillService, ItemRepository, appsettings.json)
- Changed outfit_gnn.onnx to pairwise model trained on FashionCLIP YOLO-crop embeddings (OutfitNetworkService/Models)
- Added material detection comparison of embedding models on weak labels from Polyvore titles (ml/outfit_scorer/src/eval_material.py)
- Changed default embedding model to FashionCLIP (ml/outfit_scorer/config.yaml, README.md, MODEL_CARD.md)

## 2026-10-25
- Added embedding-model-aware cache suffix and stored the embedding model in checkpoints and ONNX metadata (ml/outfit_scorer/src/data/embed.py, train.py, eval.py, export_onnx.py)
- Added storing original upload bytes next to WebP images and deleting them with the image (ImageService ImageGrpcService)
- Changed embedding backfill and self-heal to embed from the original image when available (ShelfsService ImageProcessorClientExtensions, EmbeddingBackfillService, UpdateItem)
- Changed Polyvore test wardrobe generator to also write original images (ml/outfit_scorer/src/seed_polyvore_wardrobe.py)
- Added diversity-aware top-N selection penalising repeated items, with all last-step beam candidates kept (OutfitOfferService BeamSearchOutfitGenerator, appsettings.json)
- Added model card with data, architecture, metrics, parity and limitations (ml/outfit_scorer/MODEL_CARD.md)
- Changed embedding precision to fp32 after the fp16 parity check failed (ml/outfit_scorer/config.yaml, README.md)
- Added startup warmup of the ONNX outfit scorer and scoring pipeline before Kestrel accepts requests (OutfitNetworkService)
- Changed outfit_gnn.onnx to pairwise model trained on YOLO-crop embeddings, matching ParseImageService input (OutfitNetworkService/Models)
- Added YOLO item crop before CLIP matching ParseImageService ClothingDetector, cached as *_yolo embeddings (ml/outfit_scorer/src/data/embed.py, config.yaml)
- Changed train/eval to read the embedding cache of the configured crop mode and store it in checkpoint/ONNX metadata (ml/outfit_scorer/src/train.py, eval.py, export_onnx.py)
- Added crop parity test against ParseImageService ClothingDetector (ml/outfit_scorer/tests/test_embed.py)
- Fixed tests package shadowed by the tests package shipped in the ultralytics wheel (ml/outfit_scorer/tests/__init__.py)
- Added ultralytics and opencv-python-headless with CUDA torchvision install notes (ml/outfit_scorer/requirements.txt, README.md)
- Added Polyvore test wardrobe generator with images, seed/cleanup SQL and ground-truth manifest for sanity check (ml/outfit_scorer/src/seed_polyvore_wardrobe.py)

## 2026-10-24
- Changed outfit scorer aggregation to a residual correction over mean pairwise logit, zero-initialised so training starts from pairwise (ml/outfit_scorer/src/model/scorer.py, config.yaml)
- Changed training limits to 150 epochs with patience 10 (ml/outfit_scorer/config.yaml)
- Added graph_score distribution for the pairwise model to test evaluation report (ml/outfit_scorer/src/eval.py)
- Changed outfit_gnn.onnx to pairwise CSA model selected by valid AUC (OutfitNetworkService/Models)
- Changed proto to pass slot and embedding_model in ItemNode (offer-network.proto)
- Changed NetworkOutfitScorer to fill slot via ISlotResolver and embedding_model from WardrobeItem (OutfitOfferService)
- Added Slot and EmbeddingModel to ItemNode and gRPC mapping (OutfitNetworkService)
- Changed OnnxGraphCompatibilityScorer to slot-based contract with embedding dim/model from ONNX metadata and missing-embedding handling (OutfitNetworkService)
- Added Models/** copy to output and read-only Models volume for outfitnetworkservice (OutfitNetworkService.csproj, docker-compose.yml)
- Added OutfitNetworkService.Tests with tiny_scorer.onnx contract tests and ScoringOrchestrator tests (OutfitNetworkService/tests, Microservices.sln)

## 2026-10-10
- Added CLIP embedding cache from Polyvore parquet with resume, fp16 parity check and OOM batch fallback (ml/outfit_scorer/src/data/embed.py)
- Added graph sampler with same-slot negatives and OfferService template subgraphs (ml/outfit_scorer/src/data/sampler.py)
- Added CSA + NGNN-lite dense outfit scorer with ONNX wrapper for the slot-based contract (ml/outfit_scorer/src/model/scorer.py)
- Added training with early stopping, test evaluation and baselines (ml/outfit_scorer/src/train.py, eval.py, baselines.py, evaluation.py)
- Added ONNX export with metadata and torch/onnxruntime parity check (ml/outfit_scorer/src/export_onnx.py, parity_check.py)
- Added tiny_scorer.onnx with random weights for .NET contract tests (ml/outfit_scorer/tests/fixtures)
- Added tests for sampler, model, metrics, ONNX, embedding parity with ParseImageService and end-to-end smoke (ml/outfit_scorer/tests)
- Changed dataset check to verify the actual HF layout: parquet images, splits, templates (ml/outfit_scorer/src/data/check_dataset.py)
- Added requirements and README with reproduction steps (ml/outfit_scorer)
- Added ml/.venv and ml __pycache__ to .gitignore (.gitignore)
- Added Polyvore reader for outfits, compatibility and FITB splits with OutfitSlot mapping (ml/outfit_scorer/src/data/polyvore.py)
- Added pytest setup and Polyvore reader tests (ml/outfit_scorer/tests)
- Added ML pipeline config with Polyvore semantic_category → OutfitSlot mapping (ml/outfit_scorer/config.yaml)
- Added Polyvore dataset inventory script for JSON/Parquet/archive layouts (ml/outfit_scorer/src/data/check_dataset.py)

## 2026-10-09
- Added ml/data/ to .gitignore for Polyvore dataset, embedding cache and checkpoints (.gitignore)

## 2026-10-04
- Changed frontend folder admin-portal to webui to match renamed compose service (frontend/webui, package.json, .gitignore)
- Fixed white text on light weather/season tags in outfit results (admin-portal/src/pages/outfits/outfitTheme.ts)
- Fixed outfit slot mapping to match actual YOLO-based Type dictionary in DB (OutfitOfferService appsettings.json, tests TestData)
- Added idempotent test wardrobe seed script with 25 items without photos (backend/Microservices/scripts/seed_test_wardrobe.sql)
- Changed .gitignore: ignore all **/.env except .env.example in a dedicated secrets section (.gitignore)
- Changed OutfitOfferService.Tests location to OutfitOfferService/tests and excluded tests from service build (OutfitOfferService.csproj, Microservices.sln)
- Added visual embedding backfill service with POST shelfs/items/embeddings/backfill endpoint and one-time startup run (ShelfsService)
- Added embedding self-healing on update-item when photo is kept but embedding is missing (UpdateItem.cs)
- Added DownloadPhotoAsync helper for ImageService photo stream (ShelfsService/Helpers)
- Fixed empty OpenWeatherApiKey treated as configured; 401 and empty key now raise WeatherUnavailableException (OpenWeatherClient.cs)
- Changed get-weather to return 503 with reason instead of 500 and warn on startup when weather key is missing (OutfitOfferService)
- Added OpenWeatherApiKey from .env to outfitofferservice, .env.example, and parseimageservice depends_on gateway (docker-compose.yml)
- Added gateway route for embeddings backfill (ocelot.json)
- Added offers API module with weather/generate types and Ukrainian enum labels (admin-portal/src/api/offers/offersApi.ts)
- Added authService.getUserId reading nameid claim from JWT (admin-portal/AuthService.ts)
- Added useWeather query and useGenerateOutfits mutation with 400/503 error notifications (admin-portal/src/hooks/offers)
- Added Outfit Offer wizard page /outfits with Context/Include/Exclude/Reference steps and results (admin-portal/src/pages/outfits)
- Added /outfits protected route and Outfits header link (admin-portal App.tsx, Header.tsx)
- Fixed HomePage.css import casing that broke build on case-sensitive Linux (admin-portal HomePage.tsx)
- Added admin-portal Dockerfile (vite build + nginx SPA) and adminportal service on port 5173 (docker-compose.yml)
- Fixed wardrobe page showing mock shelf instead of API data and item thumbnails not loaded via ImageService (ShelfsPage.tsx, ItemComponent.tsx)
- Changed outfit page text to white on dark background with light-text cards (admin-portal/src/pages/outfits)

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
