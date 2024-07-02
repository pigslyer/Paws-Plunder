using System;
using Godot;

public class CombatLog : VBoxContainer 
{
	private const float DefaultMsgDisplayLength = 5.0F;
	private const string MetaTimeRemaining = "CombatLog::TimeRemaining";

	// the time currently represents how long this will be at the top of the board.
	public void PushMsg(string msg)
	{
		Label label = new Label()
		{
			Text = msg,
		};

		label.SetMeta(MetaTimeRemaining, DefaultMsgDisplayLength);
		AddChild(label);
	}

	public void Clear()
	{
		foreach (Node node in GetChildren())
		{
			node.QueueFree();
		}
	}

	public override void _Process(float delta)
	{
		int checkingChild = 0;

		while (delta > 0 && checkingChild < GetChildCount())
		{
			Node topOfBoard = GetChild(checkingChild);	

			float remainingTime = (float)topOfBoard.GetMeta(MetaTimeRemaining, 0.0f);

			float subbed = Math.Min(remainingTime, delta);

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
	}
}
