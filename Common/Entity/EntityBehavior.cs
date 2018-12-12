﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common.Entities
{
    /// <summary>
    /// Defines a basic entity behavior that can be attached to entities
    /// </summary>
    public abstract class EntityBehavior
    {
        public Entity entity;

        public EntityBehavior(Entity entity)
        {
            this.entity = entity;
        }

        public virtual void Initialize(EntityProperties properties, JsonObject attributes)
        {
            
        }

        public virtual void OnGameTick(float deltaTime) { }

        public virtual void OnEntitySpawn() { }

        public virtual void OnEntityDespawn(EntityDespawnReason despawn) { }

        public abstract string PropertyName();

        public virtual void OnEntityReceiveDamage(DamageSource damageSource, float damage)
        {
            
        }

        public virtual void OnFallToGround(Vec3d lastTerrainContact, double withYMotion)
        {
        }

        
        public virtual void OnEntityReceiveSaturation(float saturation, EnumFoodCategory foodCat = EnumFoodCategory.Unknown, float saturationLossDelay = 10)
        {
            
        }

        public virtual void OnReceivedServerPos(bool isTeleport, ref EnumHandling handled)
        {
            
        }

        public virtual ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            handling = EnumHandling.NotHandled;

            return null;
        }

        public virtual void OnStateChanged(EnumEntityState beforeState, ref EnumHandling handling)
        {
            
        }

        /// <summary>
        /// The notify method bubbled up from entity.Notify()
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public virtual void Notify(string key, object data)
        {
            
        }

        public virtual void GetInfoText(StringBuilder infotext)
        {
            
        }

        public virtual void OnEntityDeath(DamageSource damageSourceForDeath)
        {

        }

        public virtual void OnInteract(EntityAgent byEntity, IItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            
        }

        public virtual void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled)
        {
            
        }
    }
}
