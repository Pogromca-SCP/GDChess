extends Node

#region Data
const PORT = 7000
const DEFAULT_SERVER_IP = "127.0.0.1"
const MAX_CONNECTIONS = 1

@onready
var ip_box: TextEdit = $IP

@onready
var join: Button = $Join

@onready
var host: Button = $Host

@onready
var solo: Button = $Solo

@onready
var board: Sprite2D = $"../Board"
#endregion

#region Shitty networking
func _ready() -> void:
	multiplayer.peer_connected.connect(_on_player_connected)
	multiplayer.peer_disconnected.connect(close_game)
	multiplayer.connection_failed.connect(remove_multiplayer_peer)
	multiplayer.server_disconnected.connect(remove_multiplayer_peer)
	join.pressed.connect(join_game)
	host.pressed.connect(create_game)
	solo.pressed.connect(start_solo_game)


func join_game() -> void:
	var address: String = ip_box.text
	
	if address.is_empty():
		address = DEFAULT_SERVER_IP
	
	var peer: ENetMultiplayerPeer = ENetMultiplayerPeer.new()
	var error: Error = peer.create_client(address, PORT)
	
	if error:
		print(error)
		return
	
	multiplayer.multiplayer_peer = peer


func create_game() -> void:
	var peer: ENetMultiplayerPeer = ENetMultiplayerPeer.new()
	var error: Error = peer.create_server(PORT, MAX_CONNECTIONS)
	
	if error:
		print(error)
		return
	
	multiplayer.multiplayer_peer = peer


func close_game(_id: int) -> void:
	multiplayer.multiplayer_peer.close()
	multiplayer.multiplayer_peer = null


func remove_multiplayer_peer() -> void:
	multiplayer.multiplayer_peer = null


func start_solo_game():
	remove_multiplayer_peer()
	player_loaded()


func player_loaded():
	self.visible = false
	board.visible = true


func _on_player_connected(id: int):
	_register_player.rpc_id(id)


@rpc("any_peer", "reliable")
func _register_player():
	player_loaded()
#endregion
