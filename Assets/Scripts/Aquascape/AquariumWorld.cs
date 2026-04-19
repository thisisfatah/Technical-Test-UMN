using System.Collections.Generic;
using UnityEngine;

namespace Aquascape
{
    public interface IAquariumOccupant
    {
        Vector2 Position { get; }
        float Radius { get; }
        bool BlocksMovement { get; }
    }

    public sealed class AquariumWorld : MonoBehaviour
    {
        private readonly List<IAquariumOccupant> occupants = new();
        private readonly List<FoodItem> foodItems = new();

        private Rect aquariumRect;
        private float spawnPadding;
        private int spawnAttempts;

        public Rect BoundsRect => aquariumRect;

        public int FishCount
        {
            get
            {
                var count = 0;
                for (var index = 0; index < occupants.Count; index++)
                {
                    if (occupants[index] is FishAgent)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int TrashCount
        {
            get
            {
                var count = 0;
                for (var index = 0; index < occupants.Count; index++)
                {
                    if (occupants[index] is TrashAgent)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int FoodCount => foodItems.Count;

        public void Initialize(Rect rect, float padding, int attempts)
        {
            aquariumRect = rect;
            spawnPadding = Mathf.Max(0f, padding);
            spawnAttempts = Mathf.Max(8, attempts);
        }

        public void RegisterOccupant(IAquariumOccupant occupant)
        {
            if (occupant != null && !occupants.Contains(occupant))
            {
                occupants.Add(occupant);
            }
        }

        public void UnregisterOccupant(IAquariumOccupant occupant)
        {
            if (occupant != null)
            {
                occupants.Remove(occupant);
            }
        }

        public void RegisterFood(FoodItem food)
        {
            if (food != null && !foodItems.Contains(food))
            {
                foodItems.Add(food);
            }
        }

        public void UnregisterFood(FoodItem food)
        {
            if (food != null)
            {
                foodItems.Remove(food);
            }
        }

        public bool TryFindSpawnPosition(float radius, out Vector2 position)
        {
            for (var attempt = 0; attempt < spawnAttempts; attempt++)
            {
                var candidate = GetRandomPoint(radius);
                if (IsAreaClear(candidate, radius, null))
                {
                    position = candidate;
                    return true;
                }
            }

            position = default;
            return false;
        }

        public bool IsAreaClear(Vector2 position, float radius, IAquariumOccupant ignoredOccupant)
        {
            if (!Contains(position, radius))
            {
                return false;
            }

            for (var index = 0; index < occupants.Count; index++)
            {
                var occupant = occupants[index];
                if (occupant == null || occupant == ignoredOccupant || !occupant.BlocksMovement)
                {
                    continue;
                }

                var combinedRadius = radius + occupant.Radius + spawnPadding;
                if ((occupant.Position - position).sqrMagnitude < combinedRadius * combinedRadius)
                {
                    return false;
                }
            }

            return true;
        }

        public Vector2 GetRandomPoint(float radius)
        {
            var min = aquariumRect.min + Vector2.one * radius;
            var max = aquariumRect.max - Vector2.one * radius;
            return new Vector2(
                Random.Range(min.x, max.x),
                Random.Range(min.y, max.y));
        }

        public Vector2 ClampInside(Vector2 position, float radius)
        {
            return new Vector2(
                Mathf.Clamp(position.x, aquariumRect.xMin + radius, aquariumRect.xMax - radius),
                Mathf.Clamp(position.y, aquariumRect.yMin + radius, aquariumRect.yMax - radius));
        }

        public bool Contains(Vector2 position, float radius)
        {
            return position.x >= aquariumRect.xMin + radius
                   && position.x <= aquariumRect.xMax - radius
                   && position.y >= aquariumRect.yMin + radius
                   && position.y <= aquariumRect.yMax - radius;
        }

        public bool ContainsPoint(Vector2 position)
        {
            return aquariumRect.Contains(position);
        }

        public Vector2 CalculateSeparation(Vector2 position, float radius, IAquariumOccupant self, float influenceDistance)
        {
            var total = Vector2.zero;
            for (var index = 0; index < occupants.Count; index++)
            {
                var occupant = occupants[index];
                if (occupant == null || occupant == self || !occupant.BlocksMovement)
                {
                    continue;
                }

                var offset = position - occupant.Position;
                var distance = offset.magnitude;
                var desiredDistance = radius + occupant.Radius + influenceDistance;
                if (distance <= 0.0001f || distance >= desiredDistance)
                {
                    continue;
                }

                var strength = 1f - (distance / desiredDistance);
                total += offset.normalized * strength;
            }

            return total;
        }

        public Vector2 CalculateBoundaryForce(Vector2 position, float radius)
        {
            var force = Vector2.zero;
            var leftDistance = position.x - (aquariumRect.xMin + radius);
            var rightDistance = (aquariumRect.xMax - radius) - position.x;
            var bottomDistance = position.y - (aquariumRect.yMin + radius);
            var topDistance = (aquariumRect.yMax - radius) - position.y;

            if (leftDistance < 1.2f)
            {
                force.x += 1f - Mathf.Clamp01(leftDistance / 1.2f);
            }

            if (rightDistance < 1.2f)
            {
                force.x -= 1f - Mathf.Clamp01(rightDistance / 1.2f);
            }

            if (bottomDistance < 1.2f)
            {
                force.y += 1f - Mathf.Clamp01(bottomDistance / 1.2f);
            }

            if (topDistance < 1.2f)
            {
                force.y -= 1f - Mathf.Clamp01(topDistance / 1.2f);
            }

            return force;
        }

        public FoodItem FindNearestFood(Vector2 position, float maxDistance)
        {
            var maxDistanceSqr = maxDistance * maxDistance;
            FoodItem nearest = null;
            var nearestDistanceSqr = maxDistanceSqr;

            for (var index = 0; index < foodItems.Count; index++)
            {
                var candidate = foodItems[index];
                if (candidate == null)
                {
                    continue;
                }

                var distanceSqr = (candidate.Position - position).sqrMagnitude;
                if (distanceSqr > nearestDistanceSqr)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistanceSqr = distanceSqr;
            }

            return nearest;
        }
    }
}
