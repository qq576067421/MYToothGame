using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BoneSender
{
    /// <summary>
    /// 按 SDK demo 的节点命名自动补齐 PlayerTextuerShow 相关引用，
    /// 避免在 BoneSender 场景里手工拖拽大量字段。
    /// </summary>
    public static class BoneSenderPlayerTextureShowAutoBinder
    {
        private const string m_LogPrefix = "[BoneSender骨架显示自动补绑]";

        private static readonly BindingFlags m_InstanceNonPublicFlags =
            BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly FieldInfo m_CameraViewsField =
            typeof(AndroidTextureBridgeBase).GetField("cameraViews", m_InstanceNonPublicFlags);

        private static readonly FieldInfo m_PointTransformField =
            typeof(Playerskeleton).GetField("_pointTransform", m_InstanceNonPublicFlags);

        private static readonly FieldInfo m_LineTransformField =
            typeof(Playerskeleton).GetField("_lineTransform", m_InstanceNonPublicFlags);

        private static readonly FieldInfo m_PlayerInfoTextField =
            typeof(Playerskeleton).GetField("_playerInfoText", m_InstanceNonPublicFlags);

        private static readonly FieldInfo m_PlayerStateTextField =
            typeof(Playerskeleton).GetField("_playerStateText", m_InstanceNonPublicFlags);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BindAfterSceneLoad()
        {
            TryBindAllLoadedInstances(true);
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void BindInEditor()
        {
            EditorApplication.delayCall += TryBindAfterEditorReload;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void TryBindAfterEditorReload()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            TryBindAllLoadedInstances(false);
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            if (!scene.IsValid())
            {
                return;
            }

            TryBindAllLoadedInstances(false);
        }
#endif

        private static void TryBindAllLoadedInstances(bool writeSummaryLog)
        {
            PlayerTextuerShow[] allShows = Resources.FindObjectsOfTypeAll<PlayerTextuerShow>();
            int changedCount = 0;
            for (int i = 0; i < allShows.Length; i++)
            {
                PlayerTextuerShow targetShow = allShows[i];
                if (targetShow == null || !targetShow.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (TryBindSingle(targetShow))
                {
                    changedCount++;
                }
            }

            if (writeSummaryLog && changedCount > 0)
            {
                Debug.Log(m_LogPrefix + " 已自动补齐 " + changedCount + " 个场景实例的显示引用");
            }
        }

        private static bool TryBindSingle(PlayerTextuerShow targetShow)
        {
            bool changed = false;

            GameObject imagePoint = FindChildGameObject(targetShow.transform, "ImagePoint");
            GameObject imageLine = FindChildGameObject(targetShow.transform, "ImageLine");
            CameraTextureView backgroundView = FindChildComponent<CameraTextureView>(targetShow.transform, "CameraTextureViewRawImageBG");
            CameraTextureView personView1 = FindChildComponent<CameraTextureView>(targetShow.transform, "CameraImagViewForPerson1");
            CameraTextureView personView2 = FindChildComponent<CameraTextureView>(targetShow.transform, "CameraImagViewForPerson2");
            CameraTextureView personView3 = FindChildComponent<CameraTextureView>(targetShow.transform, "CameraImagViewForPerson3");
            CameraTextureView personView4 = FindChildComponent<CameraTextureView>(targetShow.transform, "CameraImagViewForPerson4");

            if (targetShow.rawPointPrefab != imagePoint)
            {
                targetShow.rawPointPrefab = imagePoint;
                changed = true;
            }

            if (targetShow.rawLinePrefab != imageLine)
            {
                targetShow.rawLinePrefab = imageLine;
                changed = true;
            }

            if (targetShow.regionLinePrefab != imageLine)
            {
                targetShow.regionLinePrefab = imageLine;
                changed = true;
            }

            if ((targetShow.Playerskeletons == null || targetShow.Playerskeletons.Length == 0))
            {
                Playerskeleton[] foundSkeletons = targetShow.GetComponentsInChildren<Playerskeleton>(true);
                if (foundSkeletons != null && foundSkeletons.Length > 0)
                {
                    targetShow.Playerskeletons = foundSkeletons;
                    changed = true;
                }
            }

            AndroidTextureBridgeBase bridge = targetShow.GetComponent<AndroidTextureBridgeBase>();
            if (bridge != null && m_CameraViewsField != null)
            {
                CameraTextureView[] targetViews =
                {
                    backgroundView,
                    personView1,
                    personView2,
                    personView3,
                    personView4,
                };

                CameraTextureView[] currentViews = m_CameraViewsField.GetValue(bridge) as CameraTextureView[];
                if (!AreObjectArraysEqual(currentViews, targetViews))
                {
                    m_CameraViewsField.SetValue(bridge, targetViews);
                    MarkDirty(bridge);
                    changed = true;
                }
            }

            Playerskeleton[] playerSkeletons = targetShow.Playerskeletons;
            if (playerSkeletons != null)
            {
                for (int i = 0; i < playerSkeletons.Length; i++)
                {
                    if (TryBindSkeleton(playerSkeletons[i]))
                    {
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                MarkDirty(targetShow);
                Debug.Log(m_LogPrefix + " 已补齐节点: " + targetShow.name);
            }

            return changed;
        }

        private static bool TryBindSkeleton(Playerskeleton targetSkeleton)
        {
            if (targetSkeleton == null)
            {
                return false;
            }

            Transform bodyPointRoot = FindChildTransform(targetSkeleton.transform, "PointTransform");
            Transform leftHandPointRoot = FindChildTransform(targetSkeleton.transform, "PointLeftHandTransform");
            Transform rightHandPointRoot = FindChildTransform(targetSkeleton.transform, "PointRightHandTransform");
            Transform bodyLineRoot = FindChildTransform(targetSkeleton.transform, "LineTransform");
            Transform leftHandLineRoot = FindChildTransform(targetSkeleton.transform, "LineLeftHandTransform");
            Transform rightHandLineRoot = FindChildTransform(targetSkeleton.transform, "LineRightHandTransform");
            Text infoText = FindChildComponent<Text>(targetSkeleton.transform, "TextInfo");
            Text stateText = FindChildComponent<Text>(targetSkeleton.transform, "TextState");

            bool changed = false;
            Transform[] pointRoots =
            {
                bodyPointRoot,
                leftHandPointRoot,
                rightHandPointRoot,
            };

            Transform[] lineRoots =
            {
                bodyLineRoot,
                leftHandLineRoot,
                rightHandLineRoot,
            };

            if (m_PointTransformField != null)
            {
                Transform[] currentPointRoots = m_PointTransformField.GetValue(targetSkeleton) as Transform[];
                if (!AreObjectArraysEqual(currentPointRoots, pointRoots))
                {
                    m_PointTransformField.SetValue(targetSkeleton, pointRoots);
                    changed = true;
                }
            }

            if (m_LineTransformField != null)
            {
                Transform[] currentLineRoots = m_LineTransformField.GetValue(targetSkeleton) as Transform[];
                if (!AreObjectArraysEqual(currentLineRoots, lineRoots))
                {
                    m_LineTransformField.SetValue(targetSkeleton, lineRoots);
                    changed = true;
                }
            }

            if (m_PlayerInfoTextField != null)
            {
                Text currentInfoText = m_PlayerInfoTextField.GetValue(targetSkeleton) as Text;
                if (currentInfoText != infoText)
                {
                    m_PlayerInfoTextField.SetValue(targetSkeleton, infoText);
                    changed = true;
                }
            }

            if (m_PlayerStateTextField != null)
            {
                Text currentStateText = m_PlayerStateTextField.GetValue(targetSkeleton) as Text;
                if (currentStateText != stateText)
                {
                    m_PlayerStateTextField.SetValue(targetSkeleton, stateText);
                    changed = true;
                }
            }

            if (changed)
            {
                MarkDirty(targetSkeleton);
            }

            return changed;
        }

        private static Transform FindChildTransform(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < allChildren.Length; i++)
            {
                Transform child = allChildren[i];
                if (child != null && child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static GameObject FindChildGameObject(Transform root, string childName)
        {
            Transform child = FindChildTransform(root, childName);
            return child != null ? child.gameObject : null;
        }

        private static T FindChildComponent<T>(Transform root, string childName) where T : Component
        {
            Transform child = FindChildTransform(root, childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static bool AreObjectArraysEqual<T>(T[] left, T[] right) where T : Object
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void MarkDirty(Object targetObject)
        {
#if UNITY_EDITOR
            if (targetObject == null)
            {
                return;
            }

            EditorUtility.SetDirty(targetObject);
            if (targetObject is Component component && component.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            }
            else if (targetObject is GameObject gameObject && gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }
    }
}
