using ChessEngine.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ChessEngine.Engine
{
    public class EngineEvaluator
    {
        // CONFIGURABLE SETTINGS

        public int MaxSearchTimeMs = 1000;

        public HashSet<(int fromRow, int fromCol, int toRow, int toCol)> BannedMoves { get; set; }
            = new HashSet<(int, int, int, int)>();

        public void SetTime(int time)
        {
            Debug.WriteLine(time.ToString());
            if (time == 1) MaxSearchTimeMs = 1000;
            else if (time == 2) MaxSearchTimeMs = 3000;
            else if (time == 3) MaxSearchTimeMs = 5000;
            else if (time == 4) MaxSearchTimeMs = 10000;
            else if (time == 5) MaxSearchTimeMs = 30000;
            else if (time == 6) MaxSearchTimeMs = 60000;
            else if (time == 7) MaxSearchTimeMs = 120000;
            else if (time == 8) MaxSearchTimeMs = 300000;
            else MaxSearchTimeMs = 1000;
            Debug.WriteLine(MaxSearchTimeMs.ToString());
        }

        public int BaseSearchDepth { get; set; } = 5;
        public int EndgameSearchDepth { get; set; } = 7;
        public int EndgameMaterialThreshold { get; set; } = 1000;

        public int currentEvaluation;
        public int bestEvaluation;


        public void SetDepth(int depth)
        {
            BaseSearchDepth = depth;
            Debug.WriteLine(BaseSearchDepth.ToString());
        }

        public void SetEndgameDepth(int endgameDepth)
        {
            EndgameSearchDepth = BaseSearchDepth + endgameDepth;

            Debug.WriteLine(EndgameSearchDepth.ToString());
        }

        private Board _board;
        private MoveValidator _validator;
        private Stopwatch _searchTimer;
        private bool _timeoutReached;

        private Dictionary<PieceType, int> _pieceValues = new Dictionary<PieceType, int>
        {
            { PieceType.Pawn,   100   },
            { PieceType.Knight, 320   },
            { PieceType.Bishop, 330   },
            { PieceType.Rook,   500   },
            { PieceType.Queen,  900   },
            { PieceType.King,   20000 }
        };

        private Dictionary<PieceType, int[,]> _pieceSquareTables;

        public EngineEvaluator(Board board)
        {
            _board = board;
            _validator = new MoveValidator(board);
            ResetPieceSquareTables();
        }

        public void SetPieceValue(PieceType pieceType, int value) => _pieceValues[pieceType] = value;
        public int GetPieceValue(PieceType pieceType) => _pieceValues.ContainsKey(pieceType) ? _pieceValues[pieceType] : 0;

        public void ResetPieceValues()
        {
            _pieceValues[PieceType.Pawn] = 100;
            _pieceValues[PieceType.Knight] = 320;
            _pieceValues[PieceType.Bishop] = 330;
            _pieceValues[PieceType.Rook] = 500;
            _pieceValues[PieceType.Queen] = 900;
            _pieceValues[PieceType.King] = 20000;
        }

        public void ResetPieceSquareTables()
        {
            _pieceSquareTables = new Dictionary<PieceType, int[,]>();

            _pieceSquareTables[PieceType.Pawn] = new int[,]
            {
                { 0,  0,  0,  0,  0,  0,  0,  0 },
                {50, 50, 50, 50, 50, 50, 50, 50 },
                {10, 10, 20, 30, 30, 20, 10, 10 },
                { 5,  5, 10, 25, 25, 10,  5,  5 },
                { 0,  0,  0, 20, 20,  0,  0,  0 },
                { 5, -5,-10,  0,  0,-10, -5,  5 },
                { 5, 10, 10,-20,-20, 10, 10,  5 },
                { 0,  0,  0,  0,  0,  0,  0,  0 }
            };

            _pieceSquareTables[PieceType.Knight] = new int[,]
            {
                {-50,-40,-30,-30,-30,-30,-40,-50},
                {-40,-20,  0,  5,  5,  0,-20,-40},
                {-30,  5, 10, 15, 15, 10,  5,-30},
                {-30,  0, 15, 20, 20, 15,  0,-30},
                {-30,  5, 15, 20, 20, 15,  5,-30},
                {-30,  0, 10, 15, 15, 10,  0,-30},
                {-40,-20,  0,  0,  0,  0,-20,-40},
                {-50,-40,-30,-30,-30,-30,-40,-50}
            };

            _pieceSquareTables[PieceType.Bishop] = new int[,]
            {
                {-20,-10,-10,-10,-10,-10,-10,-20},
                {-10,  5,  0,  0,  0,  0,  5,-10},
                {-10, 10, 10, 10, 10, 10, 10,-10},
                {-10,  0, 10, 10, 10, 10,  0,-10},
                {-10,  5,  5, 10, 10,  5,  5,-10},
                {-10,  0,  5, 10, 10,  5,  0,-10},
                {-10,  0,  0,  0,  0,  0,  0,-10},
                {-20,-10,-10,-10,-10,-10,-10,-20}
            };

            _pieceSquareTables[PieceType.Rook] = new int[,]
            {
                { 0,  0,  0,  5,  5,  0,  0,  0 },
                {-5,  0,  0,  0,  0,  0,  0, -5 },
                {-5,  0,  0,  0,  0,  0,  0, -5 },
                {-5,  0,  0,  0,  0,  0,  0, -5 },
                {-5,  0,  0,  0,  0,  0,  0, -5 },
                {-5,  0,  0,  0,  0,  0,  0, -5 },
                { 5, 10, 10, 10, 10, 10, 10,  5 },
                { 0,  0,  0,  0,  0,  0,  0,  0 }
            };

            _pieceSquareTables[PieceType.Queen] = new int[,]
            {
                {-20,-10,-10, -5, -5,-10,-10,-20},
                {-10,  0,  5,  0,  0,  0,  0,-10},
                {-10,  5,  5,  5,  5,  5,  0,-10},
                {  0,  0,  5,  5,  5,  5,  0, -5},
                { -5,  0,  5,  5,  5,  5,  0, -5},
                {-10,  0,  5,  5,  5,  5,  0,-10},
                {-10,  0,  0,  0,  0,  0,  0,-10},
                {-20,-10,-10, -5, -5,-10,-10,-20}
            };

            _pieceSquareTables[PieceType.King] = new int[,]
            {
                {-30,-40,-40,-50,-50,-40,-40,-30},
                {-30,-40,-40,-50,-50,-40,-40,-30},
                {-30,-40,-40,-50,-50,-40,-40,-30},
                {-30,-40,-40,-50,-50,-40,-40,-30},
                {-20,-30,-30,-40,-40,-30,-30,-20},
                {-10,-20,-20,-20,-20,-20,-20,-10},
                { 20, 20,  0,  0,  0,  0, 20, 20},
                { 20, 30, 10,  0,  0, 10, 30, 20}
            };
        }

        //MOVE GENERATION

        public List<(int fromRow, int fromCol, int toRow, int toCol)> GetAllLegalMoves(PieceColor playerColor)
        {
            var legalMoves = new List<(int, int, int, int)>();

            for (int fromRow = 0; fromRow < 8; fromRow++)
            {
                for (int fromCol = 0; fromCol < 8; fromCol++)
                {
                    Piece piece = _board.Squares[fromRow, fromCol];
                    if (piece == null || piece.Color != playerColor)
                        continue;

                    for (int toRow = 0; toRow < 8; toRow++)
                    {
                        for (int toCol = 0; toCol < 8; toCol++)
                        {
                            if (fromRow == toRow && fromCol == toCol)
                                continue;

                            if (_validator.IsLegalMove(fromRow, fromCol, toRow, toCol))
                                legalMoves.Add((fromRow, fromCol, toRow, toCol));
                        }
                    }
                }
            }
            return legalMoves;
        }

        public int GetLegalMoveCount(PieceColor playerColor) => GetAllLegalMoves(playerColor).Count;
        public void DebugOutputLegalMoves(PieceColor playerColor) { /* kept for API compatibility */ }

        //MOVE ORDERING

        private const int MATE_SCORE = 1_000_000;

        private List<(int fromRow, int fromCol, int toRow, int toCol)> OrderMoves(
            List<(int fromRow, int fromCol, int toRow, int toCol)> moves,
            bool isMaximising)
        {
            return moves.OrderByDescending(move =>
            {
                Piece victim = _board.Squares[move.toRow, move.toCol];
                Piece attacker = _board.Squares[move.fromRow, move.fromCol];

                int score = 0;

                if (IsPromotionMove(move))
                    score += 100000;

                if (victim != null)
                {
                    int victimValue = GetPieceValue(victim.Type);
                    int attackerValue = GetPieceValue(attacker.Type);
                    score += (victimValue * 10) - attackerValue;
                }

                if (attacker.Type == PieceType.Pawn && victim == null)
                {
                    int forward = attacker.Color == PieceColor.White ? -1 : 1;
                    if ((move.toRow - move.fromRow) == forward)
                        score += 5;
                }

                return score;
            }).ToList();
        }

        private bool IsPromotionMove((int fromRow, int fromCol, int toRow, int toCol) move)
        {
            Piece p = _board.Squares[move.fromRow, move.fromCol];
            if (p == null || p.Type != PieceType.Pawn) return false;
            return move.toRow == 0 || move.toRow == 7;
        }

        //EVALUATION

        public int Evaluate(Board board)
        {
            int score = 0;
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Piece piece = board.Squares[row, col];
                    if (piece == null) continue;

                    int value = GetPieceValue(piece.Type);
                    int posValue = 0;

                    if (_pieceSquareTables != null && _pieceSquareTables.ContainsKey(piece.Type))
                    {
                        int tableRow = piece.Color == PieceColor.White ? row : 7 - row;
                        posValue = _pieceSquareTables[piece.Type][tableRow, col];
                    }

                    int total = value + posValue;
                    // Positive score always means White is ahead; negative means Black is ahead.
                    // The minimax perspective is handled in GetBestMove via isMaximising.
                    score += piece.Color == PieceColor.White ? total : -total;
                    
                }
            }
            return score;
        }

        //MAKE or UNDO

        private struct BoardState
        {
            public Piece MovingPiece;
            public Piece CapturedPiece;
            public Dictionary<(int, int), bool> PiecesMovedState;
        }

        private BoardState MakeMove((int fromRow, int fromCol, int toRow, int toCol) move)
        {
            Piece moving = _board.Squares[move.fromRow, move.fromCol];

            var state = new BoardState
            {
                MovingPiece = moving,
                CapturedPiece = _board.Squares[move.toRow, move.toCol],
                PiecesMovedState = new Dictionary<(int, int), bool>(_board.PiecesMoved)
            };

            _board.Squares[move.toRow, move.toCol] = moving;
            _board.Squares[move.fromRow, move.fromCol] = null;
            _board.MarkPieceMoved(move.toRow, move.toCol);

            if (moving.Type == PieceType.Pawn && (move.toRow == 0 || move.toRow == 7))
                _board.Squares[move.toRow, move.toCol] = new Piece(PieceType.Queen, moving.Color);

            return state;
        }

        private void UndoMove((int fromRow, int fromCol, int toRow, int toCol) move, BoardState state)
        {

            _board.Squares[move.fromRow, move.fromCol] = state.MovingPiece;
            _board.Squares[move.toRow, move.toCol] = state.CapturedPiece;
            _board.PiecesMoved = new Dictionary<(int, int), bool>(state.PiecesMovedState);
        }

        //QUIESCENCE

        private int Quiescence(int alpha, int beta, bool isMaximising)
        {
            int standPat = Evaluate(_board);

            if (isMaximising)
            {
                if (standPat >= beta) return standPat;
                if (alpha < standPat) alpha = standPat;
            }
            else
            {
                if (standPat <= alpha) return standPat;
                if (beta > standPat) beta = standPat;
            }

            PieceColor currentColor = isMaximising ? PieceColor.White : PieceColor.Black;
            var captures = GetAllLegalMoves(currentColor)
                .Where(m => _board.Squares[m.toRow, m.toCol] != null || IsPromotionMove(m))
                .ToList();

            captures = OrderMoves(captures, isMaximising);

            if (isMaximising)
            {
                foreach (var move in captures)
                {
                    BoardState state = MakeMove(move);
                    int score = Quiescence(alpha, beta, false);
                    UndoMove(move, state);

                    if (score >= beta) return score;
                    if (score > alpha) alpha = score;
                }
                return alpha;
            }
            else
            {
                foreach (var move in captures)
                {
                    BoardState state = MakeMove(move);
                    int score = Quiescence(alpha, beta, true);
                    UndoMove(move, state);

                    if (score <= alpha) return score;
                    if (score < beta) beta = score;
                }
                return beta;
            }
        }

        //ALPHA-BETA

        private int AlphaBeta(int depth, int alpha, int beta, bool isMaximising)
        {
            currentEvaluation = Evaluate(_board);

            if (_searchTimer != null && _searchTimer.ElapsedMilliseconds > MaxSearchTimeMs)
            {
                _timeoutReached = true;
                return Evaluate(_board);
            }

            if (depth == 0)
                return Quiescence(alpha, beta, isMaximising);

            PieceColor currentColor = isMaximising ? PieceColor.White : PieceColor.Black;
            var moves = GetAllLegalMoves(currentColor);

            if (moves.Count == 0)
            {
                if (_validator.IsKingInCheck(currentColor))
                    return isMaximising ? -MATE_SCORE + depth : MATE_SCORE - depth;
                else
                    return 0; // stalemate
            }

            moves = OrderMoves(moves, isMaximising);

            if (isMaximising)
            {
                int maxEval = int.MinValue;
                foreach (var move in moves)
                {
                    BoardState state = MakeMove(move);
                    int eval = AlphaBeta(depth - 1, alpha, beta, false);
                    UndoMove(move, state);

                    maxEval = Math.Max(maxEval, eval);
                    alpha = Math.Max(alpha, eval);
                    if (alpha >= beta) break;
                }
                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                foreach (var move in moves)
                {
                    BoardState state = MakeMove(move);
                    int eval = AlphaBeta(depth - 1, alpha, beta, true);
                    UndoMove(move, state);

                    minEval = Math.Min(minEval, eval);
                    beta = Math.Min(beta, eval);
                    if (alpha >= beta) break;
                }
                return minEval;
            }
        }

        //FIND BEST MOVE

        public (int fromRow, int fromCol, int toRow, int toCol) GetBestMove(PieceColor engineColor, int depth)
        {
            _searchTimer = Stopwatch.StartNew();
            _timeoutReached = false;

            // The engine is the maximiser when it plays White (positive scores are good for White).
            // The engine is the minimiser when it plays Black (negative scores are good for Black).
            bool engineIsMaximising = engineColor == PieceColor.White;

            int searchDepth = CalculateDynamicDepth(engineColor);

            Debug.WriteLine($"\n[ENGINE] Playing as {engineColor} | isMaximising={engineIsMaximising} | Time limit: {MaxSearchTimeMs}ms | Depth: {searchDepth}");

            int bestEval = engineIsMaximising ? int.MinValue : int.MaxValue;
            var bestMove = (-1, -1, -1, -1);

            for (int d = 1; d <= searchDepth; d++)
            {
                if (_searchTimer.ElapsedMilliseconds > MaxSearchTimeMs)
                {
                    _timeoutReached = true;
                    Debug.WriteLine($"[ENGINE] Time limit reached at depth {d}.");
                    break;
                }

                int alpha = int.MinValue;
                int beta = int.MaxValue;

                var allLegalMoves = GetAllLegalMoves(engineColor);
                var filteredMoves = allLegalMoves.Where(m => !BannedMoves.Contains(m)).ToList();
                var legalMoves = OrderMoves(
                    filteredMoves.Count > 0 ? filteredMoves : allLegalMoves,
                    engineIsMaximising);

                Debug.WriteLine($"\n[GET_BEST_MOVE] Depth {d}: {legalMoves.Count} moves for {engineColor} (Elapsed: {_searchTimer.ElapsedMilliseconds}ms)");

                foreach (var move in legalMoves)
                {
                    if (_searchTimer.ElapsedMilliseconds > MaxSearchTimeMs)
                    {
                        _timeoutReached = true;
                        break;
                    }

                    BoardState state = MakeMove(move);
                    // After the engine moves it's the opponent's turn, so flip isMaximising
                    int eval = AlphaBeta(d - 1, alpha, beta, !engineIsMaximising);
                    UndoMove(move, state);

                    Debug.WriteLine($"  Move ({move.fromRow},{move.fromCol}→{move.toRow},{move.toCol}): {eval}");

                    if (engineIsMaximising)
                    {
                        if (eval > bestEval)
                        {
                            bestEval = eval;
                            bestMove = move;
                            Debug.WriteLine($"  ✓ New best (max). Eval: {bestEval}");
                        }
                        alpha = Math.Max(alpha, eval);
                    }
                    else
                    {
                        if (eval < bestEval)
                        {
                            bestEval = eval;
                            bestMove = move;
                            Debug.WriteLine($"  ✓ New best (min). Eval: {bestEval}");
                        }
                        beta = Math.Min(beta, eval);
                    }
                }

                if (_timeoutReached)
                    break;
            }

            _searchTimer.Stop();

            bestEvaluation = bestEval;

            Debug.WriteLine($"\n[GET_BEST_MOVE] Final: ({bestMove.Item1},{bestMove.Item2}→{bestMove.Item3},{bestMove.Item4}) eval={bestEval} ({_searchTimer.ElapsedMilliseconds}ms)\n");
            return bestMove;
        }

        //DYNAMIC DEPTH

        private int CalculateDynamicDepth(PieceColor engineColor)
        {
            PieceColor opponentColor = engineColor == PieceColor.White ? PieceColor.Black : PieceColor.White;

            int engineMaterial = CalculateBoardMaterialValue(engineColor);
            int opponentMaterial = CalculateBoardMaterialValue(opponentColor);
            int materialAdvantage = engineMaterial - opponentMaterial;

            if (materialAdvantage > EndgameMaterialThreshold)
            {
                Debug.WriteLine($"[ENGINE] Endgame detected! {engineColor} material advantage: {materialAdvantage}. Using depth: {EndgameSearchDepth}");
                return EndgameSearchDepth;
            }

            return BaseSearchDepth;
        }

        public int CalculateBoardMaterialValue(PieceColor color)
        {
            int total = 0;
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                {
                    Piece piece = _board.Squares[row, col];
                    if (piece != null && piece.Color == color)
                        total += GetPieceValue(piece.Type);
                }
            
            return total;
            
        }

    }
}