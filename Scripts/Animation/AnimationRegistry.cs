using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Insanity.Scripts.Animation
{
    public sealed class AnimationRegistry
    {
        public sealed class AnimationVariant
        {
            public required string ActorId { get; init; }
            public required string StateName { get; init; }
            public required string VariantName { get; init; }
            public required IReadOnlyList<Texture2D> Frames { get; init; }
            public required float FramesPerSecond { get; init; }
            public required bool Loop { get; init; }
            public required string SourcePath { get; init; }

            public double Duration => Math.Max(0.1, Frames.Count / Math.Max(1.0f, FramesPerSecond));
        }

        public sealed class SoundVariant
        {
            public required string ActorId { get; init; }
            public required string EventName { get; init; }
            public required string VariantName { get; init; }
            public required AudioStream Stream { get; init; }
            public required string SourcePath { get; init; }
        }

        private sealed class ActorAssetLibrary
        {
            public required string ActorId { get; init; }
            public Dictionary<string, List<AnimationVariant>> StateVariants { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, List<SoundVariant>> EventVariants { get; } = new(StringComparer.Ordinal);
        }

        private const string CharactersRoot = "res://Assets/Characters";
        private static readonly Dictionary<string, ActorAssetLibrary> _actorCache = new(StringComparer.Ordinal);

        public IReadOnlyList<AnimationVariant> GetAnimationVariants(string actorId, string stateName)
        {
            ActorAssetLibrary library = EnsureActorLoaded(actorId);
            foreach (string candidate in AnimationStates.BuildFallbackChain(stateName))
            {
                if (library.StateVariants.TryGetValue(candidate, out List<AnimationVariant> variants) && variants.Count > 0)
                {
                    return variants;
                }
            }

            return Array.Empty<AnimationVariant>();
        }

        public IReadOnlyList<SoundVariant> GetSoundVariants(string actorId, string eventName)
        {
            ActorAssetLibrary library = EnsureActorLoaded(actorId);
            foreach (string candidate in AnimationEvents.BuildFallbackChain(eventName))
            {
                if (library.EventVariants.TryGetValue(candidate, out List<SoundVariant> variants) && variants.Count > 0)
                {
                    return variants;
                }
            }

            return Array.Empty<SoundVariant>();
        }

        public IReadOnlyList<string> GetKnownStates(string actorId)
        {
            ActorAssetLibrary library = EnsureActorLoaded(actorId);
            return library.StateVariants.Keys.OrderBy(state => state, StringComparer.Ordinal).ToArray();
        }

        public void ReloadActor(string actorId)
        {
            string normalized = NormalizeActorId(actorId);
            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            _actorCache.Remove(normalized);
            EnsureActorLoaded(normalized);
        }

        public void Clear()
        {
            _actorCache.Clear();
        }

        private static ActorAssetLibrary EnsureActorLoaded(string actorId)
        {
            string normalized = NormalizeActorId(actorId);
            if (string.IsNullOrEmpty(normalized))
            {
                return new ActorAssetLibrary { ActorId = string.Empty };
            }

            if (_actorCache.TryGetValue(normalized, out ActorAssetLibrary cached))
            {
                return cached;
            }

            ActorAssetLibrary scanned = ScanActor(normalized);
            _actorCache[normalized] = scanned;
            return scanned;
        }

        private static ActorAssetLibrary ScanActor(string actorId)
        {
            ActorAssetLibrary library = new()
            {
                ActorId = actorId,
            };

            string animationRoot = $"{CharactersRoot}/{actorId}/Animations";
            foreach (string stateDirPath in EnumerateDirectories(animationRoot))
            {
                string stateName = AnimationStates.Normalize(GetLeafName(stateDirPath));
                List<AnimationVariant> variants = LoadStateVariants(actorId, stateName, stateDirPath);
                if (variants.Count > 0)
                {
                    library.StateVariants[stateName] = variants;
                }
            }

            string soundRoot = $"{CharactersRoot}/{actorId}/Sounds";
            foreach (string eventDirPath in EnumerateDirectories(soundRoot))
            {
                string eventName = AnimationEvents.Normalize(GetLeafName(eventDirPath));
                List<SoundVariant> variants = LoadSoundVariants(actorId, eventName, eventDirPath);
                if (variants.Count > 0)
                {
                    library.EventVariants[eventName] = variants;
                }
            }

            return library;
        }

        private static List<AnimationVariant> LoadStateVariants(string actorId, string stateName, string stateDirPath)
        {
            List<AnimationVariant> variants = new();

            foreach (string variantDirPath in EnumerateDirectories(stateDirPath))
            {
                string variantName = NormalizeVariantName(GetLeafName(variantDirPath), stateName);
                AnimationVariant variant = BuildAnimationVariantFromDirectory(actorId, stateName, variantName, variantDirPath);
                if (variant != null)
                {
                    variants.Add(variant);
                }
            }

            Dictionary<string, List<string>> groupedRootFiles = new(StringComparer.Ordinal);
            foreach (string filePath in EnumerateFiles(stateDirPath))
            {
                if (!IsSupportedImage(filePath))
                {
                    continue;
                }

                string variantName = GetRootVariantName(stateName, filePath, out bool isDefaultFrameSequence);
                if (isDefaultFrameSequence)
                {
                    variantName = "default";
                }

                if (!groupedRootFiles.TryGetValue(variantName, out List<string> grouped))
                {
                    grouped = new List<string>();
                    groupedRootFiles[variantName] = grouped;
                }

                grouped.Add(filePath);
            }

            foreach ((string variantName, List<string> filePaths) in groupedRootFiles
                .OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                AnimationVariant variant = BuildAnimationVariantFromFiles(actorId, stateName, variantName, filePaths);
                if (variant != null)
                {
                    variants.Add(variant);
                }
            }

            return variants;
        }

        private static AnimationVariant BuildAnimationVariantFromDirectory(string actorId, string stateName, string variantName, string directoryPath)
        {
            List<string> imageFiles = EnumerateFiles(directoryPath)
                .Where(IsSupportedImage)
                .OrderBy(path => GetFrameSortKey(path), StringComparer.Ordinal)
                .ToList();

            return BuildAnimationVariantFromFiles(actorId, stateName, variantName, imageFiles, directoryPath);
        }

        private static AnimationVariant BuildAnimationVariantFromFiles(
            string actorId,
            string stateName,
            string variantName,
            IReadOnlyList<string> filePaths,
            string sourcePath = "")
        {
            List<Texture2D> frames = new();
            float? fpsOverride = null;

            foreach (string filePath in filePaths.OrderBy(path => GetFrameSortKey(path), StringComparer.Ordinal))
            {
                LoadImageFrames(filePath, frames, ref fpsOverride);
            }

            if (frames.Count == 0)
            {
                return null;
            }

            return new AnimationVariant
            {
                ActorId = actorId,
                StateName = stateName,
                VariantName = NormalizeVariantName(variantName, stateName),
                Frames = frames,
                FramesPerSecond = fpsOverride ?? EstimateFramesPerSecond(stateName, frames.Count),
                Loop = AnimationStates.IsLooping(stateName),
                SourcePath = string.IsNullOrEmpty(sourcePath) && filePaths.Count > 0 ? filePaths[0] : sourcePath,
            };
        }

        private static List<SoundVariant> LoadSoundVariants(string actorId, string eventName, string eventDirPath)
        {
            List<SoundVariant> variants = new();

            foreach (string filePath in EnumerateFiles(eventDirPath).OrderBy(path => path, StringComparer.Ordinal))
            {
                AudioStream stream = LoadAudioStream(filePath);
                if (stream == null)
                {
                    continue;
                }

                variants.Add(new SoundVariant
                {
                    ActorId = actorId,
                    EventName = eventName,
                    VariantName = NormalizeVariantName(Path.GetFileNameWithoutExtension(filePath), eventName),
                    Stream = stream,
                    SourcePath = filePath,
                });
            }

            return variants;
        }

        private static AudioStream LoadAudioStream(string resourcePath)
        {
            string extension = Path.GetExtension(resourcePath).ToLowerInvariant();
            if (extension == ".wav")
            {
                string globalPath = ProjectSettings.GlobalizePath(resourcePath);
                return File.Exists(globalPath) ? AudioStreamWav.LoadFromFile(globalPath) : null;
            }

            return extension == ".ogg"
                ? ResourceLoader.Load<AudioStream>(resourcePath)
                : null;
        }

        private static void LoadImageFrames(string resourcePath, List<Texture2D> frames, ref float? fpsOverride)
        {
            string globalPath = ProjectSettings.GlobalizePath(resourcePath);
            if (!File.Exists(globalPath))
            {
                return;
            }

            Image sourceImage = Image.LoadFromFile(globalPath);
            if (sourceImage == null || sourceImage.IsEmpty())
            {
                return;
            }

            string fileName = Path.GetFileNameWithoutExtension(resourcePath);
            ParseFileMetadata(fileName, out int columns, out int rows, out float? parsedFps);
            if (parsedFps.HasValue && !fpsOverride.HasValue)
            {
                fpsOverride = parsedFps;
            }

            if (columns == 1 && rows == 1)
            {
                frames.Add(ImageTexture.CreateFromImage(sourceImage));
                return;
            }

            int frameWidth = sourceImage.GetWidth() / columns;
            int frameHeight = sourceImage.GetHeight() / rows;
            if (frameWidth <= 0 || frameHeight <= 0)
            {
                frames.Add(ImageTexture.CreateFromImage(sourceImage));
                return;
            }

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    Image frameImage = Image.CreateEmpty(frameWidth, frameHeight, false, sourceImage.GetFormat());
                    frameImage.BlitRect(
                        sourceImage,
                        new Rect2I(column * frameWidth, row * frameHeight, frameWidth, frameHeight),
                        Vector2I.Zero
                    );
                    frames.Add(ImageTexture.CreateFromImage(frameImage));
                }
            }
        }

        private static string ParseFileMetadata(string fileName, out int columns, out int rows, out float? fpsOverride)
        {
            columns = 1;
            rows = 1;
            fpsOverride = null;

            string[] tokens = fileName.Split('@', StringSplitOptions.RemoveEmptyEntries);
            string baseName = tokens.Length == 0 ? fileName : tokens[0];

            for (int i = 1; i < tokens.Length; i++)
            {
                string token = tokens[i].Trim().ToLowerInvariant();
                if (TryParseGridToken(token, out int parsedColumns, out int parsedRows))
                {
                    columns = parsedColumns;
                    rows = parsedRows;
                    continue;
                }

                if (TryParseFpsToken(token, out float parsedFps))
                {
                    fpsOverride = parsedFps;
                }
            }

            return baseName;
        }

        private static bool TryParseGridToken(string token, out int columns, out int rows)
        {
            columns = 1;
            rows = 1;

            string[] parts = token.Split('x', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], out columns) ||
                !int.TryParse(parts[1], out rows))
            {
                return false;
            }

            return columns > 0 && rows > 0;
        }

        private static bool TryParseFpsToken(string token, out float fps)
        {
            fps = 0.0f;
            string digits = token.EndsWith("fps", StringComparison.Ordinal)
                ? token[..^3]
                : token;

            if (!float.TryParse(digits, out fps))
            {
                return false;
            }

            fps = Mathf.Max(1.0f, fps);
            return true;
        }

        private static bool IsSupportedImage(string resourcePath)
        {
            string extension = Path.GetExtension(resourcePath).ToLowerInvariant();
            return extension is ".png" or ".webp" or ".jpg" or ".jpeg";
        }

        private static string GetRootVariantName(string stateName, string filePath, out bool isDefaultFrameSequence)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string baseName = ParseFileMetadata(fileName, out _, out _, out _);
            string lowerBaseName = baseName.ToLowerInvariant();
            isDefaultFrameSequence = false;

            if (int.TryParse(lowerBaseName, out _))
            {
                isDefaultFrameSequence = true;
                return "default";
            }

            if (lowerBaseName.StartsWith("frame_", StringComparison.Ordinal) &&
                int.TryParse(lowerBaseName[6..], out _))
            {
                isDefaultFrameSequence = true;
                return "default";
            }

            string stateFramePrefix = $"{stateName}_frame_";
            if (lowerBaseName.StartsWith(stateFramePrefix, StringComparison.Ordinal) &&
                int.TryParse(lowerBaseName[stateFramePrefix.Length..], out _))
            {
                isDefaultFrameSequence = true;
                return "default";
            }

            int namedSequenceIndex = lowerBaseName.LastIndexOf("__", StringComparison.Ordinal);
            if (namedSequenceIndex > 0 && namedSequenceIndex < lowerBaseName.Length - 2)
            {
                string suffix = lowerBaseName[(namedSequenceIndex + 2)..];
                if (int.TryParse(suffix, out _))
                {
                    return NormalizeVariantName(baseName[..namedSequenceIndex], stateName);
                }
            }

            return NormalizeVariantName(baseName, stateName);
        }

        private static string GetFrameSortKey(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string baseName = ParseFileMetadata(fileName, out _, out _, out _).ToLowerInvariant();

            int namedSequenceIndex = baseName.LastIndexOf("__", StringComparison.Ordinal);
            if (namedSequenceIndex > 0 && namedSequenceIndex < baseName.Length - 2)
            {
                string suffix = baseName[(namedSequenceIndex + 2)..];
                if (int.TryParse(suffix, out int frameIndex))
                {
                    return $"{baseName[..namedSequenceIndex]}__{frameIndex:D6}";
                }
            }

            if (int.TryParse(baseName, out int numericOnly))
            {
                return $"default__{numericOnly:D6}";
            }

            if (baseName.StartsWith("frame_", StringComparison.Ordinal) &&
                int.TryParse(baseName[6..], out int frameNumber))
            {
                return $"default__{frameNumber:D6}";
            }

            return baseName;
        }

        private static string NormalizeVariantName(string variantName, string fallback)
        {
            if (string.IsNullOrWhiteSpace(variantName))
            {
                return fallback;
            }

            return variantName.Trim().ToLowerInvariant();
        }

        private static string NormalizeActorId(string actorId)
        {
            return actorId?.Trim() ?? string.Empty;
        }

        private static IEnumerable<string> EnumerateDirectories(string resourcePath)
        {
            return EnumerateEntries(resourcePath, directories: true);
        }

        private static IEnumerable<string> EnumerateFiles(string resourcePath)
        {
            return EnumerateEntries(resourcePath, directories: false);
        }

        private static IEnumerable<string> EnumerateEntries(string resourcePath, bool directories)
        {
            DirAccess dir = DirAccess.Open(resourcePath);
            if (dir == null)
            {
                return Array.Empty<string>();
            }

            List<string> entries = new();
            dir.ListDirBegin();
            while (true)
            {
                string next = dir.GetNext();
                if (string.IsNullOrEmpty(next))
                {
                    break;
                }

                if (next is "." or ".." || next.StartsWith(".", StringComparison.Ordinal))
                {
                    continue;
                }

                if (dir.CurrentIsDir() == directories)
                {
                    entries.Add($"{resourcePath}/{next}");
                }
            }
            dir.ListDirEnd();

            entries.Sort(StringComparer.Ordinal);
            return entries;
        }

        private static string GetLeafName(string resourcePath)
        {
            return resourcePath.TrimEnd('/').Split('/').LastOrDefault() ?? resourcePath;
        }

        private static float EstimateFramesPerSecond(string stateName, int frameCount)
        {
            if (frameCount <= 1)
            {
                return 6.0f;
            }

            return AnimationStates.Normalize(stateName) switch
            {
                AnimationStates.Run => 10.0f,
                AnimationStates.Dash => 14.0f,
                AnimationStates.WallSlide => 7.0f,
                AnimationStates.AttackMelee => 12.0f,
                AnimationStates.AttackRanged => 12.0f,
                AnimationStates.AttackCharge => 8.0f,
                AnimationStates.Hit => 10.0f,
                AnimationStates.Die => 8.0f,
                _ => 8.0f,
            };
        }
    }
}
