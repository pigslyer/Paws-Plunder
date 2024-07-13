using Godot;

namespace PawsPlunder;

public partial class Level1 : Level
{
	[Export] private Player _player = null!;
	[Export] private Path3D _catapultPath = null!;
	[Export] private PathFollow3D _catapultPathFollow = null!;
	[Export] private CatapultOverlay _catapultOverlay = null!;

	[Export] private AudioStreamPlayer _musicGameplay = null!;
	[Export] private AudioStreamPlayer _musicDeath = null!;
	[Export] private AudioStreamPlayer _windPlayer = null!;
	[Export] private AudioStreamPlayer _cannonPlayer = null!;

	private const int DEATH_SCORE_PENALTY = -1000;
	private const int WIN_SCORE = 100000;
	public override void _Ready()
	{
		_player.LockInPlace = true;
		GlobalSignals.GetInstance().AddToPlayerScore += newScore => _CheckScoreCondition(newScore);
	}

	public override void OnStart()
	{
		Logger.Debug("Level1: OnStart() called");
		// TODO: don't instantiate player until we're ready to fuck shit up 
		_player.LockInPlace = true;
		_musicGameplay.Play();
		_SpawnPlayer();
	}

	private bool _CheckScoreCondition(int score) => (score >= WIN_SCORE);
	public override bool CheckWinConditions()
	{
		// TODO: figure out getting score signals
		return false;
	}

	private void _SpawnPlayer()
	{
		_player.Initialize();
		_player.Position = Vector3.Zero;
		_player.Hud.SetDoomPortrait(DoomPortraitType.Flying);
		var tween = CreateTween().SetParallel();
		tween.TweenProperty(_catapultPathFollow, "progress_ratio", 1f, 1.5)
			.From(0f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		_catapultOverlay.FadeOut(ref tween);
		tween.TweenProperty(_player, nameof(_player.LockInPlace), false, 1f); // lock player input until landing
		tween.TweenCallback(Callable.From(() => _player.Hud.SetDoomPortrait(DoomPortraitType.Idle))).SetDelay(1.5f);

		_windPlayer.Play();
		_cannonPlayer.Play();		
		_cannonPlayer.FadeOut(1.1f);

		DiscardTimer.Start(this, 1.6f).Timeout += () => {
			_windPlayer.FadeOut(0.6f);
		};
	}

	private void _OnPlayerDied()
	{
		_musicGameplay.Stop();
		_musicDeath.Play();
	}
	
	private void _on_Player_RespawnPlayer()
	{	
		Globals.RandomizeProtag();

		_player.LockInPlace = true;
		_SpawnPlayer();

		_player.Hud.PushLog($"Good luck {Globals.ProtagonistName}!");
		GlobalSignals.AddScore(DEATH_SCORE_PENALTY);

		_musicDeath.Stop();
		_musicGameplay.Play();
	}

	/*
	 *
	 * 	private void OnScoreAdded(int score)
	{
		bool hadWon = HasWon();

		if (!hadWon && HasWon())
		{
			LogControl.PushMsg("You have pillaged enough goods! Find a cannon and press [E] to escape!");

			GetNode<CanvasItem>("%EscapeText").Show();
			GetNode<ScoreDisplay>("%ScoreDisplay").Modulate = Colors.Yellow;

			GetTree().CallGroup("Cannons", "EnableEscape");
		}
	}
	 */
	/*
	 * 	private void EscapeShip()
	{
		bool use = Input.IsActionJustPressed("plr_use");

		if (!use)
		{
			return;
		}

		Godot.Collections.Array<Node> cannons = GetTree().GetNodesInGroup("Cannons");
		Cannon? closestCannon = cannons
			.OfType<Cannon>()
			.OrderBy(c => c.GlobalPosition.DistanceTo(GlobalPosition))
			.FirstOrDefault();

		if (closestCannon == null)
		{
			GD.PushWarning("No cannons could be found for player's win condition!");
			return;
		}

		if (float.Abs(closestCannon.GlobalPosition.DistanceTo(GlobalPosition)) < 12.0f)
		{
			GetTree().ChangeSceneToPacked(_youWonScene);
		}
	}


	 */
	public override void _ExitTree()
	{

	}
}










