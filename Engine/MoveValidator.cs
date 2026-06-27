using ChessEngine.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Text;

namespace ChessEngine.Engine
{
	public class MoveValidator
	{
		private Board _board;

		public MoveValidator(Board board)
		{
			_board = board;
		}

        public bool IsLegalMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            Piece piece = _board.Squares[fromRow, fromCol];

            // Can't move if there's no piece at the source square
            if (piece == null)
                return false;

            Piece target = _board.Squares[toRow, toCol];

            // Can't capture your own piece
            if (target != null && target.Color == piece.Color)
                return false;

            // Check piece-specific rules
            bool basicLegal = piece.Type switch
            {
                PieceType.Rook => IsLegalRookMove(fromRow, fromCol, toRow, toCol),
                PieceType.Knight => IsLegalKnightMove(fromRow, fromCol, toRow, toCol),
                PieceType.Pawn => IsLegalPawnMove(fromRow, fromCol, toRow, toCol),
                PieceType.Bishop => IsLegalBishopMove(fromRow, fromCol, toRow, toCol),
                PieceType.Queen => IsLegalQueenMove(fromRow, fromCol, toRow, toCol),
                PieceType.King => IsLegalKingMove(fromRow, fromCol, toRow, toCol),
                _ => false
            };

            if (!basicLegal) return false;

            // Simulate the move and check if king is left in check
            return !MoveLeavesKingInCheck(fromRow, fromCol, toRow, toCol, piece.Color);
        }

        private bool IsLegalRookMove(int fromRow, int fromCol, int toRow, int toCol)
		{
			// Rook must move in a straight line
			// so either the row stays the same OR the column stays the same
			bool movingStraight = (fromRow == toRow) || (fromCol == toCol);

			if (!movingStraight)
				return false;

			if(fromCol == toCol)
			{
				int step = fromRow < toRow ? 1 : -1;

				for (int row = fromRow + step; row != toRow; row += step)
				{
					if (_board.Squares[row, fromCol] != null)
					{
						return false;
					}
				}
			}

			if (fromRow == toRow)
			{
				int step = fromCol < toCol ? 1 : -1;

				for (int col = fromCol + step; col != toCol; col += step)
				{
					if (_board.Squares[fromRow, col] != null)
					{
						return false;
					}
				}
			}

			return true;
		}

