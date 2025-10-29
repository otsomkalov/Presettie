# Goal

Implement new RecommendationsEngine for Spotify provided recommendations.

# Scope

- `src/Domain` - core application models and logic
- `src/Telegram` - handlers for Messages and Button clicks
- `src/MusicPlatform.Spotify` - wrapper over Spotify library
- `src/Database` - database models
- `src/Infrastructure` - mapping logic from database to domain model

# Integrations details

- Use `ISpotifyClient` to get recommendations from Spotify

# Implementation details

- Use `IResourceProvider` for text resources during an implementation (message text and buttons labels)

# Plan

- Add new case to `RecommendationEngines` DU
- Make `IMusicPlaform` inherit `IRecommender`
- Implement `IRecommender` in `SpotifyMusicPlatform`
- Update dabase model to include value for enum `RecommendationEngines`
- Add mapping for `RecommendationEngines` DU to enum and vice versa
- Add new click handler to `src/Telegram/Handlers/Click.fs` for Spotify recommendations
- Update Preset message building in `src/Telegram/Workflows.fs` to include handling for new `RecommendationEngines`
- Implement `IRecommender` in `RedisMusicPlatform` and `MemoryCachedMusicPlatform` classes by calling `IMusicPlatform.Recommend`

# Expected output:
1. Source files are updated to include handling of new engine
2. `db/seed.json` file is update with text and button label for new engine