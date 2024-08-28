extends Sprite2D

#region Enums
enum Pieces {
	NONE,
	PAWN,
	KNIGHT,
	BISHOP,
	ROOK,
	QUEEN,
	KING,
}

enum Columns {
	LEFT_ROOK,
	LEFT_KNIGHT,
	LEFT_BISHOP,
	QUEEN,
	KING,
	RIGHT_BISHOP,
	RIGHT_KNIGHT,
	RIGHT_ROOK,
}
#endregion

#region Constants
const BOARD_SIZE = 8
const CELL_WIDTH = 18
const HALF_WIDTH = CELL_WIDTH / 2

const ROOK_DIRECTIONS: Array[Vector2] = [Vector2(0, 1), Vector2(0, -1), Vector2(1, 0),
		Vector2(-1, 0)]
const BISHOP_DIRECTIONS: Array[Vector2] = [Vector2(1, 1), Vector2(1, -1), Vector2(-1, 1),
		Vector2(-1, -1)]
const ALL_DIRECTIONS: Array[Vector2] = [Vector2(0, 1), Vector2(0, -1), Vector2(1, 0),
		Vector2(-1, 0), Vector2(1, 1), Vector2(1, -1), Vector2(-1, 1), Vector2(-1, -1)]
const KNIGHT_DIRECTIONS: Array[Vector2] = [Vector2(2, 1), Vector2(2, -1), Vector2(1, 2),
		Vector2(1, -2), Vector2(-2, 1), Vector2(-2, -1), Vector2(-1, 2), Vector2(-1, -2)]

const TEXTURE_HOLDER = preload("res://scenes/texture_holder.tscn")

const BLACK_BISHOP = preload("res://assets/black_bishop.png")
const BLACK_KING = preload("res://assets/black_king.png")
const BLACK_KNIGHT = preload("res://assets/black_knight.png")
const BLACK_PAWN = preload("res://assets/black_pawn.png")
const BLACK_QUEEN = preload("res://assets/black_queen.png")
const BLACK_ROOK = preload("res://assets/black_rook.png")
const WHITE_BISHOP = preload("res://assets/white_bishop.png")
const WHITE_KING = preload("res://assets/white_king.png")
const WHITE_KNIGHT = preload("res://assets/white_knight.png")
const WHITE_PAWN = preload("res://assets/white_pawn.png")
const WHITE_QUEEN = preload("res://assets/white_queen.png")
const WHITE_ROOK = preload("res://assets/white_rook.png")

const TURN_WHITE = preload("res://assets/turn-white.png")
const TURN_BLACK = preload("res://assets/turn-black.png")

const PIECE_MOVE = preload("res://assets/Piece_move.png")
#endregion

#region Variables
var board: Array[Array]
var is_white_turn: bool = true
var choosing_move: bool = false
var moves: Array[Vector2] = []
var selected_piece: Vector2
var promotion_square: Variant = null
var white_king: bool = false
var black_king: bool = false
var white_rook_left: bool = false
var white_rook_right: bool = false
var black_rook_left: bool = false
var black_rook_right: bool = false
var en_passant: Variant = null
var white_king_pos: Vector2 = Vector2(0, Columns.KING)
var black_king_pos: Vector2 = Vector2(BOARD_SIZE - 1, Columns.KING)

@onready
var pieces: Node2D = $Pieces

@onready
var dots: Node2D = $Dots

@onready
var turn: Sprite2D = $Turn

@onready
var white_pieces: Control = $"../CanvasLayer/WhitePieces"

@onready
var black_pieces: Control = $"../CanvasLayer/BlackPieces"
#endregion

#region Sprite2D API
func _ready() -> void:
	board.append([Pieces.ROOK, Pieces.KNIGHT, Pieces.BISHOP, Pieces.QUEEN, Pieces.KING,
			Pieces.BISHOP, Pieces.KNIGHT, Pieces.ROOK])
	add_simple_row(Pieces.PAWN)
	
	for i in BOARD_SIZE - 4:
		add_simple_row(Pieces.NONE)
	
	add_simple_row(-Pieces.PAWN)
	board.append([-Pieces.ROOK, -Pieces.KNIGHT, -Pieces.BISHOP, -Pieces.QUEEN, -Pieces.KING,
			-Pieces.BISHOP, -Pieces.KNIGHT, -Pieces.ROOK])
	display_board()
	
	for group in ["white_pieces", "black_pieces"]:
		for button in get_tree().get_nodes_in_group(group):
			button.pressed.connect(_on_button_pressed.bind(button))


