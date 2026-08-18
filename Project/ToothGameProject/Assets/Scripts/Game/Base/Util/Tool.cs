using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.IO;
using LCL;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace GameDll
{
    public class Tool
    {
        public static Action<float> s_UpdateOnceFrame;
        public const float m_fToleranceValues = 0.001f;
        public static Guid NullGuid = Guid.Empty;
        public static StringBuilder StringBuilder = new StringBuilder();
        public static bool IsEqual(float a, float b)
        {
            return Mathf.Abs(a - b) <= m_fToleranceValues;
        }
        public static bool IsEqual(Vector3 a, Vector3 b)
        {
            return IsEqual(a.x, b.x) && IsEqual(a.y, b.y) && IsEqual(a.z, b.z);
        }
        public static bool IsEqualZero(float a)
        {
            return Mathf.Abs(a) <= m_fToleranceValues;
        }
        public static bool IsEqualZero(Vector3 a)
        {
            return IsEqualZero(a.x) && IsEqualZero(a.y) && IsEqualZero(a.z);
        }

        public static Vector3 ReadVector3(BinaryReader reader)
        {
            Vector3 v;
            v.x = reader.ReadSingle();
            v.y = reader.ReadSingle();
            v.z = reader.ReadSingle();
            return v;
        }
        public static Quaternion ReadQuaternion(BinaryReader reader)
        {
            Quaternion q;
            q.x = reader.ReadSingle();
            q.y = reader.ReadSingle();
            q.z = reader.ReadSingle();
            q.w = reader.ReadSingle();
            return q;
        }
        //所有子对象和他本身
        public static void SetLayerWithChild(GameObject parent, int layer)
        {
            parent.layer = layer;
            int count = parent.transform.childCount;
            for (int i = 0; i < count; ++i)
            {
                Transform child = parent.transform.GetChild(i);
                child.gameObject.layer = layer;
                SetLayerWithChild(child.gameObject, layer);
            }
        }
        public static void SetLayerWithChild(GameObject parent, GameLayer layer)
        {
            SetLayerWithChild(parent, (int)layer);
        }

        public static int[] splitStringToIntArray(string src, char sign = '+')
        {
            if (String.IsNullOrEmpty(src))
            {
                return null;
            }
            else
            {
                string[] strs = src.Split(sign);
                int[] ret = new int[strs.Length];
                for (int i = 0; i < strs.Length; i++)
                {
                    if (!int.TryParse(strs[i], out ret[i]))
                    {
                        UDebug.LogWarning("字符串转int出错！");
                        continue;
                    }
                }
                return ret;
            }
        }

        public static long[] splitStringToLongArray(string src, char sign = '+')
        {
            if (String.IsNullOrEmpty(src))
            {
                return null;
            }
            else
            {
                string[] strs = src.Split(sign);
                long[] ret = new long[strs.Length];
                for (int i = 0; i < strs.Length; i++)
                {
                    if (!long.TryParse(strs[i], out ret[i]))
                    {
                        UDebug.LogWarning("字符串转int出错！");
                        continue;
                    }
                }
                return ret;
            }
        }

        //考虑到int数组在js里面效率比较低，所以采用list
        public static bool ParseInts(List<int> iDatas, string str, char spliter = '+')
        {
            if (String.IsNullOrEmpty(str))
            {
                UDebug.LogWarning("ParseInts str is null or empty");
                return false;
            }
            string[] datas = str.Split(spliter);

            if (datas != null && datas.Length > 0)
            {
                int count = datas.Length;
                iDatas.Clear();

                for (int i = 0; i < count; ++i)
                {
                    int data = 0;
                    if (!int.TryParse(datas[i], out data))
                    {
                        UDebug.LogWarning("ParseInts error,datas[i] is not a number， 原始数据是：" + str);
                        break;
                    }
                    else
                    {
                        iDatas.Add(data);
                    }
                }
                return true;
            }
            else
            {
                UDebug.LogWarning("datas == null or datas.Legth<=0");
                return false;
            }
        }
        public static List<int> ParseInts(string str, char spliter = '+')
        {
            if (String.IsNullOrEmpty(str))
            {
                UDebug.LogWarning("ParseInts str is null or empty");
                return null;
            }
            string[] datas = str.Split(spliter);

            if (datas != null && datas.Length > 0)
            {
                int count = datas.Length;
                List<int> iDatas = new List<int>();

                for (int i = 0; i < count; ++i)
                {
                    int data = 0;
                    if (!int.TryParse(datas[i], out data))
                    {
                        UDebug.LogWarning("ParseInts error,datas[i] is not a number， 原始数据是：" + str);
                        break;
                    }
                    else
                    {
                        iDatas.Add(data);
                    }
                }
                return iDatas;
            }
            else
            {
                UDebug.LogWarning("datas == null or datas.Legth<=0");
                return null;
            }
        }

        public static bool ParseLongs(List<long> iDatas, string str, char spliter = '+')
        {
            if (String.IsNullOrEmpty(str))
            {
                UDebug.LogWarning("ParseInts str is null or empty");
                return false;
            }
            string[] datas = str.Split(spliter);

            if (datas != null && datas.Length > 0)
            {
                int count = datas.Length;
                iDatas.Clear();

                for (int i = 0; i < count; ++i)
                {
                    long data = 0;
                    if (!long.TryParse(datas[i], out data))
                    {
                        UDebug.LogWarning("ParseInts error,datas[i] is not a number， 原始数据是：" + str);
                        break;
                    }
                    else
                    {
                        iDatas.Add(data);
                    }
                }
                return true;
            }
            else
            {
                UDebug.LogWarning("datas == null or datas.Legth<=0");
                return false;
            }
        }

        public static List<long> ParseLongs(string str, char spliter = '+')
        {
            if (String.IsNullOrEmpty(str))
            {
                UDebug.LogWarning("ParseInts str is null or empty");
                return null;
            }
            string[] datas = str.Split(spliter);

            if (datas != null && datas.Length > 0)
            {
                int count = datas.Length;
                List<long> iDatas = new List<long>();

                for (int i = 0; i < count; ++i)
                {
                    long data = 0;
                    if (!long.TryParse(datas[i], out data))
                    {
                        UDebug.LogWarning("ParseLongs error,datas[i] is not a number， 原始数据是：" + str);
                        break;
                    }
                    else
                    {
                        iDatas.Add(data);
                    }
                }
                return iDatas;
            }
            else
            {
                UDebug.LogWarning("datas == null or datas.Legth<=0");
                return null;
            }
        }

        public static bool ParseStrings(List<string> iDatas, string str, char spliter = '+')
        {
            if (String.IsNullOrEmpty(str))
                return false;
            string[] datas = str.Split(spliter);
            if (datas != null && datas.Length > 0)
            {
                iDatas.Clear();
                iDatas.AddRange(datas);
                return true;
            }
            return false;
        }
        public static Vector3 ParseVector3(string str, char spliter = '+')
        {
            List<float> datas = ParseFloats(str, spliter);
            if (datas != null && datas.Count == 3)
            {
                Vector3 v = new Vector3(datas[0], datas[1], datas[2]);
                return v;
            }
            return Vector3.zero;
        }



        public static List<float> ParseFloats(string str, char spliter = '+')
        {
            string[] datas = str.Split(new char[] { spliter });

            if (datas != null && datas.Length > 0)
            {
                int count = datas.Length;
                List<float> iDatas = new List<float>();

                for (int i = 0; i < count; ++i)
                {
                    float data = 0;
                    if (!float.TryParse(datas[i], out data))
                    {
                        UDebug.LogWarning("ParseFloats error,datas[i] is not a float number， 原始数据是：" + str);
                        break;
                    }
                    else
                    {
                        iDatas.Add(data);
                    }
                }
                return iDatas;
            }
            return null;
        }
        public static float GetDistanceSqr(Vector3 v0, Vector3 v1)
        {
            Vector3 vd = v0 - v1;
            float mag = vd.sqrMagnitude;
            return mag;
        }
        //public static int GetDistance(Vector3 v0, Vector3 v1)
        //{

        //    Vector3 vd = v0 - v1;
        //    int mag = vd.magnitude;
        //    return mag;
        //}
        public static bool NoGreaterThan(Vector3 point0, Vector3 point1, float dis)
        {
            return GetDistanceSqr(point0, point1) <= dis * dis;
        }
        public static void Check(bool bError, string message = "")
        {
            if (!bError)
            {
                if (message != "")
                {
                    UDebug.LogError("Check Error:" + message);
                }
                else
                {
                    UDebug.LogError("Check Error");
                }


            }
        }
        public static void AIDebug(string message)
        {
            UDebug.Log(message);
        }
        public class bezieratParam
        {
            public float a;
            public float b;
            public float c;
            public float d;
            public float t;
        }
        public static float bezierat(bezieratParam param)
        {
            return (
                Mathf.Pow(1 - param.t, 3) * param.a +
                3 * param.t * (Mathf.Pow(1 - param.t, 2)) * param.b +
                3 * Mathf.Pow(param.t, 2) * (1 - param.t) * param.c +
                Mathf.Pow(param.t, 3) * param.d);
        }
        #region 坐标系转换(特别注意以下操作都是忽略了视口带来的影响，默认为视口大家和屏幕是一样的)
        //世界坐标转屏幕坐标
        public static Vector2 WorldToScreenPoint(Vector3 worldPos, Camera worldCamera)
        {
            return worldCamera.WorldToScreenPoint(worldPos);
        }
        //屏幕坐标转UI坐标
        public static Vector2 ScreenPointToUGUI(RectTransform uiParent, Vector2 screenPos, Camera uiCamera)
        {
            Vector2 retPos = Vector2.zero;
            try
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(uiParent, screenPos, uiCamera, out retPos);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            return retPos;
        }
        //UI坐标转屏幕坐标
        public static Vector2 UGUIToScreenPoint(RectTransform ui, Camera uiCamera)
        {
            return RectTransformUtility.WorldToScreenPoint(uiCamera, ui.position);
        }
        //uiParent 是转换过去的UI节点的父节点，也就是参考系
        public static Vector2 WorldToUGUIPoint(RectTransform uiParent, Vector3 worldPos, Camera worldCamera, Camera uiCamera)
        {
            Vector2 screen = WorldToScreenPoint(worldPos, worldCamera);
            return ScreenPointToUGUI(uiParent, screen, uiCamera);
        }
        //此算法是获取屏幕坐标和距离世界摄像机distFromCamera远距离的一个世界坐标，该坐标只能作为射线的参考
        public static Vector3 ScreenToWorldPoint(Vector2 screenPos, float distFromCamera, Camera worldCamera)
        {
            return worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distFromCamera));
        }

        public static Ray ScreenPointToRay(Vector3 screenPos, Camera camera)
        {
            return camera.ScreenPointToRay(screenPos);
        }
        public static List<RaycastHit> ScreenHitWorldObjs(Vector3 screenPos, Camera worldCamera)
        {
            Ray ray = ScreenPointToRay(screenPos, worldCamera);
            var hits = new List<RaycastHit>();
            hits.AddRange(Physics.RaycastAll(ray));
            SortHits(hits);
            return hits;
        }
        public static bool IsHitUI(Vector3 screenPos, int mask = 1 << 5)
        {
            return IsPointerOverGameObject(screenPos);
        }
        //UGUI 提供了一个检测是否点击在UI上的方法
        //EventSystem.current.IsPointerOverGameObject();
        //但是该方法在PC上检测正常，结果拿到Android真机测试上，永远检测不到。
        //方法一， 使用该方法的另一个重载方法，使用时给该方法传递一个整形参数
        // 该参数即使触摸手势的 id
        // int id = Input.GetTouch(0).fingerId;
        //public static bool IsPointerOverGameObject(int fingerID) {
        //    return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(fingerID);//移动输入模式下一样不行

        //}
        public static bool IsPointerOverGameObject()
        {
            //if (Input.touchCount > 0) {

            //    int id = Input.GetTouch(0).fingerId;
            //    return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(id);//安卓机上不行
            //}
            //else {
            //return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            PointerEventData eventData = new PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            eventData.pressPosition = InputSystemCompat.mousePosition;
            eventData.position = InputSystemCompat.mousePosition;

            List<RaycastResult> list = new List<RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, list);
            //UDebug.Log(list.Count);
            return list.Count > 0;
            // }
        }
        //方法二 通过UI事件发射射线
        //是 2D UI 的位置，非 3D 位置
        public static bool IsPointerOverGameObject(Vector2 screenPosition)
        {
            //实例化点击事件
            PointerEventData eventDataCurrentPosition = new PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            //将点击位置的屏幕坐标赋值给点击事件
            eventDataCurrentPosition.position = new Vector2(screenPosition.x, screenPosition.y);

            List<RaycastResult> results = new List<RaycastResult>();
            //向点击处发射射线
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

            return results.Count > 0;
        }
        public static bool IsPointerOverGameObject(float screen_x, float screen_y, GameObject obj)
        {
            //实例化点击事件
            PointerEventData eventDataCurrentPosition = new PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            //将点击位置的屏幕坐标赋值给点击事件
            eventDataCurrentPosition.position = new Vector2(screen_x, screen_y);

            List<RaycastResult> results = new List<RaycastResult>();
            //向点击处发射射线
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            int count = results.Count;
            for (var i = 0; i < count; ++i)
            {
                var res = results[i];
                if (res.gameObject == obj)
                {
                    return true;
                }
            }
            return false;
        }

        //检测UI
        public static List<RaycastResult> GetPointerOverUIGameObjects(Vector2 screenPosition)
        {
            //实例化点击事件
            PointerEventData eventDataCurrentPosition = new PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            //将点击位置的屏幕坐标赋值给点击事件
            eventDataCurrentPosition.position = new Vector2(screenPosition.x, screenPosition.y);

            List<RaycastResult> results = new List<RaycastResult>();
            //向点击处发射射线
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

            return results;
        }
        //方法三 通过画布上的 GraphicRaycaster 组件发射射线
        public bool IsPointerOverGameObject(Canvas canvas, Vector2 screenPosition)
        {
            //实例化点击事件
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            //将点击位置的屏幕坐标赋值给点击事件
            eventDataCurrentPosition.position = screenPosition;
            //获取画布上的 GraphicRaycaster 组件
            GraphicRaycaster uiRaycaster = canvas.gameObject.GetComponent<GraphicRaycaster>();

            List<RaycastResult> results = new List<RaycastResult>();
            // GraphicRaycaster 发射射线
            uiRaycaster.Raycast(eventDataCurrentPosition, results);

            return results.Count > 0;
        }

        private static void SortHits(List<RaycastHit> hits)
        {
            hits.Sort((ha, hb) =>
            {
                return ha.distance.CompareTo(hb.distance);
            });
        }
        public static List<RaycastHit> ScreenHitWorldObjs(Vector3 screenPos, Camera worldCamera, int mask)
        {
            Ray ray = ScreenPointToRay(screenPos, worldCamera);
            var hits = new List<RaycastHit>();
            hits.AddRange(Physics.RaycastAll(ray, float.MaxValue, mask));
            SortHits(hits);
            return hits;
        }
        //常用方法，其中鼠标坐标就是屏幕坐标（忽略视口因素）
        public static Vector3 ScreenHitWorldPos(Vector3 screenPos, Camera worldCamera, int mask)
        {
            Ray ray = ScreenPointToRay(screenPos, worldCamera);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, float.MaxValue, mask))
            {
                return hit.point;
            }
            else
            {
                return Vector3.zero;
            }
        }
        public static RaycastHit MouseHitWorldObj(Vector3 screenPos, Camera worldCamera, int mask)
        {
            Ray ray = ScreenPointToRay(screenPos, worldCamera);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, float.MaxValue, mask))
            {
                return hit;
            }
            else
            {
                return hit;
            }
        }
        #endregion


        public static void SetObjectVisible(GameObject gameObject, bool bVisible)
        {
            Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].enabled = bVisible;
            }
        }
        public static void SetObjectAlpha(GameObject gameObject, float fAlpha)
        {
            Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                for (int j = 0; j < componentsInChildren[i].materials.Length; j++)
                {
                    Color color = componentsInChildren[i].materials[j].color;
                    color.a = fAlpha;
                    if (color.a < 0f)
                    {
                        color.a = 0f;
                    }
                    componentsInChildren[i].materials[j].color = color;
                }
            }
        }
        public static Transform FindTransform(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform transform = FindTransform(parent.GetChild(i), name);
                if (transform != null)
                {
                    return transform;
                }
            }
            return null;
        }

        public static float GetBoundHigh(GameObject obj)
        {
            CapsuleCollider component = obj.GetComponent<CapsuleCollider>();
            if (component != null)
            {
                return component.height;
            }
            return 0f;
        }
        public static float DistanceWithBound(Vector3 pt1, float fBound1, Vector3 pt2, float fBound2)
        {
            float num = Vector3.Distance(pt1, pt2);
            num -= fBound1;
            num -= fBound2;
            return Mathf.Max(num, 0f);
        }

        public static void ChangeLayersRecursively(Transform transform, string name)
        {
            transform.gameObject.layer = LayerMask.NameToLayer(name);
            for (int i = 0; i < transform.childCount; i++)
            {
                ChangeLayersRecursively(transform.GetChild(i), name);
            }
        }
        public static GameObject AddChild(GameObject parent)
        {
            GameObject gameObject = new GameObject();
            if (parent != null)
            {
                Transform transform = gameObject.transform;
                transform.parent = parent.transform;
                ResetTransform(transform);
                gameObject.layer = parent.layer;
            }
            return gameObject;
        }
        public static GameObject AddChild(GameObject parent, GameObject prefab)
        {
            GameObject gameObject = (GameObject)UnityEngine.Object.Instantiate(prefab);
            AddChildImp(parent, gameObject);
            return gameObject;
        }

        public static void ResetTransform(Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
        public static void AddChildImp(GameObject parent, GameObject go)
        {
            if (go != null && parent != null)
            {
                Transform transform = go.transform;
                transform.SetParent(parent.transform);
                ResetTransform(transform);
                go.layer = parent.layer;
            }
        }
        public static GameObject GetGameObject(GameObject parent, string name)
        {
            if (parent == null)
            {
                return GameObject.Find(name);
            }
            Transform tr = parent.transform.Find(name);
            if (tr != null)
                return tr.gameObject;
            return null;
        }
        public static bool IsParent(GameObject _parent, GameObject _child)
        {
            Transform parent = _child.transform.parent;
            int maxLayer = 50;
            int layer = 0;
            while (parent != null)
            {
                if (layer >= maxLayer)
                {
                    break;
                }
                else
                {
                    layer += 1;
                }
                if (_parent.transform == parent)
                {
                    return true;
                }
                parent = parent.transform.parent;
            }
            return false;
        }

        public static Vector3 MaxVector3()
        {
            return new Vector3(10000, 10000, 10000);
        }

        //====================================
        ////数学相关
        //public static int GetDist(int2 fvPos1, int2 fvPos2)
        //{
        //    int2 v = fvPos1 - fvPos2;
        //    return v.magnitude;
        //}
        //public static float GetDist(Vector2 fvPos1, Vector2 fvPos2)
        //{
        //    Vector2 v = fvPos1 - fvPos2;
        //    return v.magnitude;
        //}
        //public static int GetDist(Vector3 fvPos1, Vector3 fvPos2)
        //{
        //    Vector3 v = fvPos1 - fvPos2;
        //    return v.magnitude;
        //}
        //public static int GetDist(int x1, int z1, int x2, int z2)
        //{
        //    return IntMath.Sqrt((x1 - x2) * (x1 - x2) + (z1 - z2) * (z1 - z2));
        //}


        //public static float GetDistSq(Vector2 fvPos1, Vector2 fvPos2)
        //{
        //    return (float)(fvPos1.x - fvPos2.x) * (fvPos1.x - fvPos2.x) + (fvPos1.y - fvPos2.y) * (fvPos1.y - fvPos2.y);
        //}
        //public static float GetDistSq(float x1, float z1, float x2, float z2)
        //{
        //    return (float)(x1 - x2) * (x1 - x2) + (z1 - z2) * (z1 - z2);
        //}
        //public static float GetDistSq(Vector3 fvPos1, Vector3 fvPos2)
        //{
        //    return (fvPos1.x - fvPos2.x) * (fvPos1.x - fvPos2.x) + (fvPos1.y - fvPos2.y) * (fvPos1.y - fvPos2.y) + (fvPos1.z - fvPos2.z) * (fvPos1.z - fvPos2.z);
        //}

        ////度转方向
        //public static Vector3 ConvertYAngleToDirection(float yAngle)
        //{
        //    yAngle = Mathf.Deg2Rad * yAngle;
        //    float z = Mathf.Cos(yAngle);
        //    float x = Mathf.Sin(yAngle);
        //    return new Vector3(x, 0, z);
        //}
        //public static float GetYAngle(Vector2 fvPos1, Vector2 fvPos2)
        //{
        //    return Vector2.Angle(fvPos1, fvPos2);
        //}

        //public static Vector3 GetCenter(Vector3 fvPos1, Vector3 fvPos2)
        //{
        //    Vector3 fvRet;
        //    fvRet.x = (fvPos1.x + fvPos2.x) / 2.0f;
        //    fvRet.y = (fvPos1.y + fvPos2.y) / 2.0f;
        //    fvRet.z = (fvPos1.z + fvPos2.z) / 2.0f;
        //    return fvRet;
        //}

        /// <summary>
        /// 获取角色当前移动方向
        /// </summary>
        /// <param name="Camera_Dir">角色参考相机的后方</param>
        /// <param name="UI_MoveDir">角色joystic移动的方向</param>
        public static Vector3 ConvertToRelatedCoord(float euler_y, float h, float v)
        {
            Vector3 move = new Vector3(h, 0, v);
            Quaternion q = Quaternion.Euler(0, euler_y, 0);
            Vector3 dir = q * move;
            return dir;
        }

        //把屏幕坐标转换成 ugui 坐标
        public static Vector2 ScreenPointToUIPoint(RectTransform tran, Vector2 screenPoint, Camera cam)
        {
            if (TransformTool.ScreenPointToLocalPointInRectangle(tran, screenPoint, cam))
            {
                return TransformTool.GetScreenPointToLocalPointInRectangleLocalPosition();
            }
            else
            {
                return Vector2.zero;
            }
        }

        /// <summary>
        /// 一个点绕另一个点旋转
        /// </summary>
        /// <param name="point">要旋转的点</param>
        /// <param name="pivot">中心点</param>
        /// <param name="euler">旋转的角度</param>
        /// <returns></returns>
        public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 euler)
        {
            Vector3 direction = point - pivot;
            Vector3 rotatedDirection = Quaternion.Euler(euler) * direction;
            Vector3 rotatedPoint = rotatedDirection + pivot;
            return rotatedPoint;
        }


        //得到unity位置可用的float最大值，因为unity对位置进行了特殊处理
        public const float FMaxValue = 10000.0f;

        public static float ConvertCM2M(int cm)
        {
            return (float)cm * 0.01f;
        }
        public static float ConvertMM2Second(int mm)
        {
            return (float)mm * 0.001f;
        }
        public static float Convert2Float(int num, float jindu = 0.001f)
        {
            return (float)num * jindu;
        }
        public static int Convert2Int(float value, int jindu = 1000)
        {
            return (int)(value * jindu);
        }
        public static Vector3 GetVector3(int x, int y, int z, float jindu = 0.001f)
        {
            float xf = Convert2Float(x, jindu);
            float yf = Convert2Float(y, jindu);
            float zf = Convert2Float(z, jindu);
            return new Vector3(xf, yf, zf);
        }
        public static string ConvertFloatString(float num, int pointCount = 2)
        {
            for (var i = 0; i < pointCount; ++i)
            {
                num = num * 10;
            }
            num = (float)Math.Round(num);
            for (var i = 0; i < pointCount; ++i)
            {
                num /= 10;
            }
            return num.ToString();
        }


        private static EventSystem s_EventSystem = null;
        public static EventSystem GetEventSystem()
        {
            if (s_EventSystem == null)
            {
                var obj = GameObject.Find("GlobalUI/EventSystem");
                if (obj != null)
                {
                    s_EventSystem = obj.GetComponent<EventSystem>();
                }
            }
            return s_EventSystem;
        }
        private static CanvasScaler s_CanvasScaler = null;
        public static CanvasScaler GetCanvasScaler()
        {
            if (s_CanvasScaler == null)
            {
                var obj = GameObject.Find("GlobalUI/GlobalCanvas");
                if (obj != null)
                {
                    s_CanvasScaler = obj.GetComponent<CanvasScaler>();
                }
            }
            return s_CanvasScaler;
        }


        public static string GetAssetName(string abName)
        {
            return System.IO.Path.GetFileNameWithoutExtension(abName);
        }

        public static void DrawCircle(Vector3 position, float radius, Color color)
        {
            float GIZMO_DISK_THICKNESS = 0.01f;
            Color oldColor = Gizmos.color;
            Gizmos.color = color;
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(position, Quaternion.identity, new Vector3(1, GIZMO_DISK_THICKNESS, 1));
            Gizmos.DrawWireSphere(Vector3.zero, radius);
            Gizmos.matrix = oldMatrix;
            Gizmos.color = oldColor;
        }

        public static void DrawWireCube(Vector3 position, float size, Color color)
        {
            Color oldColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawWireCube(position, Vector3.one * size);
            Gizmos.color = oldColor;
        }


        public static int[] GetRandomIndexes(int m)
        {
            System.Random _random = new System.Random();
            // Fisher-Yates洗牌算法优化版
            int[] indexes = new int[m];
            for (int i = 0; i < m; i++)
            {
                indexes[i] = i;
            }
            for (int i = m - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (indexes[i], indexes[j]) = (indexes[j], indexes[i]);
            }
            return indexes;
        }

        /// <summary>
        /// 计算曲线的点，这里是均匀的，和贝塞尔不一样
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="arcHeight"></param>
        /// <param name="segments"></param>
        /// <param name="isUp"></param>
        /// <returns></returns>
        public static List<Vector3> GetCircleAtStartEndPoint(Vector3 pointA, Vector3 end, 
            float arcHeight, int segments, bool isUp)
        {
            if (segments < 2)
            {
                return null;
            }
            if(isUp)
            {
                //这个是根据实际测试的
                if(pointA.x < end.x)
                {
                    arcHeight = -arcHeight;
                }
            }
            // 计算两点距离
            Vector3 direction = end - pointA;
            float distance = direction.magnitude;

            if (distance < 0.001f)
            {
                return null; // 两点重合
            }
            // 计算中点
            Vector3 center = (pointA + end) / 2f;

            // 计算垂直于连线方向的向量
            Vector3 perpendicular = Vector3.Cross(direction.normalized, Vector3.forward).normalized;

            // 弧线高度（限制最大高度以避免除零）
            float height = Mathf.Min(arcHeight, distance * 0.8f);

            // 圆弧中心点（使用几何公式确保起点和终点在圆弧上）
            Vector3 arcCenter;
            float arcRadius;
            List<Vector3> list = new List<Vector3>();
            if (Mathf.Abs(height) < 0.001f)
            {
                // 高度太小时使用直线
                Gizmos.color = Color.red;
                for (int i = 0; i <= segments; i++)
                {
                    float t = (float)i / segments;
                    Vector3 point = Vector3.Lerp(pointA, end, t);
                    list.Add(point);
                }
                return list;
            }

            // 使用圆弧几何公式计算中心和半径
            // 弦长L = distance, 弧高h = height
            // 半径R = (L² + 4h²) / (8h)
            // 圆心到弦中点的距离d = R - h
            arcRadius = (distance * distance + 4 * height * height) / (8 * Mathf.Abs(height));
            float centerToChordDistance = arcRadius - Mathf.Abs(height);

            // 圆弧中心位置
            if (height > 0)
            {
                arcCenter = center + perpendicular * centerToChordDistance;
            }
            else
            {
                arcCenter = center - perpendicular * centerToChordDistance;
            }

            // 验证起点和终点是否在圆弧上
            float startDist = Vector3.Distance(pointA, arcCenter);
            float endDist = Vector3.Distance(end, arcCenter);

            // 绘制圆弧点
            Gizmos.color = Color.red;

            // 计算起始和结束角度
            Vector3 toStart = pointA - arcCenter;
            Vector3 toEnd = end - arcCenter;
            float startAngle = Mathf.Atan2(toStart.y, toStart.x);
            float endAngle = Mathf.Atan2(toEnd.y, toEnd.x);

            // 确保沿着较短路径绘制圆弧
            float angleDiff = endAngle - startAngle;
            if (angleDiff > Mathf.PI)
            {
                endAngle -= 2 * Mathf.PI;
            }
            else if (angleDiff < -Mathf.PI)
            {
                endAngle += 2 * Mathf.PI;
            }

            // 绘制圆弧点，确保起点和终点准确
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float currentAngle = Mathf.Lerp(startAngle, endAngle, t);

                Vector3 arcPoint = arcCenter + new Vector3(
                    Mathf.Cos(currentAngle) * arcRadius,
                    Mathf.Sin(currentAngle) * arcRadius,
                    0
                );
                list.Add(arcPoint);
            }
            return list;
        }
    }
}
