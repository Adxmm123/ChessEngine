using ChessEngine.Engine;
using ChessEngine.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChessEngine
{
    public partial class MainWindow : Window
    {
        private Board _board = new Board();
        private MoveValidator _validator;
        private EngineEvaluator _engine;


        private PieceColor _humanColor;
        private PieceColor _engineColor;


        private bool _isPieceSelected = false;
        private int _selectedRow = -1;
        private int _selectedCol = -1;

        private List<MoveHint> _highlightedSquares = new();

        private PieceColor _currentTurn = PieceColor.White;

        private bool _isEngineThinking = false;


        private List<(int fromRow, int fromCol, int toRow, int toCol)> _engineMoveHistory = new();
        private const int RepetitionLimit = 3;


        private int _promotionRow = -1;
        private int _promotionCol = -1;
        private PieceColor _promotionColor = PieceColor.White;


        private (int fromRow, int fromCol, int toRow, int toCol)? _lastMove = null;

        private int bestEval;

        private int currentEval;

        public MainWindow(int engineDepth, int time, int endGameDepth, bool humanIsWhite)
        {
            InitializeComponent();

            _humanColor = humanIsWhite ? PieceColor.White : PieceColor.Black;
            _engineColor = humanIsWhite ? PieceColor.Black : PieceColor.White;

            _validator = new MoveValidator(_board);
            _engine = new EngineEvaluator(_board);

            _engine.SetDepth(engineDepth);
            _engine.SetTime(time);
            _engine.SetEndgameDepth(endGameDepth);



            DepthText.Text = $"{_engine.BaseSearchDepth}";
            if (time <= 6) ThinkingTimeText.Text = $"{_engine.MaxSearchTimeMs / 1000}s";
            else if (time == 7) ThinkingTimeText.Text = $"2min";
            else if (time == 8) ThinkingTimeText.Text = $"5min";
            EndgameDepthText.Text = $"{_engine.EndgameSearchDepth}";


            // If the engine plays White it must move first immediately
            if (_engineColor == PieceColor.White)
            {
                StatusText.Text = "White is thinking...";
                PlayerColourText.Text = "Black";
                RefreshBoard();
                TriggerEngineMove();
            }
            else
            {
                RefreshBoard();
            }
        }

        // UI DRAWING

        private void DrawBoard()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    // When the human plays Black flip the visual board so their pieces
                    int displayRow = _humanColor == PieceColor.Black ? 7 - row : row;
                    int displayCol = _humanColor == PieceColor.Black ? 7 - col : col;

                    var square = new Border
                    {
                        Background = GetBaseColor(displayRow, displayCol)
                    };

                    // Tag stores the LOGICAL (row, col) so click logic is colour-unaware
                    square.Tag = (displayRow, displayCol);
                    square.MouseLeftButtonDown += Square_Clicked;

                    // Highlight selected square
                    if (_isPieceSelected && displayRow == _selectedRow && displayCol == _selectedCol)
                    {
                        square.Background = new SolidColorBrush(Color.FromRgb(186, 202, 68));
                    }
                    else
                    {
                        // Last-move highlight (yellow) — legal-move hints
                        if (_lastMove.HasValue)
                        {
                            var lm = _lastMove.Value;
                            if ((displayRow == lm.fromRow && displayCol == lm.fromCol) ||
                                (displayRow == lm.toRow && displayCol == lm.toCol))
                            {
                                square.Background = new SolidColorBrush(Color.FromRgb(205, 185, 40));
                            }
                        }

                        foreach (var move in _highlightedSquares)
                        {
                            if (move.Row == displayRow && move.Col == displayCol)
                            {
                                square.Background = move.IsCapture
                                    ? new SolidColorBrush(Color.FromRgb(220, 80, 80))
                                    : new SolidColorBrush(Color.FromRgb(120, 200, 120));
                            }
                        }
                    }

                    Grid.SetRow(square, row);
                    Grid.SetColumn(square, col);
                    ChessBoard.Children.Add(square);
                }
            }
        }

        private void DrawPieces()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    // Flip visual position for Black perspective
                    int logicalRow = _humanColor == PieceColor.Black ? 7 - row : row;
                    int logicalCol = _humanColor == PieceColor.Black ? 7 - col : col;

                    Piece piece = _board.Squares[logicalRow, logicalCol];
                    if (piece == null) continue;

                    var image = new Image
                    {
                        Source = new BitmapImage(new Uri(GetImagePath(piece), UriKind.Relative)),
                        Margin = new Thickness(4),
                        IsHitTestVisible = false
                    };

                    Grid.SetRow(image, row);
                    Grid.SetColumn(image, col);
                    ChessBoard.Children.Add(image);
                }
            }
        }

        private string GetImagePath(Piece piece)
        {
            string color = piece.Color == PieceColor.White ? "white" : "black";
            string type = piece.Type.ToString().ToLower();
            return $"/Assets/{color}-{type}.png";
        }

        private SolidColorBrush GetBaseColor(int row, int col)
        {
            return (row + col) % 2 == 0
                ? new SolidColorBrush(Color.FromRgb(240, 217, 181))
                : new SolidColorBrush(Color.FromRgb(181, 136, 99));
        }

        private void RefreshBoard()
        {
            ChessBoard.Children.Clear();
            DrawBoard();
            DrawPieces();
        }

        // CLICK HANDLING

        private void Square_Clicked(object sender, MouseButtonEventArgs e)
        {
            if (_isEngineThinking)
                return;

            var clickedSquare = (Border)sender;
            var (row, col) = ((int, int))clickedSquare.Tag;

            // Only allow interaction on the human turn
            if (_currentTurn != _humanColor)
                return;

            if (!_isPieceSelected)
            {
                Piece piece = _board.Squares[row, col];

                // Must click on one of the human's own pieces
                if (piece == null || piece.Color != _humanColor)
                    return;

                _isPieceSelected = true;
                _selectedRow = row;
                _selectedCol = col;
                _highlightedSquares = GetLegalMoves(row, col);
                RefreshBoard();
                return;
            }

            //MOVE ATTEMPT
            if (_validator.IsLegalMove(_selectedRow, _selectedCol, row, col))
            {
                MovePiece(_selectedRow, _selectedCol, row, col);

                _currentTurn = _currentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;

                _isPieceSelected = false;
                _selectedRow = -1;
                _selectedCol = -1;
                _highlightedSquares.Clear();
                RefreshBoard();

                // Check game-over for the engine's side before it thinks
                if (_validator.IsCheckmate(_currentTurn))
                {
                    string winner = _currentTurn == PieceColor.White ? "Black" : "White";
                    ShowGameOver("Checkmate!", $"{winner} wins the game");
                    StatusText.Text = $"Game Over – {winner} wins!";
                    return;
                }
                else if (_validator.IsStalemate(_currentTurn))
                {
                    ShowGameOver("Stalemate!", "The game is a draw");
                    StatusText.Text = "Game Over – Stalemate!";
                    return;
                }

                // Engine's turn
                if (_currentTurn == _engineColor)
                {
                    StatusText.Text = $"{_engineColor} is thinking...";
                    TriggerEngineMove();
                    return;
                }

                UpdateStatusText();
                return;
            }

            // Clicked the already-selected piece — deselect and clear highlights
            if (row == _selectedRow && col == _selectedCol)
            {
                _isPieceSelected = false;
                _selectedRow = -1;
                _selectedCol = -1;
                _highlightedSquares.Clear();
                RefreshBoard();
                return;
            }

            // Clicked a different friendly piece — switch selection to it
            Piece clickedPiece = _board.Squares[row, col];
            if (clickedPiece != null && clickedPiece.Color == _humanColor)
            {
                _selectedRow = row;
                _selectedCol = col;
                _highlightedSquares = GetLegalMoves(row, col);
                RefreshBoard();
                return;
            }

            // Clicked an empty square or enemy piece that isn't a legal move — deselect
            _isPieceSelected = false;
            _selectedRow = -1;
            _selectedCol = -1;
            _highlightedSquares.Clear();
            RefreshBoard();
        }

        //ENGINE TRIGGER

        private void TriggerEngineMove()
        {
            _isEngineThinking = true;


            Task.Run(() =>
            {
                var boardSnapshot = DeepCopyBoard(_board);
                var engineCopy = new EngineEvaluator(boardSnapshot);
                engineCopy.SetDepth(_engine.BaseSearchDepth);
                engineCopy.MaxSearchTimeMs = _engine.MaxSearchTimeMs;
                engineCopy.EndgameSearchDepth = _engine.EndgameSearchDepth;
                engineCopy.BannedMoves = GetBannedEngineMoves();

                var bestMove = engineCopy.GetBestMove(_engineColor, engineCopy.BaseSearchDepth);

                Dispatcher.Invoke(() =>
                {
                    _isEngineThinking = false;

                    if (bestMove.fromRow != -1)
                    {
                        _engineMoveHistory.Add(bestMove);
                        MovePiece(bestMove.fromRow, bestMove.fromCol, bestMove.toRow, bestMove.toCol);
                        _currentTurn = _currentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
                        RefreshBoard();
                    }

                    // Check game-over for the human's side after the engine moves
                    if (_validator.IsCheckmate(_currentTurn))
                    {
                        string winner = _currentTurn == PieceColor.White ? "Black" : "White";
                        ShowGameOver("Checkmate!", $"{winner} wins the game");
                        StatusText.Text = $"Game Over – {winner} wins!";
                        return;
                    }
                    else if (_validator.IsStalemate(_currentTurn))
                    {
                        ShowGameOver("Stalemate!", "The game is a draw");
                        StatusText.Text = "Game Over – Stalemate!";
                        return;
                    }

                    bestEval = engineCopy.bestEvaluation;
                    currentEval = engineCopy.currentEvaluation;
                    BestMoveText.Text = $"({bestMove.fromRow},{bestMove.fromCol}) --> ({bestMove.toRow}, {bestMove.toCol})";
                    UpdateEngineText();

                    UpdateStatusText();
                });
            });
        }

        //MOVE LOGIC

        private void MovePiece(int fromRow, int fromCol, int toRow, int toCol)
        {


            Piece movingPiece = _board.Squares[fromRow, fromCol];
            if (movingPiece == null) return;

            _lastMove = (fromRow, fromCol, toRow, toCol);

            bool isCastling = movingPiece.Type == PieceType.King && Math.Abs(toCol - fromCol) == 2;
            bool isEnPassant = movingPiece.Type == PieceType.Pawn &&
                                  toCol == _board.EnPassantCol &&
                                  toRow == _board.EnPassantRow &&
                                  _board.Squares[toRow, toCol] == null;
            bool isPawnPromotion = movingPiece.Type == PieceType.Pawn &&
                                   ((movingPiece.Color == PieceColor.White && toRow == 0) ||
                                    (movingPiece.Color == PieceColor.Black && toRow == 7));

            _board.EnPassantCol = -1;
            _board.EnPassantRow = -1;

            if (movingPiece.Type == PieceType.Pawn && Math.Abs(toRow - fromRow) == 2)
            {
                int direction = movingPiece.Color == PieceColor.White ? -1 : 1;
                _board.EnPassantRow = fromRow + direction;
                _board.EnPassantCol = fromCol;
            }

            _board.Squares[toRow, toCol] = movingPiece;
            _board.Squares[fromRow, fromCol] = null;

            if (isEnPassant)
            {
                int capturedPawnRow = toRow + (movingPiece.Color == PieceColor.White ? 1 : -1);
                _board.Squares[capturedPawnRow, toCol] = null;
            }

            if (isCastling)
            {
                bool isKingside = toCol > fromCol;
                int rookFromCol = isKingside ? 7 : 0;
                int rookToCol = isKingside ? 5 : 3;
                Piece rook = _board.Squares[fromRow, rookFromCol];
                _board.Squares[fromRow, rookToCol] = rook;
                _board.Squares[fromRow, rookFromCol] = null;
                _board.MarkPieceMoved(fromRow, rookToCol);
            }

            if (isPawnPromotion)
                PromotePawn(toRow, toCol, movingPiece.Color);

            _board.MarkPieceMoved(toRow, toCol);
        }

        private void PromotePawn(int row, int col, PieceColor color)
        {
            // Show the promotion picker for the human or auto-queen for the engine
            if (color == _humanColor)
            {
                _promotionRow = row;
                _promotionCol = col;
                _promotionColor = color;
                PromotionOverlay.Visibility = Visibility.Visible;
            }
            else
            {
                _board.Squares[row, col] = new Piece(PieceType.Queen, color);
            }
        }

        private void PromotionQueenButton_Click(object sender, RoutedEventArgs e) => CompletePromotion(PieceType.Queen);
        private void PromotionRookButton_Click(object sender, RoutedEventArgs e) => CompletePromotion(PieceType.Rook);
        private void PromotionBishopButton_Click(object sender, RoutedEventArgs e) => CompletePromotion(PieceType.Bishop);
        private void PromotionKnightButton_Click(object sender, RoutedEventArgs e) => CompletePromotion(PieceType.Knight);

        private void CompletePromotion(PieceType pieceType)
        {
            _board.Squares[_promotionRow, _promotionCol] = new Piece(pieceType, _promotionColor);
            PromotionOverlay.Visibility = Visibility.Collapsed;
            RefreshBoard();
            _promotionRow = -1;
            _promotionCol = -1;
        }

        // MOVE GENERATION

        private List<MoveHint> GetLegalMoves(int fromRow, int fromCol)
        {
            var moves = new List<MoveHint>();
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    if (_validator.IsLegalMove(fromRow, fromCol, r, c))
                        moves.Add(new MoveHint
                        {
                            Row = r,
                            Col = c,
                            IsCapture = _board.Squares[r, c] != null
                        });
            return moves;
        }

        private class MoveHint
        {
            public int Row { get; set; }
            public int Col { get; set; }
            public bool IsCapture { get; set; }
        }

        // REPETITION AVOIDANCE

        private HashSet<(int, int, int, int)> GetBannedEngineMoves()
        {
            var banned = new HashSet<(int, int, int, int)>();
            var counts = new Dictionary<(int, int, int, int), int>();

            foreach (var move in _engineMoveHistory)
            {
                counts.TryGetValue(move, out int n);
                counts[move] = n + 1;
            }

            foreach (var (move, count) in counts)
                if (count >= RepetitionLimit)
                    banned.Add(move);

            return banned;
        }

        //BOARD COPY

        private Board DeepCopyBoard(Board source)
        {
            var copy = new Board();
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    Piece p = source.Squares[r, c];
                    copy.Squares[r, c] = p == null ? null : new Piece(p.Type, p.Color);
                }
            copy.EnPassantCol = source.EnPassantCol;
            copy.EnPassantRow = source.EnPassantRow;
            copy.PiecesMoved = new Dictionary<(int, int), bool>(source.PiecesMoved);
            return copy;
        }

        //UI HELPERS

        private void UpdateStatusText()
        {
            if (_validator.IsKingInCheck(_currentTurn))
                StatusText.Text = $"{_currentTurn}'s turn — ⚠️ Check!";
            else
                StatusText.Text = $"{_currentTurn}'s turn";
        }

        private void ShowGameOver(string title, string subtitle)
        {
            GameOverTitle.Text = title;
            GameOverSubtitle.Text = subtitle;
            GameOverOverlay.Visibility = Visibility.Visible;
        }

        private void PlayAgain_Clicked(object sender, RoutedEventArgs e)
        {
            _board = new Board();
            _validator = new MoveValidator(_board);
            _engine = new EngineEvaluator(_board);


            _currentTurn = PieceColor.White;
            _isPieceSelected = false;
            _selectedRow = -1;
            _selectedCol = -1;
            _highlightedSquares.Clear();
            _engineMoveHistory.Clear();
            _isEngineThinking = false;
            _lastMove = null;

            _promotionRow = -1;
            _promotionCol = -1;
            _promotionColor = PieceColor.White;

            GameOverOverlay.Visibility = Visibility.Collapsed;
            PromotionOverlay.Visibility = Visibility.Collapsed;

            int depth = _engine.BaseSearchDepth;


            RefreshBoard();

            if (_engineColor == PieceColor.White)
            {
                StatusText.Text = "White is thinking...";
                TriggerEngineMove();
            }
            else
            {
                StatusText.Text = "White's turn";
            }
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            var game = new HomeWindow();
            game.Show();
            this.Close();
        }

        private void UpdateEngineText()
        {



            if (currentEval >= 0)
            {
                EvaluationText.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0)); // Green for positive evaluation
                EvaluationText.Text = $"+{currentEval}";

            }
            else
            {
                EvaluationText.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0)); // Red for negative evaluation
                EvaluationText.Text = $"{currentEval}";
            }

            if (bestEval >= 0)
            {
                BestEvaluationText.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0)); // Green for positive evaluation
                BestEvaluationText.Text = $"+{bestEval}";

            }
            else
            {
                BestEvaluationText.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0)); // Red for negative evaluation
                BestEvaluationText.Text = $"{bestEval}";
            }



        }
    }
}