func _input(event: InputEvent) -> void:
	if !(event is InputEventMouseButton) || !event.pressed || promotion_square != null || \
			event.button_index != MOUSE_BUTTON_LEFT || is_mouse_out() || !is_my_turn():
		return
	
	var mouse_pos: Vector2 = get_global_mouse_position()
	var y: int = snapped(mouse_pos.x, 0) / CELL_WIDTH
	var x: int = abs(snapped(mouse_pos.y, 0)) / CELL_WIDTH
	
	if choosing_move:
		if multiplayer.multiplayer_peer == null:
			set_move(x, y)
		else:
			set_move.rpc(x, y)
	elif is_same_team(board[x][y]):
		if multiplayer.multiplayer_peer == null:
			simulate_options(x, y)
		else:
			simulate_options.rpc(x, y)
		
		show_options()
		choosing_move = true
#endregion

#region Display
func add_simple_row(piece: int) -> void:
	board.append([piece, piece, piece, piece, piece, piece, piece, piece])


func is_mouse_out() -> bool:
	return !get_rect().has_point(to_local(get_global_mouse_position()))


func display_board() -> void:
	for child in pieces.get_children():
		child.queue_free()
	
	for i in BOARD_SIZE:
		for j in BOARD_SIZE:
			var holder: Sprite2D = TEXTURE_HOLDER.instantiate()
			pieces.add_child(holder)
			holder.global_position = Vector2(j * CELL_WIDTH + HALF_WIDTH,
					-i * CELL_WIDTH - HALF_WIDTH)
			
			match board[i][j]:
				-Pieces.KING: holder.texture = BLACK_KING
				-Pieces.QUEEN: holder.texture = BLACK_QUEEN
				-Pieces.ROOK: holder.texture = BLACK_ROOK
				-Pieces.BISHOP: holder.texture = BLACK_BISHOP
				-Pieces.KNIGHT: holder.texture = BLACK_KNIGHT
				-Pieces.PAWN: holder.texture = BLACK_PAWN
				Pieces.KING: holder.texture = WHITE_KING
				Pieces.QUEEN: holder.texture = WHITE_QUEEN
				Pieces.ROOK: holder.texture = WHITE_ROOK
				Pieces.BISHOP: holder.texture = WHITE_BISHOP
				Pieces.KNIGHT: holder.texture = WHITE_KNIGHT
				Pieces.PAWN: holder.texture = WHITE_PAWN
				_: holder.texture = null
	
	turn.texture = TURN_WHITE if is_white_turn else TURN_BLACK


@rpc("any_peer", "call_local", "reliable")
func simulate_options(x: int, y: int) -> void:
	selected_piece = Vector2(x, y)
	moves = get_moves(selected_piece)


func show_options() -> void:
	if moves.is_empty():
		choosing_move = false
		return
	
	show_dots()


func show_dots() -> void:
	for move in moves:
		var holder: Sprite2D = TEXTURE_HOLDER.instantiate()
		dots.add_child(holder)
		holder.texture = PIECE_MOVE
		holder.global_position = Vector2(move.y * CELL_WIDTH + HALF_WIDTH,
				-move.x * CELL_WIDTH - HALF_WIDTH)


func delete_dots() -> void:
	for child in dots.get_children():
		child.queue_free()
#endregion

#region Movement
@rpc("any_peer", "call_local", "reliable")
func set_move(x: int, y: int) -> void:
	var just_now: bool = false
	
	for move in moves:
		if move.x == x && move.y == y:
			match board[selected_piece.x][selected_piece.y]:
				Pieces.PAWN, -Pieces.PAWN:
					just_now = move_pawn(move)
				Pieces.ROOK, -Pieces.ROOK:
					move_rook()
				Pieces.KING, -Pieces.KING:
					move_king(move)
				_:
					pass
			
			if !just_now:
				en_passant = null
			
			board[x][y] = board[selected_piece.x][selected_piece.y]
			board[selected_piece.x][selected_piece.y] = Pieces.NONE
			is_white_turn = !is_white_turn
			display_board()
			break
	
	delete_dots()
	choosing_move = false
	
	if is_my_turn() && (selected_piece.x != x || selected_piece.y != y) && \
			is_same_team(board[x][y]):
		if multiplayer.multiplayer_peer == null:
			simulate_options(x, y)
		else:
			simulate_options.rpc(x, y)
		
		show_options()
		choosing_move = true


