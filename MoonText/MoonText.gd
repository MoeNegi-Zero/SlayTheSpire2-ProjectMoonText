extends Control

@export var random_position: bool = false        # 随机位置开关
@export var random_area_x: Vector2 = Vector2(0, 1350)  # 随机区域X范围
@export var random_area_y: Vector2 = Vector2(200, 650)  # 随机区域Y范围

@export var random_text: bool = false
@export var text: String = "如果你看到这个文字，说明出错了"
@export var font_size: int = 50
@export var font: Font

@export var char_offset: Vector2 = Vector2(50, randf_range(-3.0, 3.0))
@export var char_delay: float = 0.1

@export var float_each_char: bool = true        # 上浮每一个文字
@export var float_delay: float = 5.0
@export var float_time: float = 2
@export var float_scale: float = 0.7
@export var float_distance: float = 50.0

@export var jitter_interval: float = 0.4
@export var jitter_strength: float = 1

@export var ascii_scale: float = 0.6   # 英文步进比例

@export var SpawnText: bool = false

@export var text_options: Array = [
	"[color=mediumvioletred]写[/color]",
	"[color=mediumvioletred]你[/color]",
	"[color=mediumvioletred]想[/color]",
	"[color=mediumvioletred]随[/color]",
	"[color=mediumvioletred]机[/color]",
	"[color=mediumvioletred]的[/color]",
	"[color=mediumvioletred]文[/color]",
	"[color=mediumvioletred]字[/color]"
]

var labels: Array = []
var base_positions: Array = []
var jitter_offsets: Array = []
var raw_chars: Array = []

var float_offset: float = 0.0

# ------------------------
# 预览
# ------------------------
func _process(delta):
	if SpawnText:
		SpawnText = false
		spawn()

# ------------------------
# 初始化
# ------------------------
func spawn():
	if random_text:
		text = text_options[randi() % text_options.size()]
	if random_position:
		var x = randf_range(random_area_x.x, random_area_x.y)
		var y = randf_range(random_area_y.x, random_area_y.y)
		position = Vector2(x,y)

	create_chars()
	await get_tree().process_frame
	update_container_size()
	visible = true
	modulate.a = 1
	start_typewriter()

# ------------------------
# 创建字符（核心：cursor排版）
# ------------------------
func create_chars():
	for c in get_children():
		c.queue_free()
	await get_tree().process_frame

	labels.clear()
	base_positions.clear()
	jitter_offsets.clear()
	size = Vector2.ZERO
	float_offset = 0

	var parsed = parse_bbcode_per_char(text)

	var cursor := Vector2.ZERO

	for i in range(parsed.size()):
		var label = RichTextLabel.new()

		label.bbcode_enabled = true
		label.text = parsed[i]
		label.fit_content = true
		label.scroll_active = false
		label.autowrap_mode = TextServer.AUTOWRAP_OFF
		label.size = Vector2.ZERO
		label.visible = false

		#获取字体
		if font != null:
			label.add_theme_font_override("normal_font", font)

		label.add_theme_font_size_override("normal_font_size", font_size)
		
		label.add_theme_color_override("font_shadow_color",Color(0, 0, 0, 0.5))
		label.add_theme_constant_override("shadow_outline_size",2)
		label.add_theme_constant_override("shadow_offset_x",2)
		label.add_theme_constant_override("shadow_offset_y",2)
		
		add_child(label)

		var c = raw_chars[i]

		# ------------------------
		# 放置位置
		# ------------------------
		label.position = cursor

		labels.append(label)
		base_positions.append(cursor)
		jitter_offsets.append(Vector2.ZERO)

		# ------------------------
		# 使用真实字宽推进
		# ------------------------
		var use_font = font if font != null else ThemeDB.fallback_font

		var advance : float

		if is_ascii(c):
			# 英文：用真实 glyph 宽度
			advance = use_font.get_char_size(c.unicode_at(0), font_size).x
	
			if advance <= 0:
				advance = font_size * 0.5
		else:
			# 中文：用固定宽度（关键！！）
			advance = font_size

		cursor.x += advance + 5

		# ------------------------
		# Y：保持原来的斜向结构
		# ------------------------
		cursor.y += char_offset.y

