﻿using Sandbox;
using System.Collections.Generic;

namespace PaintBall
{
	public abstract partial class BaseState : BaseNetworkable
	{
		[Net] public TimeUntil FreezeTime { get; protected set; }
		[Net] public int TimeLeftSeconds { get; set; }
		public virtual bool CanPlayerSuicide => false;
		public virtual int StateDuration => 0;
		public float StateEndTime { get; set; }
		public virtual string Name => GetType().Name;
		public virtual bool UpdateTimer => false;
		protected RealTimeUntil NextSecondTime { get; set; }
		protected static List<Player> Players = new();

		public float TimeLeft
		{
			get
			{
				return StateEndTime - Time.Now;
			}
		}

		public BaseState() { }

		public virtual void AddPlayer( Player player )
		{
			Host.AssertServer();

			Players.Add( player );
		}

		public virtual void OnPlayerJoin( Player player )
		{
			AddPlayer( player );

			if ( player.Client.IsBot )
			{
				if ( Team.Blue.GetCount() >= Team.Red.GetCount() )
					player.SetTeam( Team.Red );
				else
					player.SetTeam( Team.Blue );
			}
			else
			{
				player.MakeSpectator();
			}
		}

		public virtual void OnPlayerLeave( Player player )
		{
			Players.Remove( player );
		}

		public virtual void OnPlayerSpawned( Player player )
		{
			Game.Current?.MoveToSpawnpoint( player );
		}

		public virtual void OnPlayerKilled( Player player, Entity attacker, DamageInfo info ) { }

		public virtual void OnPlayerChangedTeam( Player player, Team oldTeam, Team newTeam )
		{
			if ( newTeam == Team.None )
			{
				player.MakeSpectator();

				return;
			}

			player.Respawn();
		}

		public virtual void OnSecond()
		{
			if ( Host.IsServer )
				TimeLeftSeconds = TimeLeft.CeilToInt();
		}

		public virtual void Tick()
		{
			if ( NextSecondTime <= 0 )
			{
				OnSecond();
				NextSecondTime = 1f;
			}
		}

		public virtual void Start()
		{
			if ( Host.IsServer && StateDuration > 0 )
			{
				Game.Current.CleanUp();
				StateEndTime = Time.Now + StateDuration;
			}
		}

		public virtual void Finish() { }
	}
}