func move_pawn(move: Vector2) -> bool:
	var promotion_row: int = BOARD_SIZE - 1 if is_white_turn else 0
	var start_position: int = 1 if is_white_turn else BOARD_SIZE - 2
	var middle_row: int = 3 if is_white_turn else 4
	
	if move.x == promotion_row:
		promote(move)
	elif move.x == middle_row && selected_piece.x == start_position:
		en_passant = move
		return true
	elif en_passant != null && en_passant.y == move.y && selected_piece.y != move.y && \
			en_passant.x == selected_piece.x:
		board[en_passant.x][en_passant.y] = Pieces.NONE
	
	return false


func move_rook() -> void:
	if selected_piece.x == 0:
		if selected_piece.y == Columns.LEFT_ROOK:
			white_rook_left = true
		elif selected_piece.y == Columns.RIGHT_ROOK:
			white_rook_right = true
	elif selected_piece.x == BOARD_SIZE - 1:
		if selected_piece.y == Columns.LEFT_ROOK:
			black_rook_left = true
		elif selected_piece.y == Columns.RIGHT_ROOK:
			black_rook_right = true


func move_king(move: Vector2) -> void:
	var initial_row: int = 0 if is_white_turn else BOARD_SIZE - 1
	var castle_piece: int = Pieces.ROOK if is_white_turn else -Pieces.ROOK
	
	if selected_piece.x == initial_row && selected_piece.y == Columns.KING:
		if is_white_turn:
			white_king = true
		else:
			black_king = true
		
		if move.y == Columns.LEFT_BISHOP:
			mark_rooks()
			board[initial_row][Columns.LEFT_ROOK] = Pieces.NONE
			board[initial_row][Columns.QUEEN] = castle_piece
		elif move.y == Columns.RIGHT_KNIGHT:
			mark_rooks()
			board[initial_row][Columns.RIGHT_ROOK] = Pieces.NONE
			board[initial_row][Columns.RIGHT_BISHOP] = castle_piece
	
	if is_white_turn:
		white_king_pos = move
	else:
		black_king_pos = move


func mark_rooks() -> void:
	if is_white_turn:
		white_rook_left = true
		white_rook_right = true
	else:
		black_rook_left = true
		black_rook_right = true


func get_moves(selected: Vector2) -> Array[Vector2]:
	match abs(board[selected.x][selected.y]):
		Pieces.PAWN: return get_pawn_moves(selected)
		Pieces.KNIGHT: return get_knight_moves(selected)
		Pieces.BISHOP: return get_bishop_moves(selected)
		Pieces.ROOK: return get_rook_moves(selected)
		Pieces.QUEEN: return get_queen_moves(selected)
		Pieces.KING: return get_king_moves(selected)
		_: return []


func get_rook_moves(piece_pos: Vector2) -> Array[Vector2]:
	return get_unlimited_moves(Pieces.ROOK, piece_pos, ROOK_DIRECTIONS)


func get_bishop_moves(piece_pos: Vector2) -> Array[Vector2]:
	return get_unlimited_moves(Pieces.BISHOP, piece_pos, BISHOP_DIRECTIONS)


func get_queen_moves(piece_pos: Vector2) -> Array[Vector2]:
	return get_unlimited_moves(Pieces.QUEEN, piece_pos, ALL_DIRECTIONS)


func get_king_moves(piece_pos: Vector2) -> Array[Vector2]:
	var available_moves: Array[Vector2] = []
	
	if is_white_turn:
		board[white_king_pos.x][white_king_pos.y] = Pieces.NONE
	else:
		board[black_king_pos.x][black_king_pos.y] = Pieces.NONE
	
	for dir in ALL_DIRECTIONS:
		var pos: Vector2 = piece_pos + dir
		
		if is_valid_position(pos) && !is_in_check(pos) && (is_empty(pos) || is_enemy(pos)):
			available_moves.append(pos)
	
	if is_white_turn && !white_king:
		if !white_rook_left && left_rook_checks():
			available_moves.append(Vector2(0, Columns.LEFT_BISHOP))
		if !white_rook_right && right_rook_checks():
			available_moves.append(Vector2(0, Columns.RIGHT_KNIGHT))
	elif !is_white_turn && !black_king:
		if !black_rook_left && left_rook_checks():
			available_moves.append(Vector2(BOARD_SIZE - 1, Columns.LEFT_BISHOP))
		if !black_rook_right && right_rook_checks():
			available_moves.append(Vector2(BOARD_SIZE - 1, Columns.RIGHT_KNIGHT))
			
	if is_white_turn:
		board[white_king_pos.x][white_king_pos.y] = Pieces.KING
	else:
		board[black_king_pos.x][black_king_pos.y] = -Pieces.KING
	
	return available_moves


