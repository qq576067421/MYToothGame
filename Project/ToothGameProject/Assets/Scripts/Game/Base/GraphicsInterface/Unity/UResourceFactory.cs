
using System;
using System.Collections.Generic;
using System.Text;

namespace GameDll
{
    public enum ResourceType
    {
        Mesh,
        Entity,
        PlayerActor,
        Actor,
        Tower,
        Effect,
        Bullet,
        Paodan,
        Laser,
        Sound,
        Item,
        PushCamp,
        ChessTower,
        Building,
        BoardRollerBuilding,
        BuildingWorker,
        TrapNeedle,
        TrapSpear,
        Door,
        Solider,
        BigWeapon,
        TrapStone,
    }
    public class UResourceFactory
    {
        public static UResource New_EntityObject(ResourceType type, emEntityType entityType)
        {
            UResource render = null;
            switch (type)
            {
                case ResourceType.Mesh:
                    {
                        render = new UEntity();
                        break;
                    }

                case ResourceType.Entity:
                    {
                        render = new UEntity();
                        break;
                    }
                case ResourceType.PlayerActor:
                    {
                        render = new UPlayerActor();
                        break;
                    }
                case ResourceType.Actor:
                case ResourceType.Solider:
                    {
                        render = new UActor();
                        break;
                    }
                case ResourceType.PushCamp:
                    {
                        render = new UActor();
                        break;
                    }
                case ResourceType.Bullet:
                    {
                        render = new UBullet();
                        break;
                    }
                case ResourceType.Effect:
                    {
                        render = new UEffect();
                        break;
                    }
                
                default:
                    return null;
            }
            if (render != null)
            {
                render.SetEntityType(entityType);
                render.Init();
            }
            return render;
        }
    }
}
