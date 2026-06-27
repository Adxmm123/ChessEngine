# ♟️ ChessEngine
This is an attempt at building my own chess engine using C# and WPF.
It is made through the use of many techniques such as alpha-beta minimax searching and iterative deepening to make it as strong and efficient as I can.


## Features
- **AI Engine** — Alpha-beta minimax system with many speed performance features
- **Adjustable Depth** — Set search depth from 0 to 8
- **Adjustable Thinking Time** — From 1 second up to 5 minutes
- **Endgame Mode** — Optional +2 depth boost when the engine has a high material advantage
- **Play as White or Black** — Board flips to match your perspective
- **Move Highlighting** — Legal moves shown on click, last move highlighted in yellow, captures highlight in red
- **Full Chess Rules** — Castling, en passant, and pawn promotion all supported
- **Repetition Avoidance** — Engine detects and avoids repeating moves to prevent draws


## Download
Download the latest release from the [Releases](https://github.com/Adxmm123/ChessEngine/releases) page.
Unzip and run "ChessEngine.exe"


## How to Play
1. On the home screen set your preferred depth and thinking time (These both impact the engine's strength)
2. Choose to play as White or Black
3. Click a piece to select it — legal moves will highlight green, captures red
4. Click the destination square to move
5. The engine will think and respond automatically
6. Feel free to play again after the round or change settings by returning home


## How the Engine Works
The engine works by using alpha-beta minimax searching with an iterating depth system to find the best moves. Iv'e attempted to make it as fast as possible through the use of alpha-beta pruning and scenarios that should not be searched. It also uses piece mapping to help guide the engine towards correct spots on the board as well as quiescence searching to avoid the horizon effect and more small features such as move ordering to help boost performance.

**Evaluation considers:**
- Material balance (piece values)
- Piece-square tables (positional bonuses per piece type)
- Checkmate detection at any depth
- Quiescence search to avoid the horizon effect on captures

**Search improvements:**
- Iterative deepening — searches incrementally deeper until time runs out
- Move ordering — captures and promotions searched first for better pruning
- Dynamic depth — automatically increases search depth in endgame positions
- Repetition avoidance — banned moves system prevents engine from looping


## Built With
- C# / .NET
- WPF (Windows Presentation Foundation)


## Screenshots
*Coming soon*