		private bool IsLegalKnightMove(int fromRow, int fromCol, int toRow, int toCol)
		{
			// Work out how far it moved in each direction
			int rowDiff = Math.Abs(toRow - fromRow);
			int colDiff = Math.Abs(toCol - fromCol);

			// A knight moves in an L — 2 squares one way, 1 square the other
			if(rowDiff == 2 && colDiff == 1 || rowDiff == 1 && colDiff == 2)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		private bool IsLegalPawnMove(int fromRow, int fromCol, int toRow, int toCol)
		{
			Piece pawn = _board.Squares[fromRow, fromCol];
			Piece target = _board.Squares[toRow, toCol];

			int direction = pawn.Color == PieceColor.White ? -1 : 1;
			int startRow = pawn.Color == PieceColor.White ? 6 : 1;

			// Forward 1 square
			if (toCol == fromCol &&
				toRow == fromRow + direction &&
				target == null)
			{
				return true;
			}

			// Forward 2 squares from starting position
			if (fromRow == startRow &&
				toCol == fromCol &&
				toRow == fromRow + (2 * direction) &&
				target == null &&
				_board.Squares[fromRow + direction, fromCol] == null)  // Check intermediate square
			{
				return true;
			}

			// Diagonal capture
			if (toRow == fromRow + direction &&
				Math.Abs(toCol - fromCol) == 1 &&
				target != null &&
				target.Color != pawn.Color)
			{
				return true;
			}

            // En passant capture
            if (toRow == fromRow + direction &&
                Math.Abs(toCol - fromCol) == 1 &&
                toRow == _board.EnPassantRow &&
                toCol == _board.EnPassantCol &&
                _board.Squares[fromRow, toCol] != null &&  // Must be an enemy pawn to capture
                _board.Squares[fromRow, toCol].Color != pawn.Color)
                return true;

            return false;
		}

		private bool IsLegalBishopMove(int fromRow, int fromCol, int toRow, int toCol)
		{

			bool movingDiagonal = (toRow - fromRow == toCol - fromCol) || (toRow - fromRow == (toCol - fromCol) * -1);

			if (!movingDiagonal)
				return false;

			int rowStep = Math.Sign(toRow - fromRow);
			int colStep = Math.Sign(toCol - fromCol);

			for (
				int row = fromRow + rowStep, col = fromCol + colStep;
				row != toRow;
				row += rowStep, col += colStep)
			{
				if (_board.Squares[row, col] != null)
					return false;
			}

			return true;
		}

		private bool IsLegalQueenMove(int fromRow, int fromCol, int toRow, int toCol)
		{

			bool movingDiagonal = (toRow - fromRow == toCol - fromCol) || (toRow - fromRow == (toCol - fromCol) * -1);
            bool movingStraight = (fromRow == toRow) || (fromCol == toCol);

            if (!movingDiagonal && !movingStraight)
				return false;

			//Bishop Code
			int rowStep = Math.Sign(toRow - fromRow);
			int colStep = Math.Sign(toCol - fromCol);

			for (
				int row = fromRow + rowStep, col = fromCol + colStep;
				row != toRow;
				row += rowStep, col += colStep)
			{
				if (_board.Squares[row, col] != null)
					return false;
			}

            //Rook Code
            if (fromCol == toCol)
            {
                int step = fromRow < toRow ? 1 : -1;

                for (int row = fromRow + step; row != toRow; row += step)
                {
                    if (_board.Squares[row, fromCol] != null)
                    {
                        return false;
                    }
                }
            }

            if (fromRow == toRow)
            {
                int step = fromCol < toCol ? 1 : -1;

                for (int col = fromCol + step; col != toCol; col += step)
                {
                    if (_board.Squares[fromRow, col] != null)
                    {
                        return false;
                    }
                }
            }

            return true;
		}

        private bool IsLegalKingMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            int rowDiff = Math.Abs(toRow - fromRow);
            int colDiff = Math.Abs(toCol - fromCol);

            // Check for castling first
            if (IsCastlingMove(fromRow, fromCol, toRow, toCol))
            {
                return IsLegalCastling(fromRow, fromCol, toRow, toCol);
            }

            // King can only move 1 square in any direction
            return rowDiff <= 1 && colDiff <= 1;
        }

