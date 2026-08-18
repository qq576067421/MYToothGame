using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameDll
{
    public class DataSaver
    {
        public class DataStream
        {
            public FileStream m_File = null;
            public BinaryWriter m_Writer = null;
            public BinaryReader m_Reader = null;
            public bool m_Drity;
            public float m_LastStartSaveTime = 0;
        }
        private static DataSaver m_Instance;
        public static DataSaver GetInstance()
        {
            if(m_Instance == null)
            {
                m_Instance = new DataSaver();
            }
            return m_Instance;
        }
        private Dictionary<int, DataStream> m_DataStreams = new Dictionary<int, DataStream>();
        private bool m_StartSaver = false;
        private float m_SaveInterval = 1.0f;
        private long m_PlayerId;
        public void SetPlayerId(long playerId)
        {
            m_PlayerId = playerId;
        }
        public void StartSaver(float saveInterval = 1.0f)
        {
            m_SaveInterval = saveInterval;
            m_StartSaver = true;
        }
        public BinaryWriter GetWriter(int msgId)
        {
            BinaryWriter hr_ = null;
            if(m_DataStreams.ContainsKey(msgId))
            {
                var steam = m_DataStreams[msgId];
                if(steam.m_Writer == null)
                {
                    steam.m_Writer = new BinaryWriter(steam.m_File);
                }

                hr_ = steam.m_Writer;
            }
            else
            {


                DataStream hr = new DataStream();
                var path = GetSavePath();
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                hr.m_File = new FileStream(path + m_PlayerId +"msg" + msgId, FileMode.OpenOrCreate);
                hr.m_Writer = new BinaryWriter(hr.m_File);

                m_DataStreams.Add(msgId, hr);

                int count = m_DataStreams.Count;
                if (count > 200)
                {
                    var keys = m_DataStreams.Keys.ToList();
                    for (int i = 0; i < count / 2; ++i)
                    {
                        var key = keys[i];
                        var writer = m_DataStreams[key];

                        if(writer.m_Reader != null)
                        {
                            writer.m_Reader.Close();
                        }
                        if(writer.m_Writer != null)
                        {
                            writer.m_Writer.Close();
                        }
                        writer.m_File.Close();
                        m_DataStreams.Remove(key);
                    }
                }

                hr_ = hr.m_Writer;
            }

            hr_.Seek(0, SeekOrigin.Begin);
            return hr_;
        }
        private string m_SavePath = "";
        public string GetSavePath()
        {
            if(string.IsNullOrEmpty(m_SavePath))
            {
                m_SavePath = LCL.MonoTool.GetPersistentPath() + "../save/";
            }
            return m_SavePath;
        }
        public BinaryReader GetReader(int msgId)
        {
            BinaryReader hr_ = null;
            if (m_DataStreams.ContainsKey(msgId))
            {
                var steam = m_DataStreams[msgId];
                if (steam.m_Reader == null)
                {
                    steam.m_Reader = new BinaryReader(steam.m_File);
                }

                hr_ = steam.m_Reader;
            }
            else
            {


                DataStream hr = new DataStream();
                var path = GetSavePath();
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                hr.m_File = new FileStream(path + m_PlayerId +"msg" + msgId, FileMode.OpenOrCreate);
                hr.m_Reader = new BinaryReader(hr.m_File);

                m_DataStreams.Add(msgId, hr);

                int count = m_DataStreams.Count;
                if (count > 200)
                {
                    var keys = m_DataStreams.Keys.ToList();
                    for (int i = 0; i < count / 2; ++i)
                    {
                        var key = keys[i];
                        var writer = m_DataStreams[key];

                        if (writer.m_Reader != null)
                        {
                            writer.m_Reader.Close();
                        }
                        if (writer.m_Writer != null)
                        {
                            writer.m_Writer.Close();
                        }
                        writer.m_File.Close();
                        m_DataStreams.Remove(key);
                    }
                }

                hr_ = hr.m_Reader;
            }
            hr_.BaseStream.Seek(0, SeekOrigin.Begin);
            return hr_;
        }

        public void Destroy()
        {
            m_StartSaver = false;
            SaveAll();

            foreach(var kv in m_DataStreams)
            {
                var stream = kv.Value;
                stream.m_File.Close();
            }
            m_DataStreams.Clear();
        }

        public void SaveReader(int msgId)
        {
            if (m_DataStreams.ContainsKey(msgId))
            {
                var data = m_DataStreams[msgId];
                if(!data.m_Drity)
                {
                    data.m_LastStartSaveTime = Time.realtimeSinceStartup;
                }
                data.m_Drity = true;
            }
        }

        public void Update()
        {
            if(!m_StartSaver)
            {
                return;
            }
            foreach(var kv in m_DataStreams)
            {
                var data = kv.Value;
                if(!data.m_Drity)
                {
                    continue;
                }
                if(m_SaveInterval < 0.5f)
                {
                    m_SaveInterval = 0.5f;
                }
                if(Time.realtimeSinceStartup - data.m_LastStartSaveTime > m_SaveInterval)
                {
                    data.m_Drity = false;
                         
                    if (data.m_Writer != null)
                    {
                        data.m_Writer.Flush();
                    }
                }
            }
        }

        private void SaveAll()
        {
            foreach (var kv in m_DataStreams)
            {
                var data = kv.Value;
                if (!data.m_Drity)
                {
                    continue;
                }

                data.m_Drity = false;

                if (data.m_Writer != null)
                {
                    data.m_Writer.Flush();
                }
                
            }
        }
    }
}
