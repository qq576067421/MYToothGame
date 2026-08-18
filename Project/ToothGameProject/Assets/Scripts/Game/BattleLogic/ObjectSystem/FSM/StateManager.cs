using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GameDll
{
    public class StateManager
    {
        protected StateBase m_State = null;
        protected emEntityState m_currentState = emEntityState.None;
        protected Dictionary<emEntityState, StateBase> m_statesDict = new Dictionary<emEntityState, StateBase>();
        private PropertyEntity m_Owner;
        private bool m_IsChangingState = false;
        public StateBase GetCurrentState()
        {
            return m_State;
        }
        public emEntityState GetCurrentStateType()
        {
            return m_currentState;
        }
        public StateBase GetState(emEntityState state)
        {
            StateBase s = null;
            m_statesDict.TryGetValue(state, out s);
            return s;
        }
        public bool ReadIsState(emEntityState state)
        {
            return m_currentState == state;
        }
        public void SetOwner(PropertyEntity owner)
        {
            m_Owner = owner;
        }
        public void AddState(emEntityState state, StateBase fsm)
        {
            if(m_Owner == null)
            {
                Debug.LogWarning("初始化m_Owner错误，添加AddState前必须先设置Owner");
            }
            fsm.m_Owner = m_Owner;
            fsm.m_StateType = state;
            this.m_statesDict.Add(state, fsm);
        }
        public void Destroy()
        {
            foreach(var kv in m_statesDict)
            {
                kv.Value.Destroy();
            }
            this.m_statesDict.Clear();
            m_Owner = null;
        }
        public void UpdateFSM(float dt)
        {
            if (this.m_State != null)
            {
                this.m_State.OnUpdate(dt);
            }
        }
        public bool ChangeState(emEntityState nextStateType)
        {
            if(m_currentState == nextStateType)
            {
                //不能切换到相同的状态
                return false;
            }

            //上一次切换尚未结束
            if(m_IsChangingState)
            {
                Debug.LogWarning("state change error, check track");
                return false;
            }
            if(m_Owner == null)
            {
                return false;
            }
            var nextState = GetState(nextStateType);
            if(nextState == null)
            {
                return false;
            }
            //不允许在离开和进入状态的时候切状态
            m_IsChangingState = true;
            emEntityState prevState = emEntityState.None;
            
            if (this.m_State != null)
            {
                prevState = this.m_State.m_StateType;
                this.m_State.OnEnd(nextStateType);
            }

            this.m_State = nextState;
            this.m_currentState = nextStateType;
            nextState.OnStart(prevState);

            //状态切换完毕
            m_IsChangingState = false;
            nextState.OnChangedState();
            return true;
        }

    }
}
