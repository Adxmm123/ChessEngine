using ChessEngine.Engine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ChessEngine
{
    public partial class HomeWindow : Window
    {
        private Board _board = new Board();
        private EngineEvaluator _engine;

        private static readonly string[] Depth = { "", "1", "2", "3", "4", "5", "6", "7", "8" };
        private static readonly string[] Time = { "", "1s", "3s", "5s", "10s", "30s", "60s", "2m", "5m" };
        private int endGameDepth = 0;

        public HomeWindow()
        {
            InitializeComponent();
            _engine = new EngineEvaluator(_board);
            endGameDepth = 0;
        }

        private void DifficultySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DifficultyLabel != null)
                DifficultyLabel.Text = Depth[(int)DifficultySlider.Value];
        }

        private void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TimeLabel != null)
                TimeLabel.Text = Time[(int)TimeSlider.Value];
        }

        private void EndGameDepthCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (EndGameDepthCheckBox.IsChecked == true)
            {
                endGameDepth = 2;
            }
            else
            {
                endGameDepth = 0;
            }
        }

        private void EndGameDepthCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            endGameDepth = 0;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            int depth = (int)DifficultySlider.Value;
            int time = (int)TimeSlider.Value;

            // True  = human plays White, engine plays Black
            // False = human plays Black, engine plays White
            bool humanIsWhite = PlayWhite.IsChecked == true;

            _engine.SetTime(time);
            _engine.SetDepth(depth);
            _engine.SetEndgameDepth(endGameDepth);

            var game = new MainWindow(depth, time, endGameDepth, humanIsWhite);
            game.Show();
            this.Close();
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}