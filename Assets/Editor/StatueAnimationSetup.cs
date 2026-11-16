#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChiecLopCuoi.EditorTools
{
    public static class StatueAnimationSetup
    {
        private const string AnimationFolder = "Assets/Animations";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Tools/Gameplay/Setup Statue Fall Animations")]
        public static void CreateAssetsAndWireScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            CreateAssetsAndWireCurrentScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[StatueAnimationSetup] Statue fall clips and controllers were created and assigned.");
        }

        public static void CreateAssetsAndWireCurrentScene()
        {
            if (!AssetDatabase.IsValidFolder(AnimationFolder))
                AssetDatabase.CreateFolder("Assets", "Animations");

            AnimationClip playerFall = CreateFallClip("Statue_Fall", -90f);
            AnimationClip enemyFall = CreateFallClip("Statue_Fall_Enemy", 90f);
            AnimatorController playerController = CreateController("Statue_Controller", playerFall);
            AnimatorController enemyController = CreateController("Statue_Enemy_Controller", enemyFall);

            ConfigureStatue("prop_statue_girl_0", playerController);
            ConfigureStatue("prop_statue_girl_gift_left_0", enemyController);
        }

        private static AnimationClip CreateFallClip(string clipName, float finalAngle)
        {
            string path = $"{AnimationFolder}/{clipName}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = clipName, frameRate = 60f };
                AssetDatabase.CreateAsset(clip, path);
            }

            var curve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(0.5f, finalAngle, 0f, 0f));
            clip.ClearCurves();
            clip.SetCurve(string.Empty, typeof(Transform), "localEulerAngles.z", curve);
            AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings
            {
                startTime = 0f,
                stopTime = 0.5f,
                loopTime = false,
                loopBlend = false
            });
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController CreateController(string controllerName, AnimationClip fallClip)
        {
            string path = $"{AnimationFolder}/{controllerName}.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            controller.parameters = new[]
            {
                new AnimatorControllerParameter
                {
                    name = "IsDestroyed",
                    type = AnimatorControllerParameterType.Bool,
                    defaultBool = false
                }
            };

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in stateMachine.states)
                stateMachine.RemoveState(child.state);

            AnimatorState idle = stateMachine.AddState("Idle", new Vector3(220f, 60f, 0f));
            AnimatorState fall = stateMachine.AddState("Fall", new Vector3(460f, 60f, 0f));
            fall.motion = fallClip;
            stateMachine.defaultState = idle;

            AnimatorStateTransition transition = idle.AddTransition(fall);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, "IsDestroyed");

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigureStatue(string objectName, RuntimeAnimatorController controller)
        {
            GameObject statue = GameObject.Find(objectName);
            if (statue == null)
                throw new MissingReferenceException($"Could not find statue '{objectName}' in {ScenePath}.");

            Animator animator = statue.GetComponent<Animator>();
            if (animator == null)
                animator = statue.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            if (statue.GetComponent<StatueController>() == null)
                statue.AddComponent<StatueController>();

            EditorUtility.SetDirty(statue);
        }
    }
}
#endif
