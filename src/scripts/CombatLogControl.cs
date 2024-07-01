using Godot;

public class CombatLogControl : Control
{
	private const float MsgDisplayLength = 5.0F;
	private RichTextLabel _label;
	private float _msgTime;

	public void SetMsg(string msg)
	{
		_msgTime = MsgDisplayLength;
		_label.Text = msg;
	}

	public void SetMsg(string msg, float len)
	{
		_msgTime = len;
		_label.Text = msg;
	}

	public string GetMsg()
	{
		return _label.Text;
	}

	public override void _PhysicsProcess(float delta)
	{
		_msgTime -= delta;
		if (_msgTime <= 0.0F)
		{
			_label.Text = null;
		}
	}

	public override void _Ready()
	{
		base._Ready();
		_label = (RichTextLabel)GetNode(new NodePath("Msg"));
	}
}