func left_rook_checks() -> bool:
	var row: int = 0 if is_white_turn else BOARD_SIZE - 1
	return is_empty(Vector2(row, Columns.LEFT_KNIGHT)) && \
			is_empty(Vector2(row, Columns.LEFT_BISHOP)) && \
			!is_in_check(Vector2(row, Columns.LEFT_BISHOP)) && \
			is_empty(Vector2(row, Columns.QUEEN)) && !is_in_check(Vector2(row, Columns.QUEEN)) && \
			!is_in_check(Vector2(row, Columns.KING))


func right_rook_checks() -> bool:
	var row: int = 0 if is_white_turn else BOARD_SIZE - 1
	return !is_in_check(Vector2(row, Columns.KING)) && \
			is_empty(Vector2(row, Columns.RIGHT_BISHOP)) && \
			!is_in_check(Vector2(row, Columns.RIGHT_BISHOP)) && \
			is_empty(Vector2(row, Columns.RIGHT_KNIGHT)) && \
			!is_in_check(Vector2(row, Columns.RIGHT_KNIGHT))


func get_knight_moves(piece_pos: Vector2) -> Array[Vector2]:
	return get_single_moves(Pieces.KNIGHT, piece_pos, KNIGHT_DIRECTIONS)


func get_pawn_moves(piece_pos: Vector2) -> Array[Vector2]:
	var available_moves: Array[Vector2] = []
	var direction: Vector2 = Vector2(1, 0) if is_white_turn else Vector2(-1, 0)
	var is_first_move: bool = piece_pos.x == 1 if is_white_turn else piece_pos.x == BOARD_SIZE - 2
	var middle_row: int = 4 if is_white_turn else 3
	
	if en_passant != null && piece_pos.x == middle_row && abs(en_passant.y - piece_pos.y) == 1:
		var inner_pos: Vector2 = en_passant + direction
		
		if validate_en_passant(Pieces.PAWN, inner_pos, piece_pos):
			available_moves.append(inner_pos)
	
	var pos: Vector2 = piece_pos + direction
	
	if is_empty(pos) && validate_move(Pieces.PAWN, pos, piece_pos):
		available_moves.append(pos)
	
	for i in [-1, 1]:
		pos = piece_pos + Vector2(direction.x, i)
		
		if is_valid_position(pos) && is_enemy(pos) && validate_move(Pieces.PAWN, pos, piece_pos):
			available_moves.append(pos)
	
	pos = piece_pos + direction * 2
	
	if is_first_move && is_empty(pos) && is_empty(piece_pos + direction) && \
			validate_move(Pieces.PAWN, pos, piece_pos):
		available_moves.append(pos)
	
	return available_moves


func get_unlimited_moves(piece: int, piece_pos: Vector2,
		directions: Array[Vector2]) -> Array[Vector2]:
	var available_moves: Array[Vector2] = []
	
	for dir in directions:
		var pos: Vector2 = piece_pos + dir
		
		while is_valid_position(pos):
			if is_empty(pos) && validate_move(piece, pos, piece_pos): 
				available_moves.append(pos)
			elif is_enemy(pos):
				if validate_move(piece, pos, piece_pos):
					available_moves.append(pos)
				
				break
			else:
				break
			
			pos += dir
	
	return available_moves


func get_single_moves(piece: int, piece_pos: Vector2,
		directions: Array[Vector2]) -> Array[Vector2]:
	var available_moves: Array[Vector2] = []
	
	for dir in directions:
		var pos: Vector2 = piece_pos + dir
		
		if is_valid_position(pos) && (is_empty(pos) || is_enemy(pos)) && \
				validate_move(piece, pos, piece_pos):
			available_moves.append(pos)
	
	return available_moves


func validate_move(piece: int, pos: Vector2, piece_pos: Vector2) -> bool:
	var tmp: int = board[pos.x][pos.y]
	board[pos.x][pos.y] = piece if is_white_turn else -piece
	board[piece_pos.x][piece_pos.y] = Pieces.NONE
	var result: bool = !is_in_check(white_king_pos if is_white_turn else black_king_pos)
	board[pos.x][pos.y] = tmp
	board[piece_pos.x][piece_pos.y] = piece if is_white_turn else -piece
	return result


