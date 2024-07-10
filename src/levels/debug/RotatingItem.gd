extends Sprite3D

@export var _rotateSpeedDeg: float;

var _currentRotation: float;

func _process(delta):
	var camera: Camera3D = get_viewport().get_camera();
	
	_currentRotation += _rotateSpeedDeg * delta;
	
	if _currentRotation > 360:
		_currentRotation -= 360;
	
	global_rotation = camera.global_rotation;
	rotation.y = 0;
	rotation.x = _currentRotation;
	
