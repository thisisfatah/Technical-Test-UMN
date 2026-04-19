using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Aquascape.Editor
{
    [CustomEditor(typeof(AquascapeController))]
    public sealed class AquascapeControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Controller Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use the controller for aquarium layout, camera framing, runtime flow, and prefab hookups. Environment visuals are edited directly as normal scene objects under Environment.", MessageType.Info);

            var controller = (AquascapeController)target;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Preview"))
                {
                    Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Apply Aquascape Preview");
                    controller.ApplyFullEditorPreview();
                    MarkSceneDirty(controller);
                }

                if (GUILayout.Button("Sync Camera"))
                {
                    Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Sync Aquascape Camera");
                    controller.SyncCameraPreview();
                    MarkSceneDirty(controller);
                }
            }

            if (GUILayout.Button("Fit Camera To Aquarium"))
            {
                Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Fit Aquascape Camera");
                controller.FitCameraToAquariumHeight();
                MarkSceneDirty(controller);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void MarkSceneDirty(AquascapeController controller)
        {
            EditorUtility.SetDirty(controller);
            if (controller.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            }
        }
    }
}
