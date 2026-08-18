using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostingCtrl : MonoBehaviour
{
    public class GhostInfo
    {
        public GameObject snappedObject;
        public Mesh[] mesh;
        public float snapTime;
        public List<Material> m_Materials;
    }
    public enum SnapStyle
    {
        Interval = 1,
        Distance = 2,
        Code = 3
    }
    public GameObject m_Root;
    public float m_LifeTime = 1.0f;
    public int m_InitGhostsCount = 1;
    public int m_MaxGhostsCount = 5;
    public SnapStyle m_SnapStyle = SnapStyle.Interval;
    public bool m_OpenUseTexture = false;
    public bool m_OpenModifyVertexColor = false;

    public float m_SnapDistance = 0.2f;
    private Vector3 m_LastPosition;
    public float m_SnapInterval = 0.05f;
    private float m_TimeUsed = 0;

    private bool m_StartSnap = false;

    public Gradient m_ColorOverTimeline = new Gradient()
    {
        alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) },
        colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }
    };

    public Shader m_GhostShader;
    private List<SkinnedMeshRenderer> smrs;
    private GameObject m_GhostRootObject;
    private List<GhostInfo> ghosts = new List<GhostInfo>();
    Queue<GhostInfo> ghostPool = new Queue<GhostInfo>();
    public bool m_IsTest = false;
    public float m_TestDelay = 5.0f;
    private void Start()
    {
        if(m_IsTest)
        {
            if(m_TestDelay > 0)
            {
                StartCoroutine(DelayInit(m_TestDelay));
            }
            else
            {
                Init();
                StartSnap();
            }

        }
    }

    private IEnumerator DelayInit(float time)
    {
        yield return new WaitForSeconds(time);
        Init();
        StartSnap();
    }

    public void Init()
    {
        if(m_GhostShader == null)
        {
            return;
        }

        smrs = new List<SkinnedMeshRenderer>(m_Root.GetComponentsInChildren<SkinnedMeshRenderer>());
        if (m_GhostRootObject)
        {
            Destroy(m_GhostRootObject, m_LifeTime);
        }

        m_GhostRootObject = new GameObject("_LegacyGhosting:" + gameObject.name);
        m_GhostRootObject.layer = m_Root.layer;

        for (int i = 0; i < m_InitGhostsCount; i++)
        {
            CreateGhost();
        }
    }

    private void CreateGhost()
    {
        var gi = new GhostInfo();
        var go = new GameObject("_SnappedGhost");
        go.transform.parent = m_GhostRootObject.transform;
        go.transform.position = m_Root.transform.position;
        go.transform.eulerAngles = m_Root.transform.eulerAngles;
        go.transform.localScale = m_Root.transform.localScale;

        go.layer = m_GhostRootObject.layer;
        var giMeshList = new List<Mesh>();
        foreach (var smr in smrs)
        {

            bool use_texture = m_OpenUseTexture;
            Material[] mats = null;
            if (use_texture)
            {
                var m = smr.sharedMesh;
                mats = new Material[m.subMeshCount];
                for (int j = 0; j < m.subMeshCount; j++)
                {
                    var oriMat = smr.sharedMaterials[j];
                    mats[j] = new Material(oriMat);
                    if (m_GhostShader)
                    {
                        mats[j].shader = m_GhostShader;
                    }
                }
            }

            var child = new GameObject("_mesh:" + smr.name);
            child.transform.parent = go.transform;
            child.transform.position = smr.transform.position;
            child.transform.eulerAngles = smr.transform.eulerAngles;
            child.transform.localScale = smr.transform.lossyScale;

            child.layer = go.layer;
            var nmesh = new Mesh();
            nmesh.MarkDynamic();

            giMeshList.Add(child.AddComponent<MeshFilter>().sharedMesh = nmesh);
            if(use_texture)
            {
                child.AddComponent<MeshRenderer>().materials = mats;
                if (gi.m_Materials == null)
                {
                    gi.m_Materials = new List<Material>();
                }
                gi.m_Materials.AddRange(mats);
            }
            else
            {
                var mat = new Material(m_GhostShader);
                if(gi.m_Materials == null)
                {
                    gi.m_Materials = new List<Material>();
                }
                gi.m_Materials.Add(mat);
                child.AddComponent<MeshRenderer>().material = mat;
            }

        }
        gi.snappedObject = go;
        gi.mesh = giMeshList.ToArray();
        go.SetActive(false);
        ghostPool.Enqueue(gi);
    }


    public void StartSnap()
    {
        m_StartSnap = true;

        m_LastPosition = m_Root.transform.position;
        m_TimeUsed = 0;
    }
    public void PauseSnap()
    {
        m_StartSnap = false;
    }
    private void OnDestroy()
    {
        m_StartSnap = false;
        if (m_GhostRootObject)
        {
            Destroy(m_GhostRootObject);
            m_GhostRootObject = null;
        }
    }
    private GhostInfo m_LastGhost;
    public void SnapLegacyGhost()
    {

        GhostInfo gi = null;
        if (ghosts.Count >= m_MaxGhostsCount)
        {
            gi = ghosts[0];
            ghosts.RemoveAt(0);
            ghosts.Add(gi);
        }
        else
        {
            if(ghostPool.Count == 0)
            {
                CreateGhost();
                return;
            }
            gi = ghostPool.Dequeue();
            ghosts.Add(gi);
            gi.snappedObject.SetActive(true);
        }
        Snap(gi.mesh);
        var sot = gi.snappedObject.transform;
        var _transform = m_Root.transform;
        sot.position = _transform.position;
        sot.rotation = _transform.rotation;
        sot.localScale = _transform.localScale;
        gi.snapTime = Time.realtimeSinceStartup;

        m_LastGhost = gi;
    }


    Mesh Snap()
    {
        Mesh mesh = new Mesh();
        List<CombineInstance> combineList = new List<CombineInstance>();
        foreach (var smr in smrs)
        {

            Mesh m = new Mesh();
            smr.BakeMesh(m);

            for (int i = 0; i < m.subMeshCount; i++)
            {
                CombineInstance ci = new CombineInstance
                {
                    mesh = m,
                    subMeshIndex = i,
                    transform = smr.localToWorldMatrix
                };
                combineList.Add(ci);
            }
        }


        mesh.CombineMeshes(combineList.ToArray(), false, true);


        return mesh;
    }

    Mesh[] Snap(Mesh[] mesh)
    {
        for (int i = 0; i < smrs.Count; i++)
        {
            var smr = smrs[i];
            smr.BakeMesh(mesh[i]);
        }

        return mesh;
    }
    private void Update()
    {
        if(!m_StartSnap)
        {
            return;
        }

        if(m_SnapStyle == SnapStyle.Interval)
        {
            OnUpdateSnapByInterval();
        }
        else if(m_SnapStyle == SnapStyle.Distance)
        {
            OnUpdateSnapByDistance();
        }
    }

    private void OnUpdateSnapByDistance()
    {
        m_TimeUsed += Time.deltaTime;
        if (m_TimeUsed > m_SnapInterval)
        {
            m_TimeUsed = 0;

            var pos = m_Root.transform.position;
            var dist = Vector3.Distance(pos, m_LastPosition);
            if (dist > m_SnapDistance)
            {
                m_LastPosition = pos;
                SnapLegacyGhost();
            }
        }
    }

    private void OnUpdateSnapByInterval()
    {
        m_TimeUsed += Time.deltaTime;
        if(m_TimeUsed > m_SnapInterval)
        {
            m_TimeUsed = 0;
            SnapLegacyGhost();
        }

    }

    void LateUpdate()
    {
        FadingGhosts();
    }


    public void FadingGhosts()
    {
        int ghost_count = ghosts.Count;
        if (ghost_count <= 0)
        {
            return;
        }
        float cur_time = Time.realtimeSinceStartup;
        for(int i = ghost_count - 1; i >= 0; --i)
        {
            var gi = ghosts[i];
            if(cur_time - gi.snapTime > m_LifeTime)
            {
                ghostPool.Enqueue(gi);
                ghosts.RemoveAt(i);
                if(m_LastGhost == gi)
                {
                    m_LastGhost = null;
                }
            }
        }

        foreach (var gi in ghosts)
        {
            float timeLived = cur_time - gi.snapTime;
            float normalizedTime = Mathf.InverseLerp(0, m_LifeTime, timeLived);
            Color32 c = m_ColorOverTimeline.Evaluate(normalizedTime);

            if(m_OpenModifyVertexColor)
            {
                foreach (var m in gi.mesh)
                {
                    var clrs = m.colors32;
                    if (clrs == null || clrs.Length != m.vertexCount)
                    {
                        clrs = new Color32[m.vertexCount];
                    }
                    for (var i = 0; i < clrs.Length; i++)
                    {
                        clrs[i] = c;
                    }
                    m.colors32 = clrs;
                }
            }
            else
            {
                foreach (var m in gi.m_Materials)
                {
                    m.color = c;
                }
            }

        }


    }
}
