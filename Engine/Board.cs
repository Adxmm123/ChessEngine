using ChessEngine.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChessEngine.Engine
{
    public class Board
    {
        // 8x8 array — null means empty square
        public Piece[,] Squares { get; private set; } = new Piece[8, 8];

        public int EnPassantCol { get; set; } = -1;
        public int EnPassantRow { get; set; } = -1;

        // Track which pieces have moved (for castling rights)
        // Key: (row, col), Value: has moved
        public Dictionary<(int, int), bool> PiecesMoved { get; set; } = new Dictionary<(int, int), bool>();

        public Board()
        {
            SetupStartingPosition();
        }

        private void SetupStartingPosition()
        {
            // Black back rank (row 0)
            Squares[0, 0] = new Piece(PieceType.Rook, PieceColor.Black);
            Squares[0, 1] = new Piece(PieceType.Knight, PieceColor.Black);
            Squares[0, 2] = new Piece(PieceType.Bishop, PieceColor.Black);
            Squares[0, 3] = new Piece(PieceType.Queen, PieceColor.Black);
            Squares[0, 4] = new Piece(PieceType.King, PieceColor.Black);
            Squares[0, 5] = new Piece(PieceType.Bishop, PieceColor.Black);
            Squares[0, 6] = new Piece(PieceType.Knight, PieceColor.Black);
            Squares[0, 7] = new Piece(PieceType.Rook, PieceColor.Black);

            // Black pawns (row 1)
            for (int col = 0; col < 8; col++)
                Squares[1, col] = new Piece(PieceType.Pawn, PieceColor.Black);

            // White pawns (row 6)
            for (int col = 0; col < 8; col++)
                Squares[6, col] = new Piece(PieceType.Pawn, PieceColor.White);

            // White back rank (row 7)
            Squares[7, 0] = new Piece(PieceType.Rook, PieceColor.White);
            Squares[7, 1] = new Piece(PieceType.Knight, PieceColor.White);
            Squares[7, 2] = new Piece(PieceType.Bishop, PieceColor.White);
            Squares[7, 3] = new Piece(PieceType.Queen, PieceColor.White);
            Squares[7, 4] = new Piece(PieceType.King, PieceColor.White);
            Squares[7, 5] = new Piece(PieceType.Bishop, PieceColor.White);
            Squares[7, 6] = new Piece(PieceType.Knight, PieceColor.White);
            Squares[7, 7] = new Piece(PieceType.Rook, PieceColor.White);

            // Rows 2-5 are null (empty) by default

            // Initialize PiecesMoved - all pieces start as not moved
            PiecesMoved[(0, 0)] = false; // Black queenside rook
            PiecesMoved[(0, 4)] = false; // Black king
            PiecesMoved[(0, 7)] = false; // Black kingside rook
            PiecesMoved[(7, 0)] = false; // White queenside rook
            PiecesMoved[(7, 4)] = false; // White king
            PiecesMoved[(7, 7)] = false; // White kingside rook
        }

        public bool HasPieceMoved(int row, int col)
        {
            return PiecesMoved.ContainsKey((row, col)) && PiecesMoved[(row, col)];
        }

        public void MarkPieceMoved(int row, int col)
        {
            PiecesMoved[(row, col)] = true;
        }
    }
}