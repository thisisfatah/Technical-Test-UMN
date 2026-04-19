using UnityEngine;

namespace Aquascape
{
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        private Camera targetCamera;
        private AquariumWorld world;
        private SpawnService spawnService;

        public void Initialize(Camera sceneCamera, AquariumWorld aquariumWorld, SpawnService service)
        {
            targetCamera = sceneCamera;
            world = aquariumWorld;
            spawnService = service;
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0) || targetCamera == null || world == null || spawnService == null)
            {
                return;
            }

            var mousePosition = Input.mousePosition;
            var worldPosition = targetCamera.ScreenToWorldPoint(mousePosition);
            var clickPosition = new Vector2(worldPosition.x, worldPosition.y);
            if (!world.ContainsPoint(clickPosition))
            {
                return;
            }

            var hits = Physics2D.OverlapPointAll(clickPosition);
            for (var index = 0; index < hits.Length; index++)
            {
                var trash = hits[index].GetComponent<TrashAgent>();
                if (trash != null)
                {
                    trash.BeginCleanup();
                    return;
                }
            }

            for (var index = 0; index < hits.Length; index++)
            {
                var fish = hits[index].GetComponent<FishAgent>();
                if (fish != null)
                {
                    fish.TriggerFlee(clickPosition);
                    return;
                }
            }

            spawnService.SpawnFood(clickPosition);
        }
    }
}