func validate_en_passant(piece: int, pos: Vector2, piece_pos: Vector2) -> bool:
	var tmp: int = board[pos.x][pos.y]
	board[pos.x][pos.y] = piece if is_white_turn else -piece
	board[piece_pos.x][piece_pos.y] = Pieces.NONE
	board[en_passant.x][en_passant.y] = Pieces.NONE
	var result: bool = !is_in_check(white_king_pos if is_white_turn else black_king_pos)
	board[pos.x][pos.y] = tmp
	board[piece_pos.x][piece_pos.y] = piece if is_white_turn else -piece
	board[en_passant.x][en_passant.y] = -piece if is_white_turn else piece
	return result
#endregion

#region Misc
func is_my_turn() -> bool:
	return multiplayer.multiplayer_peer == null || (is_white_turn if multiplayer.is_server() else \
			!is_white_turn)


static func is_valid_position(pos: Vector2) -> bool:
	return pos.x >= 0 && pos.x < BOARD_SIZE && pos.y >= 0 && pos.y < BOARD_SIZE


func is_empty(pos: Vector2) -> bool:
	return board[pos.x][pos.y] == Pieces.NONE


func is_same_team(piece: int) -> bool:
	return piece > Pieces.NONE if is_white_turn else piece < Pieces.NONE


func is_enemy(pos: Vector2) -> bool:
	return board[pos.x][pos.y] < Pieces.NONE if is_white_turn else board[pos.x][pos.y] > Pieces.NONE


func promote(pos: Vector2) -> void:
	promotion_square = pos
	
	if !is_my_turn():
		return
	
	white_pieces.visible = is_white_turn
	black_pieces.visible = !is_white_turn


func _on_button_pressed(button: Button) -> void:
	var num_char: int = int(button.name.substr(0, 1))
	
	if multiplayer.multiplayer_peer == null:
		promotion(num_char)
	else:
		promotion.rpc(num_char)


@rpc("any_peer", "call_local", "reliable")
func promotion(num: int) -> void:
	board[promotion_square.x][promotion_square.y] = -num if is_white_turn else num
	white_pieces.visible = false
	black_pieces.visible = false
	promotion_square = null
	display_board()


func is_in_check(king_pos: Vector2) -> bool:
	var pawn_direction = 1 if is_white_turn else -1
	var pawn_attacks = [king_pos + Vector2(pawn_direction, 1),
			king_pos + Vector2(pawn_direction, -1)]
	var attacker: int = -Pieces.PAWN if is_white_turn else Pieces.PAWN
	
	for at in pawn_attacks:
		if is_valid_position(at) && board[at.x][at.y] == attacker:
			return true
	
	return is_attacked(king_pos, ALL_DIRECTIONS, -Pieces.KING if is_white_turn else \
			Pieces.KING) || is_attacked_lines(king_pos) || is_attacked(king_pos, KNIGHT_DIRECTIONS,
			-Pieces.KNIGHT if is_white_turn else Pieces.KNIGHT)


func is_attacked(king_pos: Vector2, directions: Array[Vector2], attacker: int) -> bool:
	for dir in directions:
		var pos: Vector2 = king_pos + dir
		
		if is_valid_position(pos) && board[pos.x][pos.y] == attacker:
			return true
	
	return false


func is_attacked_lines(king_pos: Vector2) -> bool:
	var straight_attackers: Array = [-Pieces.ROOK, -Pieces.QUEEN] if is_white_turn else \
			[Pieces.ROOK, Pieces.QUEEN]
	var diagonal_attackers: Array = [-Pieces.BISHOP, -Pieces.QUEEN] if is_white_turn else \
			[Pieces.BISHOP, Pieces.QUEEN]
	
	for dir in ALL_DIRECTIONS:
		var pos: Vector2 = king_pos + dir
		
		while is_valid_position(pos):
			if !is_empty(pos):
				var piece: int = board[pos.x][pos.y]
				
				if (dir.x == 0 || dir.y == 0) && piece in straight_attackers:
					return true
				elif (dir.x != 0 && dir.y != 0) && piece in diagonal_attackers:
					return true
				break
			
			pos += dir
	
	return false
#endregion