func update_container_size():
	if labels.size() == 0:
		return

	# 用第一个元素初始化
	var first = labels[0]
	var bounds = Rect2(first.position, first.get_combined_minimum_size())

	# 合并所有子节点
	for i in range(1, labels.size()):
		var l = labels[i]
		var rect = Rect2(l.position, l.get_combined_minimum_size())
		bounds = bounds.merge(rect)

	# 设置容器大小
	size = bounds.size

	# 可选：让子节点相对左上角归一
	for l in labels:
		l.position -= bounds.position


# ------------------------
# 打字机
# ------------------------
func start_typewriter():
	await get_tree().process_frame

	for i in range(labels.size()):
		var l = labels[i]

		l.visible = true

		# 每个字符出现立即开始抖动
		start_single_jitter(i)

		if is_inside_tree(): await get_tree().create_timer(char_delay).timeout

	if is_inside_tree(): await get_tree().create_timer(float_delay).timeout

	start_float()


# ------------------------
# 单字符抖动（协程）
# ------------------------
func start_single_jitter(index: int):
	_jitter_loop(index)

func _jitter_loop(index: int) -> void:
	while is_inside_tree() && modulate.a>0:
		jitter_offsets[index] = Vector2(
			randf_range(-jitter_strength, jitter_strength),
			randf_range(-jitter_strength, jitter_strength)
		)

		update_positions()
		if is_inside_tree(): await get_tree().create_timer(jitter_interval).timeout
	
	visible = false

# ------------------------
# 上浮 + 渐隐
# ------------------------
func start_float():
	if(float_each_char):
		var t := 0.0

		while t < float_time:
			t += get_process_delta_time()
			var ratio = t / float_time

			float_offset = lerp(0.0, -float_distance, ratio)
			modulate.a = 1.0 - ratio

			for l in labels:
				# 缩小至float_scale
				l.scale = Vector2(1,1).lerp(Vector2(float_scale,float_scale), ratio)

			update_positions()
		
			if is_inside_tree():await get_tree().process_frame
	else:
		var t := 0.0
		var initial_y = position.y
		pivot_offset = size/2

		while t < float_time:
			t += get_process_delta_time()
			var ratio = t / float_time

			position.y = lerp(initial_y, initial_y - float_distance, ratio)
			scale = Vector2(1,1).lerp(Vector2(float_scale,float_scale), ratio)
			modulate.a = 1.0 - ratio
		
			if is_inside_tree():await get_tree().process_frame

# ------------------------
# 统一位置计算（动画层）
# ------------------------
func update_positions():
	for i in range(labels.size()):
		labels[i].position = base_positions[i] + jitter_offsets[i] + Vector2(0, float_offset)


# ------------------------
# BBCode 拆分（同时记录 raw_char）
# ------------------------
func parse_bbcode_per_char(input: String) -> Array:
	var result = []
	var color_stack = []
	raw_chars.clear()

	var i = 0

	while i < input.length():
		if input[i] == "[":
			var end = input.find("]", i)
			if end == -1:
				break

			var tag = input.substr(i, end - i + 1)

			if tag.begins_with("[color="):
				color_stack.append(tag)
			elif tag == "[/color]":
				if color_stack.size() > 0:
					color_stack.pop_back()

			i = end + 1
			continue

		var c = input[i]

		raw_chars.append(c)

		var wrapped = ""

		for t in color_stack:
			wrapped += t

		wrapped += c

		for j in range(color_stack.size() - 1, -1, -1):
			wrapped += "[/color]"

		result.append(wrapped)

		i += 1

	return result


# ------------------------
# 基于ASCII的字符类型判断
# ------------------------
func is_ascii(c: String) -> bool:
	return c.length() > 0 and c.unicode_at(0) < 128
