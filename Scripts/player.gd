extends CharacterBody3D


const SPEED = 8
const JUMP_VELOCITY = 4.5
var footstep_timer = 0.0
var footstep_interval = 0.8

var gravity_scale = 1.0
var is_surfing = false
var SURF_SPEED_THRESHOLD = 5.0


@onready var neck := $Neck
@onready var camera := $Neck/Camera
@onready var footstep_sound := $Steps
@onready var humming := $Humming


func is_moving_downhill() -> bool:
	if is_on_floor():
		var floor_normal = get_floor_normal()
		var floor_angle = acos(floor_normal.dot(Vector3.UP))
# Convert angle to degrees
		floor_angle = rad_to_deg(floor_angle)
# Define the minimum angle to consider as downhill
		var min_downhill_angle = 15  # Adjust as needed
		if floor_angle >= min_downhill_angle:
# Check if the player is moving in the direction of the slope
			var slope_direction = floor_normal.cross(Vector3.RIGHT)
			var movement_direction = velocity.normalized()
			var angle_between = rad_to_deg(acos(slope_direction.dot(movement_direction)))
			if angle_between < 90:
				return true
	return false


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
	elif event.is_action_pressed("ui_cancel"):
		Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
	if Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED:
		if event is InputEventMouseMotion:
			neck.rotate_y(-event.relative.x * 0.005)
			camera.rotate_x(-event.relative.y * 0.005)
			camera.rotation.x = clamp(camera.rotation.x, deg_to_rad(-60), deg_to_rad(80))
		

func _physics_process(delta: float) -> void:
	# Add the gravity.
	if not is_on_floor():
		velocity += get_gravity() * delta

	# Handle hum.
	if Input.is_action_just_pressed("hum"):
		humming.play()
	if humming.finished and Input.is_action_just_released("hum"):
		humming.stop()

	handle_normal_movement(delta)

	move_and_slide()




func handle_normal_movement(delta: float):
	# Get the input direction and handle the movement/deceleration.
	# As good practice, you should replace UI actions with custom gameplay actions.
	var input_dir := Input.get_vector("left", "right", "forward", "back")
	var direction = (neck.transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	if direction:
		velocity.x = direction.x * SPEED
		velocity.z = direction.z * SPEED
		
		footstep_timer += delta
		if footstep_timer >= footstep_interval:
			footstep_sound.play()
			footstep_timer = 0.0
	else:
		velocity.x = move_toward(velocity.x, 0, SPEED)
		velocity.z = move_toward(velocity.z, 0, SPEED)
		footstep_timer = 0.0
		
		

func handle_surfing(delta: float):
	# Get input for strafing
	var strafe_input = Input.get_vector("strafe left", "strafe right","accelerate","decelerate")
	# Adjust the direction based on camera or player orientation
	var strafe_direction = neck.transform.basis.x * strafe_input.x
	# Adjust velocity for strafing
	velocity.x = strafe_direction.y * SPEED * 1.5  # Increase speed if desired
	velocity.z = strafe_direction.xd * SPEED * 1.5

	# Apply reduced gravity
	velocity.y += get_gravity() * gravity_scale * delta


func enter_surf_mode():
	is_surfing = true
	# Start a tween to interpolate gravity_scale to 0.5
	gravity_scale = 0.5

func exit_surf_mode():
	is_surfing = false
	# Reset gravity
	gravity_scale = 1.0
	# Reset other physics properties if necessary
