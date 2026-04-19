using System;
using System.IO;
using UnityEngine;

namespace Aquascape
{
    public readonly struct AquariumPaths
    {
        public AquariumPaths(string rootPath, string configPath, string runtimeImportsPath, string seedImportsPath)
        {
            RootPath = rootPath;
            ConfigPath = configPath;
            RuntimeImportsPath = runtimeImportsPath;
            SeedImportsPath = seedImportsPath;
        }

        public string RootPath { get; }
        public string ConfigPath { get; }
        public string RuntimeImportsPath { get; }
        public string SeedImportsPath { get; }
    }

    public static class AquariumPathResolver
    {
        public static string GetRootPath()
        {
            var parent = Directory.GetParent(Application.dataPath);
            return parent != null ? parent.FullName : Application.dataPath;
        }

        public static AquariumPaths Resolve(string rootPath, string folderName)
        {
            var normalizedFolderName = string.IsNullOrWhiteSpace(folderName) ? "RuntimeImports" : folderName.Trim();
            var configPath = Path.Combine(rootPath, "config.json");
            var runtimeImportsPath = Path.Combine(rootPath, normalizedFolderName);
            var seedImportsPath = Path.Combine(Application.streamingAssetsPath, "SeedImports");
            return new AquariumPaths(rootPath, configPath, runtimeImportsPath, seedImportsPath);
        }
    }

    [Serializable]
    public sealed class AquariumConfigData
    {
        public ScannerConfig scanner = new();
        public FoodConfig food = new();
        public InteractionConfig interaction = new();
        public FishConfigGroup fish = new();
        public TrashConfigGroup trash = new();

        public void Normalize()
        {
            scanner ??= new ScannerConfig();
            food ??= new FoodConfig();
            interaction ??= new InteractionConfig();
            fish ??= new FishConfigGroup();
            trash ??= new TrashConfigGroup();

            scanner.Normalize();
            food.Normalize();
            interaction.Normalize();
            fish.Normalize();
            trash.Normalize();
        }

        public FishTypeConfig GetFishProfile(string typeId) => fish.GetProfile(typeId);

        public TrashTypeConfig GetTrashProfile(string typeId) => trash.GetProfile(typeId);
    }

    [Serializable]
    public sealed class ScannerConfig
    {
        public string folderName = "RuntimeImports";
        public float pollIntervalSeconds = 0.5f;

        public void Normalize()
        {
            folderName = string.IsNullOrWhiteSpace(folderName) ? "RuntimeImports" : folderName.Trim();
            pollIntervalSeconds = Mathf.Max(0.2f, pollIntervalSeconds);
        }
    }

    [Serializable]
    public sealed class FoodConfig
    {
        public float lifetimeSeconds = 16f;
        public float scale = 0.22f;
        public float bobAmplitude = 0.08f;
        public float bobFrequency = 2.4f;

        public void Normalize()
        {
            lifetimeSeconds = Mathf.Max(3f, lifetimeSeconds);
            scale = Mathf.Max(0.08f, scale);
            bobAmplitude = Mathf.Max(0f, bobAmplitude);
            bobFrequency = Mathf.Max(0.1f, bobFrequency);
        }
    }

    [Serializable]
    public sealed class InteractionConfig
    {
        public float fleeDurationSeconds = 1.6f;
        public float fleeSpeedMultiplier = 1.85f;

        public void Normalize()
        {
            fleeDurationSeconds = Mathf.Max(0.2f, fleeDurationSeconds);
            fleeSpeedMultiplier = Mathf.Max(1f, fleeSpeedMultiplier);
        }
    }

    [Serializable]
    public sealed class FishConfigGroup
    {
        public FishTypeConfig @default = FishTypeConfig.CreateDefault();
        public FishTypeConfig[] types = Array.Empty<FishTypeConfig>();

        public void Normalize()
        {
            @default ??= FishTypeConfig.CreateDefault();
            @default.Normalize("DEFAULT");

            types ??= Array.Empty<FishTypeConfig>();
            for (var index = 0; index < types.Length; index++)
            {
                if (types[index] == null)
                {
                    types[index] = FishTypeConfig.CreateDefault();
                }

                types[index].Normalize($"TYPE_{index}");
            }
        }

