﻿using Sandbox;

namespace PaintBall
{
	[Library( "pb_smg", Title = "SMG", Spawnable = true )]
	[Hammer.EditorModel( "weapons/rust_smg/rust_smg.vmdl" )]
	public partial class SMG : ProjectileWeapon<BaseProjectile>
	{
		public override bool Automatic => true;
		public override int Bucket => 0;
		public override int ClipSize => 30;
		public override float Gravity => 7f;
		public override string Icon => "ui/weapons/smg.png";
		public override float PrimaryRate => 8f;
		public override float ReloadTime => 3f;
		public override float Speed => 2500f;
		public override string ViewModelPath => "weapons/rust_smg/v_rust_smg.vmdl";

		public override void Spawn()
		{
			base.Spawn();

			AmmoClip = ClipSize;
			ReserveAmmo = 60;

			SetModel( "weapons/rust_smg/rust_smg.vmdl" );
		}

		public override void SimulateAnimator( PawnAnimator anim )
		{
			anim.SetParam( "holdtype", 2 );
			anim.SetParam( "aimat_weight", 1.0f );
		}
	}
}
