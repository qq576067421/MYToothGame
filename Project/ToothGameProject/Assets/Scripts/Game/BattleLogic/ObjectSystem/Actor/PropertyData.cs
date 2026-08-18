using System;
using System.Collections.Generic;


namespace GameDll
{

    public class PropertyData
    {
        private Dictionary<int, float> m_PropertiesDict = new Dictionary<int, float>();

        private Dictionary<int, float> m_PropertiesDictV = new Dictionary<int, float>();

        
        public void Reset()
        {
            m_PropertiesDict.Clear();
            m_PropertiesDictV.Clear();
        }
        public bool ReadHasProperty(int type)
        {
            return m_PropertiesDict.ContainsKey(type);
        }
        public bool ReadHasPropertyV(int type)
        {
            return m_PropertiesDictV.ContainsKey(type);
        }
        public float ReadProperty(int type)
        {
            float value = 0;
            if (m_PropertiesDict.ContainsKey(type))
            {
                value = m_PropertiesDict[type];
            }
            return value;
        }
        public float ReadPropertyV(int type)
        {
            float value = 1.0f;
            if (m_PropertiesDictV.ContainsKey(type))
            {
                value = m_PropertiesDictV[type];
            }
            return value;
        }
        public void SetProperty(int type, float value)
        {
            if (m_PropertiesDict.ContainsKey(type))
            {
                m_PropertiesDict[type] = value;
            }
            else
            {
                m_PropertiesDict.Add(type, value);
            }
        }
        public void SetPropertyV(int type, float value)
        {
            value = 1.0f - value;
            if (m_PropertiesDictV.ContainsKey(type))
            {
                m_PropertiesDictV[type] = value;
            }
            else
            {
                m_PropertiesDictV.Add(type, value);
            }
        }
        public void AddProperty(int type, float value)
        {
            if (m_PropertiesDict.ContainsKey(type))
            {
                m_PropertiesDict[type] += value;
            }
            else
            {
                m_PropertiesDict.Add(type, value);
            }
        }

        public void AddPropertyV(int type, float value)
        {
            value = 1.0f - value;
            if (m_PropertiesDictV.ContainsKey(type))
            {
                var old_value = m_PropertiesDictV[type];
                var new_value = old_value * value;
                m_PropertiesDictV[type] = new_value;
            }
            else
            {
                m_PropertiesDictV.Add(type, value);
            }
        }
    }
}
