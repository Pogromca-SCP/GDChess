using Godot;
using System.Collections.Generic;
using System.Linq;

namespace GDChess;

public enum Pieces : sbyte
{
    BKing = -6,
    BQueen,
    BRook,
    BBishop,
    BKnight,
    BPawn,
    None,
    Pawn,
    Knight,
    Bishop,
    Rook,
    Queen,
    King,
}

public enum Columns : byte
{
    LeftRook,
    LeftKnight,
    LeftBishop,
    Queen,
    King,
    RightBishop,
    RightKnight,
    RightRook,
}

public partial class CSChess : Sprite2D
{
    #region Constants
    public const int BoardSize = 8;

    public const int CellWidth = 18;

    public const int HalfWidth = CellWidth / 2;

    public Vector2[] RookDirections { get; } = [new(0, 1), new(0, -1), new(1, 0), new(-1, 0)];

    public Vector2[] BishopDirections { get; } = [new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)];

    public Vector2[] AllDirections { get; } = [new(0, 1), new(0, -1), new(1, 0), new(-1, 0),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)];

    public Vector2[] KnightDirections { get; } = [new(2, 1), new(2, -1), new(1, 2), new(1, -2),
        new(-2, 1), new(-2, -1), new(-1, 2), new(-1, -2)];

    public PackedScene TextureHolder { get; } =
        GD.Load<PackedScene>("res://scenes/texture_holder.tscn");

    public CompressedTexture2D BlackBishop { get; } =
        GD.Load<CompressedTexture2D>("res://assets/black_bishop.png");

    public CompressedTexture2D BlackKing { get; } =
        GD.Load<CompressedTexture2D>("res://assets/black_king.png");

    public CompressedTexture2D BlackKnight { get; } =
        GD.Load<CompressedTexture2D>("res://assets/black_knight.png");

    public CompressedTexture2D BlackPawn { get; } =
        GD.Load<CompressedTexture2D>("res://assets/black_pawn.png");

    public CompressedTexture2D BlackQueen { get; } =
        GD.Load<CompressedTexture2D>("res://assets/black_queen.png");

    public CompressedTexture2D BlackRook { get; } =
        GD.Load<CompressedTexture2D>("res://assets/black_rook.png");

    public CompressedTexture2D WhiteBishop { get; } =
        GD.Load<CompressedTexture2D>("res://assets/white_bishop.png");

    public CompressedTexture2D WhiteKing { get; } =
        GD.Load<CompressedTexture2D>("res://assets/white_king.png");

    public CompressedTexture2D WhiteKnight { get; } =
        GD.Load<CompressedTexture2D>("res://assets/white_knight.png");

    public CompressedTexture2D WhitePawn { get; } =
        GD.Load<CompressedTexture2D>("res://assets/white_pawn.png");

    public CompressedTexture2D WhiteQueen { get; } =
        GD.Load<CompressedTexture2D>("res://assets/white_queen.png");

    public CompressedTexture2D WhiteRook { get; } =
        GD.Load<CompressedTexture2D>("res://assets/white_rook.png");

    public CompressedTexture2D TurnWhite { get; } =
        GD.Load<CompressedTexture2D>("res://assets/turn-white.png");

    public CompressedTexture2D TurnBlack { get; } =
        GD.Load<CompressedTexture2D>("res://assets/turn-black.png");

    public CompressedTexture2D PieceMove { get; } =
        GD.Load<CompressedTexture2D>("res://assets/Piece_move.png");
    #endregion

    #region Variables
    public List<Pieces[]> Board { get; set; } = [];

    public bool IsWhiteTurn { get; set; } = true;

    public bool ChoosingMove { get; set; } = false;

    public List<Vector2> Moves { get; set; } = [];

    public Vector2 SelectedPiece { get; set; } = new();

    public Vector2? PromotionSquare { get; set; } = null;

    public bool WhiteKingBool { get; set; } = false;

    public bool BlackKingBool { get; set; } = false;

    public bool WhiteRookLeft { get; set; } = false;

    public bool WhiteRookRight { get; set; } = false;

    public bool BlackRookLeft { get; set; } = false;

    public bool BlackRookRight { get; set; } = false;

    public Vector2? EnPassant { get; set; } = null;

    public Vector2 WhiteKingPos { get; set; } = new(0, (float) Columns.King);

    public Vector2 BlackKingPos { get; set; } = new(BoardSize - 1, (float) Columns.King);

    public Node2D PiecesNode { get; set; } = null!;

    public Node2D Dots { get; set; } = null!;

    public Sprite2D Turn { get; set; } = null!;

    public Control WhitePieces { get; set; } = null!;

    public Control BlackPieces { get; set; } = null!;
    #endregion

    #region Sprite2D API
    public override void _Ready()
    {
        PiecesNode = GetNode<Node2D>("Pieces");
        Dots = GetNode<Node2D>("Dots");
        Turn = GetNode<Sprite2D>("Turn");
        WhitePieces = GetNode<Control>("../CanvasLayer/WhitePieces");
        BlackPieces = GetNode<Control>("../CanvasLayer/BlackPieces");
        Board.Add([Pieces.Rook, Pieces.Knight, Pieces.Bishop, Pieces.Queen, Pieces.King,
            Pieces.Bishop, Pieces.Knight, Pieces.Rook]);
        AddSimpleRow(Pieces.Pawn);

        for (var i = 0; i < BoardSize - 4; ++i)
        {
            AddSimpleRow(Pieces.None);
        }

        AddSimpleRow(Pieces.BPawn);
        Board.Add([Pieces.BRook, Pieces.BKnight, Pieces.BBishop, Pieces.BQueen, Pieces.BKing,
            Pieces.BBishop, Pieces.BKnight, Pieces.BRook]);
        DisplayBoard();

        foreach (var group in new[] { "white_pieces", "black_pieces" })
        {
            foreach (var button in GetTree().GetNodesInGroup(group))
            {
                if (button is Button btn)
                {
                    btn.Pressed += () => OnButtonPressed(btn);
                }
            }
        }
    }

    public override void _Input(InputEvent ev)
    {
        if (ev is not InputEventMouseButton btn || !btn.Pressed || PromotionSquare.HasValue ||
            btn.ButtonIndex != MouseButton.Left || IsMouseOut() || !IsMyTurn())
        {
            return;
        }

        var mousePos = GetGlobalMousePosition();
        var y = (int) Mathf.Snapped(mousePos.X, 0) / CellWidth;
        var x = (int) Mathf.Abs(Mathf.Snapped(mousePos.Y, 0)) / CellWidth;

        if (ChoosingMove)
        {
            if (Multiplayer.MultiplayerPeer is null)
            {
                SetMove(x, y);
            }
            else
            {
                Rpc(nameof(SetMove), x, y);
            }
        }
        else
        {
            if (Multiplayer.MultiplayerPeer is null)
            {
                SimulateOptions(x, y);
            }
            else
            {
                Rpc(nameof(SimulateOptions), x, y);
            }
        }

        ShowOptions();
        ChoosingMove = true;
    }
    #endregion

    #region Display
    public void AddSimpleRow(Pieces piece) =>
        Board.Add([piece, piece, piece, piece, piece, piece, piece, piece]);

    public bool IsMouseOut() => !GetRect().HasPoint(ToLocal(GetGlobalMousePosition()));

    public void DisplayBoard()
    {
        foreach (var child in PiecesNode.GetChildren())
        {
            child.QueueFree();
        }

        for (var i = 0; i < BoardSize; ++i)
        {
            for (var j = 0; j < BoardSize; ++j)
            {
                var holder = TextureHolder.Instantiate<Sprite2D>();
                PiecesNode.AddChild(holder);
                holder.GlobalPosition = new Vector2(j * CellWidth + HalfWidth,
                    -i * CellWidth - HalfWidth);

                holder.Texture = Board[i][j] switch
                {
                    Pieces.BKing => BlackKing,
                    Pieces.BQueen => BlackQueen,
                    Pieces.BRook => BlackRook,
                    Pieces.BBishop => BlackBishop,
                    Pieces.BKnight => BlackKnight,
                    Pieces.BPawn => BlackPawn,
                    Pieces.Pawn => WhitePawn,
                    Pieces.Knight => WhiteKnight,
                    Pieces.Bishop => WhiteBishop,
                    Pieces.Rook => WhiteRook,
                    Pieces.Queen => WhiteQueen,
                    Pieces.King => WhiteKing,
                    _ => null,
                };
            }
        }

        Turn.Texture = IsWhiteTurn ? TurnWhite : TurnBlack;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SimulateOptions(int x, int y)
    {
        SelectedPiece = new Vector2(x, y);
        Moves = GetMoves(SelectedPiece);
    }

    public void ShowOptions()
    {
        if (Moves.Count < 1)
        {
            ChoosingMove = false;
            return;
        }

        ShowDots();
    }

    public void ShowDots()
    {
        foreach (var move in Moves)
        {
            var holder = TextureHolder.Instantiate<Sprite2D>();
            Dots.AddChild(holder);
            holder.Texture = PieceMove;
            holder.GlobalPosition = new Vector2(move.Y * CellWidth + HalfWidth,
                -move.X * CellWidth - HalfWidth);
        }
    }

    public void DeleteDots()
    {
        foreach (var child in Dots.GetChildren())
        {   
            child.QueueFree();
        }
    }
    #endregion

    #region Movement
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SetMove(int x, int y)
    {
        var justNow = false;

        foreach (var move in Moves)
        {
            if (move.X == x && move.Y == y)
            {
                switch (Get(SelectedPiece))
                {
                    case Pieces.Pawn:
                    case Pieces.BPawn:
                        justNow = MovePawn(move);
                        break;
                    case Pieces.Rook:
                    case Pieces.BRook:
                        MoveRook();
                        break;
                    case Pieces.King:
                    case Pieces.BKing:
                        MoveKing(move);
                        break;
                    default:
                        break;
                }

                if (!justNow)
                {
                    EnPassant = null;
                }

                Board[x][y] = Get(SelectedPiece);
                Set(SelectedPiece, Pieces.None);
                IsWhiteTurn = !IsWhiteTurn;
                DisplayBoard();
                break;
            }
        }

        DeleteDots();
        ChoosingMove = false;

        if (IsMyTurn() && (SelectedPiece.X != x || SelectedPiece.Y != y) &&
            IsSameTeam(Board[x][y]))
        {
            if (Multiplayer.MultiplayerPeer is null)
            {
                SimulateOptions(x, y);
            }
            else
            {
                Rpc(nameof(SimulateOptions), x, y);
            }

            ShowOptions();
            ChoosingMove = true;
        }
    }

    public bool MovePawn(Vector2 move)
    {
        var promotionRow = IsWhiteTurn ? BoardSize - 1 : 0;
        var startPosition = IsWhiteTurn ? 1 : BoardSize - 2;
        var middleRow = IsWhiteTurn ? 3 : 4;

        if (move.X == promotionRow)
        {
            Promote(move);
        }
        else if (move.X == middleRow && SelectedPiece.X == startPosition)
        {
            EnPassant = move;
            return true;
        }
        else if (EnPassant.HasValue && EnPassant.Value.Y == move.Y && SelectedPiece.Y != move.Y &&
            EnPassant.Value.X == move.X)
        {
            Set(EnPassant.Value, Pieces.None);
        }

        return false;
    }

    public void MoveRook()
    {
        if (SelectedPiece.X == 0.0f)
        {
            if (SelectedPiece.Y == (float) Columns.LeftRook)
            {
                WhiteRookLeft = true;
            }
            else if (SelectedPiece.Y == (float) Columns.RightRook)
            {
                WhiteRookRight = true;
            }
        }
        else if (SelectedPiece.X == BoardSize - 1)
        {
            if (SelectedPiece.Y == (float) Columns.LeftRook)
            {
                BlackRookLeft = true;
            }
            else if (SelectedPiece.Y == (float) Columns.RightRook)
            {
                BlackRookRight = true;
            }
        }
    }

    public void MoveKing(Vector2 move)
    {
        var initialRow = IsWhiteTurn ? 0 : BoardSize - 1;
        var castlePiece = IsWhiteTurn ? Pieces.Rook : Pieces.BRook;

        if (SelectedPiece.X == initialRow && SelectedPiece.Y == (float) Columns.King)
        {
            if (IsWhiteTurn)
            {
                WhiteKingBool = true;
            }
            else
            {
                BlackKingBool = true;
            }

            if (move.Y == (float) Columns.LeftBishop)
            {
                MarkRooks();
                Set(new(initialRow, (float) Columns.LeftRook), Pieces.None);
                Set(new(initialRow, (float) Columns.Queen), castlePiece);
            }
            else if (move.Y == (float) Columns.RightKnight)
            {
                MarkRooks();
                Set(new(initialRow, (float) Columns.RightRook), Pieces.None);
                Set(new(initialRow, (float) Columns.RightBishop), castlePiece);
            }
        }

        if (IsWhiteTurn)
        {
            WhiteKingPos = move;
        }
        else
        {
            BlackKingPos = move;
        }
    }

    public void MarkRooks()
    {
        if (IsWhiteTurn)
        {
            WhiteRookLeft = true;
            WhiteRookRight = true;
        }
        else
        {
            BlackRookLeft = true;
            BlackRookRight = true;
        }
    }

    public List<Vector2> GetMoves(Vector2 selected) => Get(selected) switch
    {
        Pieces.Pawn or Pieces.BPawn => GetPawnMoves(selected),
        Pieces.Knight or Pieces.BKnight => GetKnightMoves(selected),
        Pieces.Bishop or Pieces.BBishop => GetBishopMoves(selected),
        Pieces.Rook or Pieces.BRook => GetRookMoves(selected),
        Pieces.Queen or Pieces.BQueen => GetQueenMoves(selected),
        Pieces.King or Pieces.King => GetKingMoves(selected),
        _ => []
    };

    public List<Vector2> GetRookMoves(Vector2 piecePos) =>
        GetUnlimitedMoves(Pieces.Rook, piecePos, RookDirections);

    public List<Vector2> GetBishopMoves(Vector2 piecePos) =>
        GetUnlimitedMoves(Pieces.Bishop, piecePos, BishopDirections);

    public List<Vector2> GetQueenMoves(Vector2 piecePos) =>
        GetUnlimitedMoves(Pieces.Queen, piecePos, AllDirections);

    public List<Vector2> GetKingMoves(Vector2 piecePos)
    {
        var moves = new List<Vector2>();
        Set(IsWhiteTurn ? WhiteKingPos : BlackKingPos, Pieces.None);

        foreach (var dir in AllDirections)
        {
            var pos = piecePos + dir;

            if (IsValidPosition(pos) && !IsInCheck(pos) && (IsEmpty(pos) || IsEnemy(pos)))
            {
                moves.Add(pos);
            }
        }

        if (IsWhiteTurn && !WhiteKingBool)
        {
            if (!WhiteRookLeft && LeftRookChecks())
            {
                moves.Add(new(0, (float) Columns.LeftBishop));
            }

            if (!WhiteRookRight && RightRookChecks())
            {
                moves.Add(new(0, (float) Columns.RightKnight));
            }
        }
        else if (!IsWhiteTurn && !BlackKingBool)
        {
            if (!BlackRookLeft && LeftRookChecks())
            {
                moves.Add(new(BoardSize - 1, (float) Columns.LeftBishop));
            }

            if (!BlackRookRight && RightRookChecks())
            {
                moves.Add(new(BoardSize - 1, (float) Columns.RightKnight));
            }
        }

        Set(IsWhiteTurn ? WhiteKingPos : BlackKingPos, IsWhiteTurn ? Pieces.King : Pieces.BKing);
        return moves;
    }

    public bool LeftRookChecks()
    {
        var row = IsWhiteTurn ? 0 : BoardSize - 1;
        return IsEmpty(new(row, (float) Columns.LeftKnight)) &&
            IsEmpty(new(row, (float) Columns.LeftBishop)) &&
            !IsInCheck(new(row, (float) Columns.LeftBishop)) &&
            IsEmpty(new(row, (float) Columns.Queen)) &&
            !IsInCheck(new(row, (float)Columns.Queen)) &&
            !IsInCheck(new(row, (float) Columns.King));
    }

    public bool RightRookChecks()
    {
        var row = IsWhiteTurn ? 0 : BoardSize - 1;
        return !IsInCheck(new(row, (float) Columns.King)) &&
            IsEmpty(new(row, (float) Columns.RightBishop)) &&
            !IsInCheck(new(row, (float) Columns.RightBishop)) &&
            IsEmpty(new(row, (float) Columns.RightKnight)) &&
            !IsInCheck(new(row, (float) Columns.RightKnight));
    }

    public List<Vector2> GetKnightMoves(Vector2 piecePos) =>
        GetSingleMoves(Pieces.Knight, piecePos, KnightDirections);

    public List<Vector2> GetPawnMoves(Vector2 piecePos)
    {
        var moves = new List<Vector2>();
        var direction = IsWhiteTurn ? new Vector2(1, 0) : new Vector2(-1, 0);
        var isFirstMove = IsWhiteTurn ? piecePos.X == 1.0f : piecePos.X == BoardSize - 2;
        var middleRow = IsWhiteTurn ? 4 : 3;

        if (EnPassant.HasValue && piecePos.X == middleRow &&
            Mathf.Abs(EnPassant.Value.Y - piecePos.Y) == 1.0f)
        {
            var innerPos = EnPassant.Value + direction;

            if (ValidateEnPassant(Pieces.Pawn, innerPos, piecePos))
            {
                moves.Add(innerPos);
            }
        }

        var pos = piecePos + direction;

        if (IsEmpty(pos) && ValidateMove(Pieces.Pawn, pos, piecePos))
        {
            moves.Add(pos);
        }

        foreach (var i in new sbyte[] { -1, 1 })
        {
            pos = piecePos + new Vector2(direction.X, i);

            if (IsValidPosition(pos) && IsEnemy(pos) && ValidateMove(Pieces.Pawn, pos, piecePos))
            {
                moves.Add(pos);
            }
        }

        pos = piecePos + direction * 2;

        if (isFirstMove && IsEmpty(pos) && IsEmpty(piecePos + direction) &&
            ValidateMove(Pieces.Pawn, pos, piecePos))
        {
            moves.Add(pos);
        }

        return moves;
    }

    public List<Vector2> GetUnlimitedMoves(Pieces piece, Vector2 piecePos,
        IEnumerable<Vector2> directions)
    {
        var moves = new List<Vector2>();

        foreach (var dir in directions)
        {
            var pos = piecePos + dir;

            while (IsValidPosition(pos))
            {
                if (IsEmpty(pos) && ValidateMove(piece, pos, piecePos))
                {
                    moves.Add(pos);
                }
                else if (IsEnemy(pos))
                {
                    if (ValidateMove(piece, pos, piecePos))
                    {
                        moves.Add(pos);
                    }

                    break;
                }
                else
                {
                    break;
                }

                pos += dir;
            }
        }

        return moves;
    }

    public List<Vector2> GetSingleMoves(Pieces piece, Vector2 piecePos,
        IEnumerable<Vector2> directions)
    {
        var moves = new List<Vector2>();

        foreach (var dir in directions)
        {
            var pos = piecePos + dir;

            if (IsValidPosition(pos) && (IsEmpty(pos) || IsEnemy(pos)) &&
                ValidateMove(piece, pos, piecePos))
            {
                moves.Add(pos);
            }
        }

        return moves;
    }

    public bool ValidateMove(Pieces piece, Vector2 pos, Vector2 piecePos)
    {
        var tmp = Get(pos);
        Set(pos, IsWhiteTurn ? piece : (Pieces) (-(sbyte) piece));
        Set(piecePos, Pieces.None);
        var result = !IsInCheck(IsWhiteTurn ? WhiteKingPos : BlackKingPos);
        Set(pos, tmp);
        Set(piecePos, IsWhiteTurn ? piece : (Pieces) (-(sbyte) piece));
        return result;
    }

    public bool ValidateEnPassant(Pieces piece, Vector2 pos, Vector2 piecePos)
    {
        if (!EnPassant.HasValue)
        {
            return false;
        }

        var tmp = Get(pos);
        Set(pos, IsWhiteTurn ? piece : (Pieces) (-(sbyte) piece));
        Set(piecePos, Pieces.None);
        Set(EnPassant.Value, Pieces.None);
        var result = !IsInCheck(IsWhiteTurn ? WhiteKingPos : BlackKingPos);
        Set(pos, tmp);
        Set(piecePos, IsWhiteTurn ? piece : (Pieces) (-(sbyte) piece));
        Set(EnPassant.Value, IsWhiteTurn ? (Pieces) (-(sbyte) piece) : piece);
        return result;
    }
    #endregion

    #region Misc
    public bool IsMyTurn() => Multiplayer.MultiplayerPeer is null ||
        (Multiplayer.IsServer() ? IsWhiteTurn : !IsWhiteTurn);

    public static bool IsValidPosition(Vector2 pos) => pos.X >= 0.0f && pos.X < BoardSize &&
        pos.Y >= 0.0f && pos.Y < BoardSize;

    public bool IsEmpty(Vector2 pos) => Get(pos) == Pieces.None;

    public bool IsSameTeam(Pieces piece) =>
        IsWhiteTurn ? piece > Pieces.None : piece < Pieces.None;

    public bool IsEnemy(Vector2 pos) =>
        IsWhiteTurn ? Get(pos) < Pieces.None : Get(pos) > Pieces.None;

    public void Promote(Vector2 pos)
    {
        PromotionSquare = pos;

        if (!IsMyTurn())
        {
            return;
        }

        WhitePieces.Visible = IsWhiteTurn;
        BlackPieces.Visible = !IsWhiteTurn;
    }

    private void OnButtonPressed(Button button)
    {
        var numChar = int.Parse(button.Name.ToString().Substr(0, 1));

        if (Multiplayer.MultiplayerPeer is null)
        {
            Promotion(numChar);
        }
        else
        {
            Rpc(nameof(Promotion), numChar);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void Promotion(int num)
    {
        if (!PromotionSquare.HasValue)
        {
            return;
        }

        Set(PromotionSquare.Value, (Pieces) (IsWhiteTurn ? -num : num));
        WhitePieces.Visible = false;
        BlackPieces.Visible = false;
        PromotionSquare = null;
        DisplayBoard();
    }

    public bool IsInCheck(Vector2 kingPos)
    {
        var pawnDirection = IsWhiteTurn ? 1 : -1;
        IEnumerable<Vector2> pawnAttacks = [kingPos + new Vector2(pawnDirection, 1),
            kingPos + new Vector2(pawnDirection, -1)];
        var attacker = IsWhiteTurn ? Pieces.BPawn : Pieces.Pawn;

        foreach (var at in pawnAttacks)
        {
            if (IsValidPosition(at) && Get(at) == attacker)
            {
                return true;
            }
        }

        return IsAttacked(kingPos, AllDirections, IsWhiteTurn ? Pieces.BKing : Pieces.King) ||
            IsAttackedLines(kingPos) ||
            IsAttacked(kingPos, KnightDirections, IsWhiteTurn ? Pieces.BKnight : Pieces.Knight);
    }

    public bool IsAttacked(Vector2 kingPos, IEnumerable<Vector2> directions, Pieces attacker)
    {
        foreach (var dir in directions)
        {
            var pos = kingPos + dir;

            if (IsValidPosition(pos) && Get(pos) == attacker)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsAttackedLines(Vector2 kingPos)
    {
        IEnumerable<Pieces> straightAttackers =
            IsWhiteTurn ? [Pieces.BRook, Pieces.BQueen] : [Pieces.Rook, Pieces.Queen];
        IEnumerable<Pieces> diagonalAttackers =
            IsWhiteTurn ? [Pieces.BBishop, Pieces.BQueen] : [Pieces.Bishop, Pieces.Queen];

        foreach (var dir in AllDirections)
        {
            var pos = kingPos + dir;

            while (IsValidPosition(pos))
            {
                if (!IsEmpty(pos))
                {
                    var piece = Get(pos);

                    if ((dir.X == 0.0f || dir.Y == 0.0f) && straightAttackers.Contains(piece))
                    {
                        return true;
                    }
                    else if ((dir.X != 0.0f && dir.Y != 0.0f) && diagonalAttackers.Contains(piece))
                    {
                        return true;
                    }

                    break;
                }

                pos += dir;
            }
        }

        return false;
    }

    private Pieces Get(Vector2 pos) => Board[(int) pos.X][(int) pos.Y];

    private Pieces Set(Vector2 pos, Pieces piece) => Board[(int) pos.X][(int) pos.Y] = piece;
    #endregion
}
