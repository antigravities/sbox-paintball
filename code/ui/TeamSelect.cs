﻿using Sandbox;
using Sandbox.UI;
using System;

namespace PaintBall
{
	[UseTemplate]
	public partial class TeamSelect : Panel
	{
		public static TeamSelect Instance;
		public Button Blue { get; set; }
		public Button Red { get; set; }
		public Label Timer { get; set; }
		private bool _open = true;

		public TeamSelect()
		{
			Instance = this;
		}

		public override void Tick()
		{
			base.Tick();

			if ( Input.Pressed( InputButton.Menu ) )
				_open = !_open;

			SetClass( "open", _open );

			if ( !IsVisible )
				return;

			var player = Local.Pawn;
			if ( player == null )
				return;

			var game = Game.Instance;
			if ( game == null )
				return;

			var state = game.CurrentGameState;
			if ( state == null )
				return;

			if ( state.UpdateTimer )
				Timer.Text = TimeSpan.FromSeconds( state.TimeLeftSeconds ).ToString( @"mm\:ss" );
			else
				Timer.Text = "";
		}

		public void Close()
		{
			_open = false;
		}

		public void BecomeSpectator()
		{
			Player.ChangeTeamCommand( Team.None );
		}

		public void JoinBlue()
		{
			Player.ChangeTeamCommand( Team.Blue );
		}

		public void JoinRed()
		{
			Player.ChangeTeamCommand( Team.Red );
		}		
	}
}
