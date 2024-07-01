extends Label

export var color1: Color;
export var color2: Color;

var is_color_1: bool = false;

func _on_Timer_timeout():
	if is_color_1:
		self_modulate = color1;
	else:
		self_modulate = color2;
	
	is_color_1 = !is_color_1;
