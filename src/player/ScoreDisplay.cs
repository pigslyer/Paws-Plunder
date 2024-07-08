using System;
using Godot;

namespace PawsPlunder;

public partial class ScoreDisplay : Control
{
	[Export(hintString: "suffix:s")] private float _interpolationTime = 1.0f;

	private int _startingInterpolatedScore;
	private int _targetInterpolatedScore;
	private float _lerp = 1.0F;
	[Export] private Label _label = null!;
	
	public override void _Ready()
	{
		GlobalSignals.GetInstance().AddToPlayerScore += newScore => 
			InterpolateScoreTo(_targetInterpolatedScore + newScore);
	}
	
	public void InterpolateScoreTo(int newTargetScore)
	{
		_startingInterpolatedScore = GetLerpedScore();
		_targetInterpolatedScore = newTargetScore;
		_lerp = 0.0F;
	}

	public override void _PhysicsProcess(double delta)
	{
		float fDelta = (float)delta;

		_lerp = Math.Min(1.0F, _lerp + fDelta / _interpolationTime);
		_label.Text = " " + GetLerpedScore().ToString();
	}

	private int GetLerpedScore()
	{
		return LerpInt(_startingInterpolatedScore, _targetInterpolatedScore, _lerp);		
	}

	public static int LerpInt(int start, int end, float lerp)
	{
		return start + (int)Math.Floor(lerp * (end - start - 1)) + (lerp > 0.0F ? 1 : 0);
	}
}
