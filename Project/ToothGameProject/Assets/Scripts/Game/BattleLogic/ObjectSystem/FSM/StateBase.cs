using System;
namespace GameDll
{
    public enum StateLevel
    {
        None = 1 << 0,
        Normal = 1 << 1,
        Trapped = 1 << 2,
        Dead = 1 << 3,

    }
    public class StateBase
    {
        public emEntityState m_StateType;
        public PropertyEntity m_Owner;

        public emEntityState GetStateType()
        {
            return m_StateType;
        }
        public void OnStart(emEntityState prevState)
        {
            this.Start(prevState);
        }
        public void OnUpdate(float dt)
        {
            this.Update(dt);
        }

        public void OnEnd(emEntityState nextState)
        {
            this.End(nextState);
        }
        protected virtual void Start(emEntityState prevState)
        {
        }
        protected virtual void Update(float dt)
        {
        }
        protected virtual void End(emEntityState nextState)
        {
        }

        
        public virtual StateLevel GetStateLevel()
        {
            return  StateLevel.Normal;
        }

        public virtual void OnChangedState()
        {

        }
        public virtual void Destroy()
        {

        }
    }
}
