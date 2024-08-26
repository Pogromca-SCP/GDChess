using Godot;

namespace GDChess;

public partial class CSHost : CanvasLayer
{
    #region Data
    public const int Port = 7000;

    public const string DefaultServerIP = "127.0.0.1";

    public const int MaxConnections = 1;

    public TextEdit IPBox { get; set; } = null!;

    public Button Join { get; set; } = null!;

    public Button Host { get; set; } = null!;

    public Button Solo { get; set; } = null!;

    public Sprite2D Board { get; set; } = null!;
    #endregion

    #region Node API
    public override void _Ready()
    {
        IPBox = GetNode<TextEdit>("IP");
        Join = GetNode<Button>("Join");
        Host = GetNode<Button>("Host");
        Solo = GetNode<Button>("Solo");
        Board = GetNode<Sprite2D>("../Board");
        Multiplayer.PeerConnected += OnPlayerConnected;
        Multiplayer.PeerDisconnected += CloseGame;
        Multiplayer.ConnectionFailed += RemoveMultiplayerPeer;
        Multiplayer.ServerDisconnected += RemoveMultiplayerPeer;
        Join.Pressed += JoinGame;
        Host.Pressed += CreateGame;
        Solo.Pressed += StartSoloGame;
    }
    #endregion

    #region Shitty networking
    public void JoinGame()
    {
        var address = IPBox.Text;

        if (string.IsNullOrEmpty(address))
        {
            address = DefaultServerIP;
        }

        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateClient(address, Port);

        if (error != Error.Ok)
        {
            GD.Print(error);
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
    }

    public void CreateGame()
    {
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateServer(Port, MaxConnections);

        if (error != Error.Ok)
        {
            GD.Print(error);
            return;
        }

        Multiplayer.MultiplayerPeer = peer;
    }

    public void CloseGame(long id)
    {
        Multiplayer.MultiplayerPeer.Close();
        Multiplayer.MultiplayerPeer = null;
    }

    public void RemoveMultiplayerPeer() => Multiplayer.MultiplayerPeer = null;

    public void StartSoloGame()
    {
        RemoveMultiplayerPeer();
        PlayerLoaded();
    }

    public void PlayerLoaded()
    {
        Visible = false;
        Board.Visible = true;
    }

    private void OnPlayerConnected(long id) => RpcId(id, nameof(RegisterPlayer));

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RegisterPlayer() => PlayerLoaded();
    #endregion
}
