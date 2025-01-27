﻿using Hammer;
using Sandbox;
using System;

namespace PaintBall
{
	[Skip]
	public abstract partial class Weapon : BaseWeapon
	{
		[Net, Predicted] public int AmmoClip { get; set; }
		[Net, Predicted] public bool IsReloading { get; protected set; }
		[Net, Predicted] public int ReserveAmmo { get; protected set; }
		[Net, Predicted] public TimeSince TimeSinceDeployed { get; protected set; }
		[Net, Predicted] public TimeSince TimeSinceReload { get; protected set; }
		public virtual bool Automatic => false;
		public virtual int Bucket => 0;
		public virtual int ClipSize => 20;
		public virtual string CrosshairClass => "standard";
		public virtual bool Droppable => true;
		public virtual string FireSound => "pbg";
		public virtual string Icon => "ui/weapons/pistol.png";
		public virtual bool IsMelee => false;
		public virtual string ModelPath => "";
		public PickupTrigger PickupTrigger { get; protected set; }
		public Entity PreviousOwner { get; private set; }
		public virtual float ReloadTime => 5f;
		public virtual float Spread => 0f;
		public TimeSince TimeSinceDropped { get; private set; }
		public virtual bool UnlimitedAmmo => false;

		public Weapon() { }

		public override void Spawn()
		{
			base.Spawn();

			PickupTrigger = new PickupTrigger
			{
				Parent = this,
				Position = Position
			};

			PickupTrigger.PhysicsBody.EnableAutoSleeping = false;
		}

		public override void ClientSpawn()
		{
			base.ClientSpawn();

			if ( Local.Pawn is not Player player )
				return;

			if ( !IsLocalPawn && IsActiveChild() )
			{
				CreateViewModel();
				CreateHudElements();

				if ( player.LifeState == LifeState.Alive || player.CurrentPlayer != Owner )
					ViewModelEntity.EnableDrawing = false;
			}
		}

		public override void ActiveStart( Entity entity )
		{
			base.ActiveStart( entity );

			TimeSinceDeployed = 0;
			IsReloading = false;

			if ( IsServer )
				OnActiveStartClient( To.Everyone );
		}

		public override void ActiveEnd( Entity ent, bool dropped )
		{
			base.ActiveEnd( ent, dropped );

			if ( IsServer )
				OnActiveEndClient( To.Everyone );
		}

		public override void Simulate( Client owner )
		{
			if ( TimeSinceDeployed < 0.6f )
				return;

			if ( !IsReloading )
				base.Simulate( owner );

			if ( IsReloading && TimeSinceReload > ReloadTime )
				OnReloadFinish();
		}

		public override bool CanPrimaryAttack()
		{
			if ( !Game.Current.State.FreezeTime )
				return false;

			if ( Automatic == false && !Input.Pressed( InputButton.Attack1 ) )
				return false;
			else if ( Automatic == true && !Input.Down( InputButton.Attack1 ) )
				return false;

			var rate = PrimaryRate;
			if ( rate <= 0 )
				return true;

			return TimeSincePrimaryAttack > (1 / rate);
		}

		public override bool CanSecondaryAttack()
		{
			if ( !Game.Current.State.FreezeTime )
				return false;

			if ( !Input.Pressed( InputButton.Attack2 ) )
				return false;

			var rate = SecondaryRate;
			if ( rate <= 0 )
				return true;

			return TimeSinceSecondaryAttack > (1 / rate);
		}

		public override void Reload()
		{
			if ( IsReloading )
				return;

			TimeSinceReload = 0;
			IsReloading = true;

			(Owner as AnimEntity).SetAnimBool( "b_reload", true );

			ReloadEffects();
		}

		public override bool CanReload()
		{
			if ( AmmoClip >= ClipSize || (!UnlimitedAmmo && ReserveAmmo == 0) )
				return false;

			return base.CanReload();
		}

		public override void CreateViewModel()
		{
			Host.AssertClient();

			if ( string.IsNullOrEmpty( ViewModelPath ) )
				return;

			ViewModelEntity = new ViewModel
			{
				Position = Position,
				Owner = Owner,
				EnableViewmodelRendering = true
			};

			ViewModelEntity.FieldOfView = 70;
			ViewModelEntity.SetModel( ViewModelPath );
		}

		public override void CreateHudElements()
		{
			if ( Local.Hud == null ) return;

			CrosshairPanel = new Crosshair
			{
				Parent = Local.Hud,
				TargetWeapon = this
			};

			CrosshairPanel.AddClass( CrosshairClass );
		}

		public virtual void OnReloadFinish()
		{
			IsReloading = false;
			AmmoClip += TakeAmmo( ClipSize - AmmoClip );
		}

		public override void OnCarryStart( Entity carrier )
		{
			base.OnCarryStart( carrier );

			if ( PickupTrigger.IsValid() )
				PickupTrigger.EnableTouch = false;

			PreviousOwner = Owner;
		}

		public override void OnCarryDrop( Entity dropper )
		{
			base.OnCarryDrop( dropper );

			if ( PickupTrigger.IsValid() )
				PickupTrigger.EnableTouch = true;

			TimeSinceDropped = 0f;

			OnActiveEndClient( To.Everyone );
		}

		public void Remove()
		{
			PhysicsGroup?.Wake();
			Delete();
		}

		[ClientRpc]
		protected virtual void ReloadEffects()
		{
			ViewModelEntity?.SetAnimBool( "reload", true );
		}

		[ClientRpc]
		protected void OnActiveStartClient()
		{
			if ( IsLocalPawn || ViewModelEntity != null )
				return;

			CreateViewModel();

			if ( (Local.Pawn as Player).CurrentPlayer != Owner )
				ViewModelEntity.EnableDrawing = false;

			ViewModelEntity?.SetAnimBool( "deploy", true );
		}


		[ClientRpc]
		protected void OnActiveEndClient()
		{
			if ( IsLocalPawn )
				return;

			DestroyHudElements();
			DestroyViewModel();
		}

		[ClientRpc]
		protected virtual void ShootEffects()
		{
			Host.AssertClient();

			// Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );

			if ( IsLocalPawn )
				_ = new Sandbox.ScreenShake.Perlin( 1f, 0.2f, 0.8f );

			ViewModelEntity?.SetAnimBool( "fire", true );
			CrosshairPanel?.CreateEvent( "fire" );
		}

		[ClientRpc]
		protected virtual void StartReloadEffects()
		{
			ViewModelEntity?.SetAnimBool( "reload", true );
		}

		public void DealDamage( Entity entity, Entity attacker, Vector3 position, Vector3 force, int hitbox )
		{
			var info = new DamageInfo()
				.WithAttacker( attacker )
				.WithWeapon( this )
				.WithPosition( position )
				.WithForce( force )
				.WithHitbox( hitbox );

			info.Damage = float.MaxValue;

			entity.TakeDamage( info );
		}

		protected virtual int TakeAmmo( int ammo )
		{
			if ( UnlimitedAmmo )
				return ammo;

			int available = Math.Min( ReserveAmmo, ammo );
			ReserveAmmo -= available;

			return available;
		}
	}
}