        private bool IsCastlingMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            // King must move 2 squares horizontally
            return fromRow == toRow && Math.Abs(toCol - fromCol) == 2;
        }

        private bool IsLegalCastling(int fromRow, int fromCol, int toRow, int toCol)
        {
            Piece king = _board.Squares[fromRow, fromCol];

            // King must not have moved
            if (_board.HasPieceMoved(fromRow, fromCol))
                return false;

            // King cannot be in check
            if (IsKingInCheck(king.Color))
                return false;

            // Determine if kingside or queenside castling
            bool isKingside = toCol > fromCol; // Moving right = kingside
            int rookCol = isKingside ? 7 : 0;
            Piece rook = _board.Squares[fromRow, rookCol];

            // Rook must exist and must be a rook
            if (rook == null || rook.Type != PieceType.Rook || rook.Color != king.Color)
                return false;

            // Rook must not have moved
            if (_board.HasPieceMoved(fromRow, rookCol))
                return false;

            // Check if path is clear between king and rook
            int minCol = Math.Min(fromCol, rookCol);
            int maxCol = Math.Max(fromCol, rookCol);

            for (int col = minCol + 1; col < maxCol; col++)
            {
                if (_board.Squares[fromRow, col] != null)
                    return false;
            }

            // Check if king moves through or into check
            int checkCol = isKingside ? fromCol + 1 : fromCol - 1; // Square king moves through
            int finalCol = isKingside ? fromCol + 2 : fromCol - 2; // Final square king lands on

            // King cannot move through check
            if (IsSquareUnderAttack(fromRow, checkCol, king.Color))
                return false;

            // King cannot move into check
            if (IsSquareUnderAttack(fromRow, finalCol, king.Color))
                return false;

            return true;
        }

        private bool IsSquareUnderAttack(int row, int col, PieceColor friendlyColor)
        {
            // Check if any enemy piece can move to this square
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Piece piece = _board.Squares[r, c];
                    if (piece == null || piece.Color == friendlyColor)
                        continue;

                    // Temporarily ignore the square we're checking
                    Piece targetPiece = _board.Squares[row, col];
                    _board.Squares[row, col] = null;

                    bool canAttack = piece.Type switch
                    {
                        PieceType.Pawn => CanPawnAttackSquare(r, c, row, col, piece.Color),
                        PieceType.Knight => IsLegalKnightMove(r, c, row, col),
                        PieceType.Bishop => IsLegalBishopMove(r, c, row, col),
                        PieceType.Rook => IsLegalRookMove(r, c, row, col),
                        PieceType.Queen => IsLegalQueenMove(r, c, row, col),
                        PieceType.King => Math.Abs(row - r) <= 1 && Math.Abs(col - c) <= 1,
                        _ => false
                    };

                    // Restore the piece
                    _board.Squares[row, col] = targetPiece;

                    if (canAttack)
                        return true;
                }
            }
            return false;
        }

        private bool CanPawnAttackSquare(int fromRow, int fromCol, int toRow, int toCol, PieceColor color)
        {
            int direction = color == PieceColor.White ? -1 : 1;
            return toRow == fromRow + direction && Math.Abs(toCol - fromCol) == 1;
        }

        public bool IsKingInCheck(PieceColor color)
        {
            // Step 1 - find where the king is
            int kingRow = -1, kingCol = -1;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Piece piece = _board.Squares[row, col];
                    if (piece != null && piece.Type == PieceType.King && piece.Color == color)
                    {
                        kingRow = row;
                        kingCol = col;
                    }
                }
            }

            // Step 2 - check if any enemy piece can attack the king
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Piece piece = _board.Squares[row, col];
                    if (piece != null && piece.Color != color)
                    {
                        if (CanAttackSquare(row, col, kingRow, kingCol))
                            return true;
                    }
                }
            }

            return false;
        }

        private bool CanAttackSquare(int fromRow, int fromCol, int toRow, int toCol)
        {
            Piece piece = _board.Squares[fromRow, fromCol];

            switch (piece.Type)
            {
                case PieceType.Rook: return IsLegalRookMove(fromRow, fromCol, toRow, toCol);
                case PieceType.Knight: return IsLegalKnightMove(fromRow, fromCol, toRow, toCol);
                case PieceType.Bishop: return IsLegalBishopMove(fromRow, fromCol, toRow, toCol);
                case PieceType.Queen: return IsLegalQueenMove(fromRow, fromCol, toRow, toCol);
                case PieceType.King:
                    int rowDiff = Math.Abs(toRow - fromRow);
                    int colDiff = Math.Abs(toCol - fromCol);
                    return rowDiff <= 1 && colDiff <= 1;
                case PieceType.Pawn: return CanPawnAttack(fromRow, fromCol, toRow, toCol);
                default: return false;
            }
        }

        private bool CanPawnAttack(int fromRow, int fromCol, int toRow, int toCol)
        {
            Piece pawn = _board.Squares[fromRow, fromCol];
            int direction = pawn.Color == PieceColor.White ? -1 : 1;

            // Pawns only attack diagonally — not the square in front
            return toRow == fromRow + direction && Math.Abs(toCol - fromCol) == 1;
        }

        private bool MoveLeavesKingInCheck(int fromRow, int fromCol, int toRow, int toCol, PieceColor color)
        {
            // Temporarily make the move
            Piece movingPiece = _board.Squares[fromRow, fromCol];
            Piece capturedPiece = _board.Squares[toRow, toCol];

            _board.Squares[toRow, toCol] = movingPiece;
            _board.Squares[fromRow, fromCol] = null;

            // Check if our king is now in check
            bool inCheck = IsKingInCheck(color);

            // Undo the move
            _board.Squares[fromRow, fromCol] = movingPiece;
            _board.Squares[toRow, toCol] = capturedPiece;

            return inCheck;
        }

        public bool IsCheckmate(PieceColor color)
        {
            // Not checkmate if not even in check
            if (!IsKingInCheck(color))
                return false;

            // Checkmate = in check AND has no legal moves
            return HasNoLegalMoves(color);
        }

        public bool IsStalemate(PieceColor color)
        {
            // Stalemate = NOT in check AND has no legal moves
            if (IsKingInCheck(color))
                return false;

            return HasNoLegalMoves(color);
        }

        private bool HasNoLegalMoves(PieceColor color)
        {
            // Iterate through all squares and check if any piece of this color has a legal move
            for (int fromRow = 0; fromRow < 8; fromRow++)
            {
                for (int fromCol = 0; fromCol < 8; fromCol++)
                {
                    Piece piece = _board.Squares[fromRow, fromCol];
                    if (piece == null || piece.Color != color)
                        continue;

                    // Try only relevant destination squares based on piece type
                    if (HasLegalMovesForPiece(fromRow, fromCol, piece.Type))
                        return false; // Found a legal move
                }
            }

            return true; // No legal moves found
        }

        private bool HasLegalMovesForPiece(int fromRow, int fromCol, PieceType type)
        {
            // Only check relevant squares based on piece type to avoid checking all 64 squares
            List<(int, int)> relevantSquares = GetRelevantSquaresForPiece(fromRow, fromCol, type);

            foreach (var (toRow, toCol) in relevantSquares)
            {
                if (IsLegalMove(fromRow, fromCol, toRow, toCol))
                    return true;
            }

            return false;
        }

        private List<(int, int)> GetRelevantSquaresForPiece(int fromRow, int fromCol, PieceType type)
        {
            var squares = new List<(int, int)>();

            switch (type)
            {
                case PieceType.Pawn:
                    AddPawnSquares(fromRow, fromCol, squares);
                    break;
                case PieceType.Knight:
                    AddKnightSquares(fromRow, fromCol, squares);
                    break;
                case PieceType.Bishop:
                    AddDiagonalSquares(fromRow, fromCol, squares);
                    break;
                case PieceType.Rook:
                    AddStraightSquares(fromRow, fromCol, squares);
                    break;
                case PieceType.Queen:
                    AddStraightSquares(fromRow, fromCol, squares);
                    AddDiagonalSquares(fromRow, fromCol, squares);
                    break;
                case PieceType.King:
                    AddKingSquares(fromRow, fromCol, squares);
                    break;
            }

            return squares;
        }

        private void AddPawnSquares(int row, int col, List<(int, int)> squares)
        {
            Piece pawn = _board.Squares[row, col];
            int direction = pawn.Color == PieceColor.White ? -1 : 1;

            // Forward move
            if (IsInBounds(row + direction, col))
                squares.Add((row + direction, col));

            // 2-square move from start
            if ((pawn.Color == PieceColor.White && row == 6) || (pawn.Color == PieceColor.Black && row == 1))
            {
                if (IsInBounds(row + 2 * direction, col))
                    squares.Add((row + 2 * direction, col));
            }

            // Captures
            if (IsInBounds(row + direction, col - 1))
                squares.Add((row + direction, col - 1));
            if (IsInBounds(row + direction, col + 1))
                squares.Add((row + direction, col + 1));
        }

        private void AddKnightSquares(int row, int col, List<(int, int)> squares)
        {
            int[] rows = { -2, -2, -1, -1, 1, 1, 2, 2 };
            int[] cols = { -1, 1, -2, 2, -2, 2, -1, 1 };

            for (int i = 0; i < 8; i++)
            {
                int newRow = row + rows[i];
                int newCol = col + cols[i];
                if (IsInBounds(newRow, newCol))
                    squares.Add((newRow, newCol));
            }
        }

        private void AddKingSquares(int row, int col, List<(int, int)> squares)
        {
            for (int r = row - 1; r <= row + 1; r++)
            {
                for (int c = col - 1; c <= col + 1; c++)
                {
                    if ((r != row || c != col) && IsInBounds(r, c))
                        squares.Add((r, c));
                }
            }
        }

        private void AddStraightSquares(int row, int col, List<(int, int)> squares)
        {
            // Horizontal
            for (int c = 0; c < 8; c++)
            {
                if (c != col)
                    squares.Add((row, c));
            }
            // Vertical
            for (int r = 0; r < 8; r++)
            {
                if (r != row)
                    squares.Add((r, col));
            }
        }

        private void AddDiagonalSquares(int row, int col, List<(int, int)> squares)
        {
            // All four diagonal directions
            int[][] directions = new int[][]
            {
                new int[] { 1, 1 },
                new int[] { 1, -1 },
                new int[] { -1, 1 },
                new int[] { -1, -1 }
            };

            foreach (var direction in directions)
            {
                for (int i = 1; i < 8; i++)
                {
                    int newRow = row + direction[0] * i;
                    int newCol = col + direction[1] * i;
                    if (IsInBounds(newRow, newCol))
                        squares.Add((newRow, newCol));
                }
            }
        }

        private bool IsInBounds(int row, int col)
        {
            return row >= 0 && row < 8 && col >= 0 && col < 8;
        }
    }
}
