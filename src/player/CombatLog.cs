using System;
using Godot;

namespace PawsPlunder;

public partial class CombatLog : VBoxContainer 
{
	[Signal] public delegate void EmptiedEventHandler();
	[Signal] public delegate void EntryAddedEventHandler();

	private const float DefaultMsgDisplayLength = 5.0F;
	private const string MetaTimeRemaining = "CombatLog::TimeRemaining";

	private float _totalTime = 0f;

	// the time currently represents how long this will be at the top of the board.
	public void PushMsg(string msg)
	{
		Label label = new Label()
		{
			Text = msg,
		};
		AddChild(label);

		float time = DefaultMsgDisplayLength - _totalTime;

		label.SetMeta(MetaTimeRemaining, time);
		_totalTime += time;
		
		EmitSignal(nameof(EntryAdded));
	}

	public void Clear()
	{
		foreach (Node node in GetChildren())
		{
			node.QueueFree();
		}
	}

	public override void _Process(double delta)
	{
		_totalTime = Math.Max(_totalTime - (float)delta, 0.0f);

		int checkingChild = 0;

		while (delta > 0 && checkingChild < GetChildCount())
		{
			Node topOfBoard = GetChild(checkingChild);	

			float remainingTime = (float)topOfBoard.GetMeta(MetaTimeRemaining, 0.0f);

			float subbed = Math.Min(remainingTime, (float)delta);

			remainingTime -= subbed;
			delta -= subbed;

			if (remainingTime < float.Epsilon)
			{
				topOfBoard.QueueFree();	
			}
			else
			{
				topOfBoard.SetMeta(MetaTimeRemaining, remainingTime);
			}

			checkingChild += 1;
		}

		if (checkingChild != 0 && GetChild(checkingChild - 1).IsQueuedForDeletion())
		{
			EmitSignal(nameof(Emptied));
		}
	}
}
