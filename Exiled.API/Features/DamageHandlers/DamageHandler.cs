// -----------------------------------------------------------------------
// <copyright file="DamageHandler.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.API.Features.DamageHandlers
{
    using CustomPlayerEffects;

    using Footprinting;

    using PlayerStatsSystem;

    using UnityEngine;

    using BaseHandler = PlayerStatsSystem.DamageHandlerBase;

    /// <summary>
    /// A wrapper to easily manipulate the behavior of <see cref="BaseHandler"/>.
    /// </summary>
    public abstract class DamageHandler : DamageHandlerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DamageHandler"/> class.
        /// </summary>
        protected DamageHandler()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DamageHandler"/> class.
        /// </summary>
        /// <param name="target">The target to be set.</param>
        /// <param name="attacker">The attacker to be set.</param>
        protected DamageHandler(Player target, Player attacker)
        {
            Target = target;
            Attacker = attacker;
            TargetFootprint = target.Footprint;
            AttackerFootprint = attacker.Footprint;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DamageHandler"/> class.
        /// </summary>
        /// <param name="target">The target to be set.</param>
        /// <param name="baseHandler"><inheritdoc cref="DamageHandlerBase.Base"/></param>
        protected DamageHandler(Player target, BaseHandler baseHandler)
            : base(baseHandler)
        {
            Target = target;
            Attacker = baseHandler is PlayerStatsSystem.AttackerDamageHandler handler ? Player.Get(handler.Attacker.Hub) : null;
            TargetFootprint = target.Footprint;
            AttackerFootprint = Attacker?.Footprint ?? default;
        }

        /// <summary>
        /// Gets or sets the <see cref="Player"/> target.
        /// </summary>
        public Player Target { get; protected set; }

        /// <summary>
        /// Gets or sets the <see cref="Player"/> attacker. CAN BE NULL!.
        /// </summary>
        public Player Attacker { get; set; }

        /// <summary>
        /// Gets or sets the target's <see cref="Footprint"/>.
        /// </summary>
        public Footprint TargetFootprint { get; protected set; }

        /// <summary>
        /// Gets or sets the attacker's <see cref="Footprint"/>.
        /// </summary>
        public Footprint AttackerFootprint { get; protected set; }

        /// <summary>
        /// Gets or sets the amount of damage to be dealt.
        /// </summary>
        public virtual float Damage
        {
            get => SafeCast(out StandardDamageHandler handler) ? handler.Damage : 0f;
            set
            {
                if (SafeCast(out StandardDamageHandler handler))
                    handler.Damage = value;
            }
        }

        /// <summary>
        /// Gets or sets the start velocity.
        /// </summary>
        public Vector3 StartVelocity
        {
            get => SafeCast(out StandardDamageHandler handler) ? handler.StartVelocity : Vector3.zero;
            set
            {
                if (SafeCast(out StandardDamageHandler handler))
                    handler.StartVelocity = value;
            }
        }

        /// <summary>
        /// Gets or sets the direct damage.
        /// </summary>
        public float DealtHealthDamage
        {
            get => SafeCast(out StandardDamageHandler handler) ? handler.DealtHealthDamage : 0f;
            set
            {
                if (SafeCast(out StandardDamageHandler handler))
                    handler.DealtHealthDamage = value;
            }
        }

        /// <summary>
        /// Gets or sets the damage absorbed by AHP processes.
        /// </summary>
        public float AbsorbedAhpDamage
        {
            get => SafeCast(out StandardDamageHandler handler) ? handler.AbsorbedAhpDamage : 0f;
            set
            {
                if (SafeCast(out StandardDamageHandler handler))
                    handler.AbsorbedAhpDamage = value;
            }
        }

        /// <inheritdoc/>
        public override Action ApplyDamage(Player player)
        {
            if (!SafeCast(out StandardDamageHandler damageHandler))
                return player.GetModule<HealthStat>().CurValue > 0f ? Action.Damage : Action.Death;

            if (Damage <= 0f)
                return Action.None;

            StartVelocity = player.ReferenceHub.playerMovementSync.PlayerVelocity;
            Cast<StandardDamageHandler>().StartVelocity.y = Mathf.Max(damageHandler.StartVelocity.y, 0f);
            AhpStat ahpModule = player.GetModule<AhpStat>();
            HealthStat healthModule = player.GetModule<HealthStat>();

            if (Damage <= -1f)
            {
                ahpModule.CurValue = 0f;
                healthModule.CurValue = 0f;
                return Action.Death;
            }

            ProcessDamage(player);

            foreach (PlayerEffect effect in player.ActiveEffects)
            {
                if (effect is IDamageModifierEffect damageModifierEffect)
                    Damage *= damageModifierEffect.GetDamageModifier(Damage, damageHandler, damageHandler.Hitbox);
            }

            DealtHealthDamage = ahpModule.ServerProcessDamage(Damage);
            AbsorbedAhpDamage = Damage - DealtHealthDamage;
            healthModule.CurValue -= DealtHealthDamage;

            return player.GetModule<HealthStat>().CurValue > 0f ? Action.Damage : Action.Death;
        }

        /// <inheritdoc/>
        public override string ToString() => $"{Target} {Damage} ({Type}) {(Attacker != null ? Attacker.Nickname : "No one")}";
    }
}