        public FishTypeConfig GetProfile(string typeId)
        {
            var normalizedType = string.IsNullOrWhiteSpace(typeId) ? "DEFAULT" : typeId.Trim().ToUpperInvariant();
            var resolved = @default.Clone();
            resolved.type = normalizedType;

            for (var index = 0; index < types.Length; index++)
            {
                var candidate = types[index];
                if (!string.Equals(candidate.type, normalizedType, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                resolved.Merge(candidate);
                resolved.type = normalizedType;
                break;
            }

            return resolved;
        }
    }

    [Serializable]
    public sealed class TrashConfigGroup
    {
        public TrashTypeConfig @default = TrashTypeConfig.CreateDefault();
        public TrashTypeConfig[] types = Array.Empty<TrashTypeConfig>();

        public void Normalize()
        {
            @default ??= TrashTypeConfig.CreateDefault();
            @default.Normalize("DEFAULT");

            types ??= Array.Empty<TrashTypeConfig>();
            for (var index = 0; index < types.Length; index++)
            {
                if (types[index] == null)
                {
                    types[index] = TrashTypeConfig.CreateDefault();
                }

                types[index].Normalize($"TYPE_{index}");
            }
        }

        public TrashTypeConfig GetProfile(string typeId)
        {
            var normalizedType = string.IsNullOrWhiteSpace(typeId) ? "DEFAULT" : typeId.Trim().ToUpperInvariant();
            var resolved = @default.Clone();
            resolved.type = normalizedType;

            for (var index = 0; index < types.Length; index++)
            {
                var candidate = types[index];
                if (!string.Equals(candidate.type, normalizedType, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                resolved.Merge(candidate);
                resolved.type = normalizedType;
                break;
            }

            return resolved;
        }
    }

    [Serializable]
    public sealed class FishTypeConfig
    {
        public string type = "DEFAULT";
        public float minSpeed = 0.7f;
        public float maxSpeed = 1.4f;
        public float detectionRadius = 2.6f;
        public float hungerCooldown = 4.5f;
        public float hungerDrainPerSecond = 14f;
        public float scale = 0.92f;
        public float turnResponsiveness = 3.8f;
        public float wanderRetargetSeconds = 2.2f;
        public float avoidanceStrength = 1.25f;

        public static FishTypeConfig CreateDefault() => new();

        public FishTypeConfig Clone()
        {
            return new FishTypeConfig
            {
                type = type,
                minSpeed = minSpeed,
                maxSpeed = maxSpeed,
                detectionRadius = detectionRadius,
                hungerCooldown = hungerCooldown,
                hungerDrainPerSecond = hungerDrainPerSecond,
                scale = scale,
                turnResponsiveness = turnResponsiveness,
                wanderRetargetSeconds = wanderRetargetSeconds,
                avoidanceStrength = avoidanceStrength
            };
        }

        public void Merge(FishTypeConfig other)
        {
            if (other == null)
            {
                return;
            }

            type = string.IsNullOrWhiteSpace(other.type) ? type : other.type.Trim().ToUpperInvariant();
            minSpeed = other.minSpeed;
            maxSpeed = other.maxSpeed;
            detectionRadius = other.detectionRadius;
            hungerCooldown = other.hungerCooldown;
            hungerDrainPerSecond = other.hungerDrainPerSecond;
            scale = other.scale;
            turnResponsiveness = other.turnResponsiveness;
            wanderRetargetSeconds = other.wanderRetargetSeconds;
            avoidanceStrength = other.avoidanceStrength;
            Normalize(type);
        }

        public void Normalize(string fallbackType)
        {
            type = string.IsNullOrWhiteSpace(type) ? fallbackType : type.Trim().ToUpperInvariant();
            minSpeed = Mathf.Max(0.05f, minSpeed);
            maxSpeed = Mathf.Max(minSpeed, maxSpeed);
            detectionRadius = Mathf.Max(0.3f, detectionRadius);
            hungerCooldown = Mathf.Max(0.1f, hungerCooldown);
            hungerDrainPerSecond = Mathf.Max(0.1f, hungerDrainPerSecond);
            scale = Mathf.Max(0.2f, scale);
            turnResponsiveness = Mathf.Max(0.2f, turnResponsiveness);
            wanderRetargetSeconds = Mathf.Max(0.2f, wanderRetargetSeconds);
            avoidanceStrength = Mathf.Max(0f, avoidanceStrength);
        }
    }

    [Serializable]
    public sealed class TrashTypeConfig
    {
        public string type = "DEFAULT";
        public float minSpeed = 0.18f;
        public float maxSpeed = 0.6f;
        public float scale = 0.9f;
        public float swayAmplitude = 0.2f;
        public float swayFrequency = 1.1f;
        public float driftChangeInterval = 2.6f;
        public float avoidanceStrength = 0.8f;

        public static TrashTypeConfig CreateDefault() => new();

        public TrashTypeConfig Clone()
        {
            return new TrashTypeConfig
            {
                type = type,
                minSpeed = minSpeed,
                maxSpeed = maxSpeed,
                scale = scale,
                swayAmplitude = swayAmplitude,
                swayFrequency = swayFrequency,
                driftChangeInterval = driftChangeInterval,
                avoidanceStrength = avoidanceStrength
            };
        }

        public void Merge(TrashTypeConfig other)
        {
            if (other == null)
            {
                return;
            }

            type = string.IsNullOrWhiteSpace(other.type) ? type : other.type.Trim().ToUpperInvariant();
            minSpeed = other.minSpeed;
            maxSpeed = other.maxSpeed;
            scale = other.scale;
            swayAmplitude = other.swayAmplitude;
            swayFrequency = other.swayFrequency;
            driftChangeInterval = other.driftChangeInterval;
            avoidanceStrength = other.avoidanceStrength;
            Normalize(type);
        }

        public void Normalize(string fallbackType)
        {
            type = string.IsNullOrWhiteSpace(type) ? fallbackType : type.Trim().ToUpperInvariant();
            minSpeed = Mathf.Max(0.02f, minSpeed);
            maxSpeed = Mathf.Max(minSpeed, maxSpeed);
            scale = Mathf.Max(0.2f, scale);
            swayAmplitude = Mathf.Max(0f, swayAmplitude);
            swayFrequency = Mathf.Max(0.1f, swayFrequency);
            driftChangeInterval = Mathf.Max(0.2f, driftChangeInterval);
            avoidanceStrength = Mathf.Max(0f, avoidanceStrength);
        }
    }

    public static class AquariumConfigLoader
    {
        public static AquariumConfigData LoadOrCreate(string rootPath)
        {
            var configPath = Path.Combine(rootPath, "config.json");
            if (!File.Exists(configPath))
            {
                var generated = CreateDefault();
                Persist(configPath, generated);
                return generated;
            }

            try
            {
                var json = File.ReadAllText(configPath);
                var parsed = JsonUtility.FromJson<AquariumConfigData>(json);
                if (parsed == null)
                {
                    throw new InvalidDataException("config.json returned null.");
                }

                parsed.Normalize();
                return parsed;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to read config.json. Using defaults instead. Reason: {exception.Message}");
                var fallback = CreateDefault();
                Persist(configPath, fallback);
                return fallback;
            }
        }

        public static AquariumConfigData CreateDefault()
        {
            var config = new AquariumConfigData
            {
                scanner = new ScannerConfig
                {
                    folderName = "RuntimeImports",
                    pollIntervalSeconds = 0.5f
                },
                food = new FoodConfig
                {
                    lifetimeSeconds = 16f,
                    scale = 0.22f,
                    bobAmplitude = 0.08f,
                    bobFrequency = 2.4f
                },
                interaction = new InteractionConfig
                {
                    fleeDurationSeconds = 1.6f,
                    fleeSpeedMultiplier = 1.85f
                },
                fish = new FishConfigGroup
                {
                    @default = new FishTypeConfig
                    {
                        type = "DEFAULT",
                        minSpeed = 0.72f,
                        maxSpeed = 1.4f,
                        detectionRadius = 2.4f,
                        hungerCooldown = 4.8f,
                        hungerDrainPerSecond = 12f,
                        scale = 0.92f,
                        turnResponsiveness = 3.8f,
                        wanderRetargetSeconds = 2.4f,
                        avoidanceStrength = 1.2f
                    },
                    types = new[]
                    {
                        new FishTypeConfig
                        {
                            type = "ANGELFISH",
                            minSpeed = 0.7f,
                            maxSpeed = 1.28f,
                            detectionRadius = 2.7f,
                            hungerCooldown = 4.8f,
                            hungerDrainPerSecond = 11f,
                            scale = 0.92f,
                            turnResponsiveness = 3.5f,
                            wanderRetargetSeconds = 2.6f,
                            avoidanceStrength = 1.15f
                        },
                        new FishTypeConfig
                        {
                            type = "COD",
                            minSpeed = 0.95f,
                            maxSpeed = 1.7f,
                            detectionRadius = 2.2f,
                            hungerCooldown = 4.3f,
                            hungerDrainPerSecond = 14f,
                            scale = 1.05f,
                            turnResponsiveness = 4.6f,
                            wanderRetargetSeconds = 1.8f,
                            avoidanceStrength = 1.35f
                        },
                        new FishTypeConfig
                        {
                            type = "POMFRET",
                            minSpeed = 0.78f,
                            maxSpeed = 1.42f,
                            detectionRadius = 2.6f,
                            hungerCooldown = 5f,
                            hungerDrainPerSecond = 10.5f,
                            scale = 0.95f,
                            turnResponsiveness = 3.9f,
                            wanderRetargetSeconds = 2.4f,
                            avoidanceStrength = 1.18f
                        },
                        new FishTypeConfig
                        {
                            type = "SEAHORSE",
                            minSpeed = 0.48f,
                            maxSpeed = 0.95f,
                            detectionRadius = 3.1f,
                            hungerCooldown = 5.8f,
                            hungerDrainPerSecond = 9f,
                            scale = 0.88f,
                            turnResponsiveness = 2.8f,
                            wanderRetargetSeconds = 3.4f,
                            avoidanceStrength = 1.05f
                        },
                        new FishTypeConfig
                        {
                            type = "STARFISH",
                            minSpeed = 0.36f,
                            maxSpeed = 0.72f,
                            detectionRadius = 1.8f,
                            hungerCooldown = 6.4f,
                            hungerDrainPerSecond = 8.2f,
                            scale = 0.78f,
                            turnResponsiveness = 2.2f,
                            wanderRetargetSeconds = 3.8f,
                            avoidanceStrength = 0.9f
                        }
                    }
                },
                trash = new TrashConfigGroup
                {
                    @default = new TrashTypeConfig
                    {
                        type = "DEFAULT",
                        minSpeed = 0.22f,
                        maxSpeed = 0.52f,
                        scale = 0.88f,
                        swayAmplitude = 0.16f,
                        swayFrequency = 1.2f,
                        driftChangeInterval = 2.8f,
                        avoidanceStrength = 0.7f
                    },
                    types = new[]
                    {
                        new TrashTypeConfig
                        {
                            type = "CHIPS",
                            minSpeed = 0.22f,
                            maxSpeed = 0.58f,
                            scale = 0.78f,
                            swayAmplitude = 0.24f,
                            swayFrequency = 1.5f,
                            driftChangeInterval = 2.2f,
                            avoidanceStrength = 0.8f
                        },
                        new TrashTypeConfig
                        {
                            type = "LOG",
                            minSpeed = 0.12f,
                            maxSpeed = 0.36f,
                            scale = 1.08f,
                            swayAmplitude = 0.1f,
                            swayFrequency = 0.8f,
                            driftChangeInterval = 3.6f,
                            avoidanceStrength = 0.6f
                        }
                    }
                }
            };

            config.Normalize();
            return config;
        }

        private static void Persist(string path, AquariumConfigData config)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllText(path, JsonUtility.ToJson(config, true));
        }
    }
}
