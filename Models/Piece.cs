using System;
using System.Collections.Generic;
using System.Text;

namespace ChessEngine.Models
{

    public enum PieceType { None, Pawn, Rook, Knight, Bishop, Queen, King}
    public enum PieceColor { None, White, Black}

    public class Piece
    {
        public PieceType Type { get; set; }
        public PieceColor Color { get; set; }

        public Piece(PieceType type, PieceColor color)
        {
            Type = type;
            Color = color;
        }
    }
}
