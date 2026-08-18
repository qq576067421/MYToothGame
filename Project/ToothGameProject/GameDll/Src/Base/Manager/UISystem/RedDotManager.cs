using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameHot
{
    /// <summary>
    /// 全局红点管理器
    /// </summary>
    public class RedPointManager
    {
        private static RedPointManager m_Instance;
        public static RedPointManager GetInstance()
        {
            if(m_Instance == null)
            {
                m_Instance = new RedPointManager();
            }
            return m_Instance;
        } 
        private Dictionary<string, RedPoint> m_RedPointDict = new Dictionary<string, RedPoint>();

        /// <summary>
        /// 获取红点状态
        /// </summary>
        public bool GetRedPointValue(string path)
        {
            var redPoint = GetRedPoint(path);
            if (redPoint == null)
                return false;
            return redPoint.value;
        }


        /// <summary>
        /// 设置红点状态
        /// </summary>
        public void SetRedPointValue(string path, bool value)
        {
            var redPoint = GetRedPoint(path);
            if (redPoint == null)
                return;
            if (redPoint.childDict.Count > 0)
            {
                UnityEngine.Debug.LogErrorFormat($"只能设置最底层红点的状态,路径:{redPoint.path}");
                return;
            }
            redPoint.SetValue(value);
        }

        /// <summary>
        /// 移除红点及其子红点
        /// </summary>
        public void RemoveRedPoint(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            if (!m_RedPointDict.ContainsKey(path))
                return;
            var redPoint = m_RedPointDict[path];
            redPoint.Remove();
            redPoint = null;
        }

        /// <summary>
        /// 获取红点
        /// </summary>
        private RedPoint GetRedPoint(string path)
        {
            RedPoint red = null;
            if (m_RedPointDict.TryGetValue(path, out red))
                return red;
            return CreateRedPoint(path);
        }

        /// <summary>
        /// 创建红点
        /// </summary>
        private RedPoint CreateRedPoint(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                UDebug.LogError("红点路径不能为空！！！");
                return null;
            }

            try
            {
                string[] names = path.Split('/');
                RedPoint root = null;
                RedPoint last = null;
                if (m_RedPointDict.ContainsKey(names[0]))
                {
                    root = m_RedPointDict[names[0]];
                }
                else
                {
                    root = _CreateOneRedPoint(names[0]);
                }
                last = root;
                for (int i = 1; i < names.Length; ++i)
                {
                    var name = names[i];
                    var child = last.GetChild(name);
                    if (child != null)
                    {
                        last = child;
                        continue;
                    }
                    last = _CreateOneRedPoint(name, last);
                }
                return last;
            }
            catch (Exception ex)
            {
                UDebug.LogError($"创建红点失败！！！,{path}  {ex.Message}  {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 创建单个红点
        /// </summary>
        private RedPoint _CreateOneRedPoint(string name, RedPoint parent = null)
        {
            var redPoint = new RedPoint();
            redPoint.name = name;
            if (parent != null)
            {
                redPoint.parent = parent;
                parent.childDict[name] = redPoint;
                redPoint.path = string.Format("{0}/{1}", parent.path, name);
            }
            else
            {
                redPoint.path = name;
            }
            m_RedPointDict[redPoint.path] = redPoint;
            return redPoint;
        }
    }

    public class RedPoint
    {
        public string name;
        public string path;
        public bool value;
        public RedPoint parent = null;
        public Dictionary<string, RedPoint> childDict = new Dictionary<string, RedPoint>();

        public bool HasChild(string name)
        {
            return childDict.ContainsKey(name);
        }

        public RedPoint GetChild(string name)
        {
            RedPoint red = null;
            childDict.TryGetValue(name, out red);
            return red;
        }

        /// <summary>
        /// 设置红点状态
        /// </summary>
        public void SetValue(bool value)
        {
            if (this.value == value)
            {
                return;
            }
            this.value = value;
            if (CGameProcedure.Event.OnRedPointValueChange != null)
            {
                CGameProcedure.Event.OnRedPointValueChange(path, value);
            }
            if (parent == null)
                return;
            if (value && (!parent.value))
            {
                parent.SetValue(value);
            }
            else if ((!value) && parent.value)
            {
                parent.ChangeValueByChilds();
            }
        }

        /// <summary>
        /// 更新红点状态 by 子红点
        /// </summary>
        public void ChangeValueByChilds()
        {
            bool value = false;
            foreach (var rp in childDict)
            {
                if (rp.Value.value)
                {
                    value = true;
                    break;
                }
            }
            SetValue(value);
        }

        /// <summary>
        /// 移除红点及其子红点
        /// </summary>
        public void Remove()
        {
            if (CGameProcedure.Event.OnRedPointValueChange != null)
            {
                CGameProcedure.Event.OnRedPointValueChange(path, false);
            }
            foreach (var rp in childDict)
            {
                rp.Value.Remove();
            }
        }

        public override string ToString()
        {
            return string.Format("Name: {0}, Path: {1}, Value: {2}, Parent: {3}, ChildCount: {4}", name, path, value, parent.path, childDict.Count);
        }
    }
}