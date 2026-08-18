using UnityEngine;

namespace GameDll
{
    /// <summary>
    /// 创建并维护声音系统唯一的 <see cref="AudioListener"/>，使其跟随业务指定的目标位置和旋转。
    /// </summary>
    internal sealed class AudioListenerController
    {
        private AudioListener m_AudioListener;
        private Transform m_ListenerTransform;
        private Transform m_DefaultTarget;
        private Transform m_CurrentTarget;

        /// <summary>在声音运行时根节点下创建监听器对象。</summary>
        /// <param name="soundRoot">跨场景保留的声音运行时根节点。</param>
        public void Initialize(Transform soundRoot)
        {
            var listenerGo = new GameObject("AudioListener");
            listenerGo.transform.SetParent(soundRoot, false);
            m_AudioListener = listenerGo.AddComponent<AudioListener>();
            m_ListenerTransform = listenerGo.transform;
        }

        /// <summary>禁用监听器并清除全部目标和运行时引用。</summary>
        public void UnInit()
        {
            if (m_AudioListener != null)
            {
                m_AudioListener.enabled = false;
            }
            m_AudioListener = null;
            m_ListenerTransform = null;
            m_DefaultTarget = null;
            m_CurrentTarget = null;
        }

        /// <summary>设置没有临时目标时使用的默认跟随目标。</summary>
        /// <param name="target">默认目标；允许传入 <see langword="null"/>。</param>
        public void SetDefaultTarget(Transform target)
        {
            m_DefaultTarget = target;
            if (m_CurrentTarget == null)
            {
                SyncTransform();
            }
        }

        /// <summary>设置优先于默认目标的当前跟随目标。</summary>
        /// <param name="target">当前目标；传入 <see langword="null"/> 后恢复使用默认目标。</param>
        public void SetTarget(Transform target)
        {
            m_CurrentTarget = target;
            SyncTransform();
        }

        /// <summary>读取当前声音监听器的世界坐标。</summary>
        /// <returns>监听器已创建时返回其世界坐标；否则返回 <see cref="Vector3.zero"/>。</returns>
        public Vector3 ReadPosition()
        {
            return m_ListenerTransform != null ? m_ListenerTransform.position : Vector3.zero;
        }

        /// <summary>立即将监听器的位置和旋转同步到当前目标或默认目标。</summary>
        public void SyncTransform()
        {
            if (m_ListenerTransform == null)
            {
                return;
            }
            var target = m_CurrentTarget != null ? m_CurrentTarget : m_DefaultTarget;
            if (target != null)
            {
                m_ListenerTransform.SetPositionAndRotation(target.position, target.rotation);
            }
        }
    }
}
