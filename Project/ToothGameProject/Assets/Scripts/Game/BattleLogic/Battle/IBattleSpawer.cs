using GameDll;
using System.Collections.Generic;
using UnityEngine;

namespace GameDll
{
    public abstract class IBattleSpawer
    {


        public abstract void OnCreate(IBattle battle);


        public abstract PropertyEntity ReadHero(int id);


        public abstract List<PropertyEntity> ReadHeroes();

 
        public abstract void OnLoadMap(int stage);

        public abstract List<PropertyEntity> ReadGuardHeroes();


        public abstract void Update(float dt);
        public abstract void OnRelease();

        public abstract int ReadWildWave();
        public abstract int ReadWave();

        public virtual int ReadMonsterCount()
        {
            return 0;
        }

        public virtual void OnSpawedUnit(PropertyEntity hero)
        {

        }
    }
}
