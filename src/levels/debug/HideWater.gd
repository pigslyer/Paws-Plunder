extends Area3D

@export var _water: NodePath;



func _on_HideWater_body_entered(_body):
	get_node(_water).visible = false;


func _on_HideWater_body_exited(_body):
	get_node(_water).visible = true;
