#if UNITY_EDITOR
using NUnit.Framework;

namespace Aquascape.Tests
{
    public sealed class RuntimeImportParserTests
    {
        [Test]
        public void TryParse_AcceptsFishPng()
        {
            var success = RuntimeImportParser.TryParse(
                @"C:\Temp\RuntimeImports\FISH_COD_20260401165920.png",
                out var descriptor,
                out var reason);

            Assert.That(success, Is.True, reason);
            Assert.That(descriptor.Kind, Is.EqualTo(RuntimeImportKind.Fish));
            Assert.That(descriptor.TypeId, Is.EqualTo("COD"));
            Assert.That(descriptor.Timestamp, Is.EqualTo("20260401165920"));
        }

        [Test]
        public void TryParse_RejectsInvalidPattern()
        {
            var success = RuntimeImportParser.TryParse(
                @"C:\Temp\RuntimeImports\COD_20260401165920.png",
                out _,
                out var reason);

            Assert.That(success, Is.False);
            Assert.That(reason, Does.Contain("Invalid naming format"));
        }

        [Test]
        public void DefaultConfig_FallsBackToDefaultProfileForUnknownFish()
        {
            var config = AquariumConfigLoader.CreateDefault();
            var resolved = config.GetFishProfile("UNKNOWN");

            Assert.That(resolved.type, Is.EqualTo("UNKNOWN"));
            Assert.That(resolved.minSpeed, Is.EqualTo(config.fish.@default.minSpeed));
            Assert.That(resolved.maxSpeed, Is.EqualTo(config.fish.@default.maxSpeed));
        }
    }
}
#endif
