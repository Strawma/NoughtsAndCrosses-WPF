using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Resources;
using System.Speech.Synthesis;
using System.Threading;

namespace NoughtsCrossesWPF
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class GameWindow : Window
    {
        GameInfo Info;
        Color[] PColours;
        bool MusicRestart = false; //restarts main theme if needed
        Random r = new Random();
        double Time;

        int GameTurn;
        bool Start = false;
        Rectangle[,] Tiles; //tile list
        Rectangle[,] TileColors; //tiles under tiles for color
        int[,] GameScores;
        Line Winline; //for winchecking
        double[,] LinePos;

        public GameWindow(GameInfo InfoT)
        {
            Info = InfoT;
            Tiles = new Rectangle[Info.GameSettings[0], Info.GameSettings[0]]; //array for tiles
            TileColors = new Rectangle[Info.GameSettings[0], Info.GameSettings[0]]; //array for tileColors
            PColours = new Color[] { Color.FromRgb(Info.ColourBytes[0, 0], Info.ColourBytes[0, 1], Info.ColourBytes[0, 2]), Color.FromRgb(Info.ColourBytes[1, 0], Info.ColourBytes[1, 1], Info.ColourBytes[1, 2]) }; //Converts RGB Bytes to Actual Colours
            InitializeComponent();
        }

        //---------------------------------------------------------------------------------Util Stuff
        private void CloseWindow(object sender, MouseButtonEventArgs e)
        {
            Start = false; //prevents weird stuff
            if (MusicRestart == true) Utils.PlaySound("Main", Info.SoundPlayers[0], Info.Music);
            Window main = new MainWindow(Info);
            Utils.NewWindow(this, main, Info);
        }
        private void ButtonHover(object sender, MouseEventArgs e) //button highlighting
        {
            Rectangle button = sender as Rectangle;
            Utils.ButtonHover(button, Info.SoundPlayers[2], "i");
        }
        private void ButtonLeave(object sender, MouseEventArgs e)
        {
            Rectangle button = sender as Rectangle;
            Utils.ButtonHover(button, Info.SoundPlayers[2]);
        }
        private void FullScreenClick(object sender, MouseButtonEventArgs e)
        {
            Utils.FullScreen(NCFullScreen, this, Info.SoundPlayers[1]);
        }
        private void CheckState(object sender, DependencyPropertyChangedEventArgs e)
        {
            Utils.CheckState(NCFullScreen, this); //ensures fullscreen button is correct
        }


        //-----------------------------------------------------------------------------------------Other Stuff
        private void FormLoad(object sender, EventArgs e)
        {
            Time = Info.GameSettings[2];
            lblTimer.Content = "Timer: " + Time.ToString("n2");

            SolidColorBrush b = new SolidColorBrush(PColours[0]); //colours battlebar
            SolidColorBrush b2 = new SolidColorBrush(PColours[1]);
            BattleBar.Foreground = b;
            BattleBar.Background = b2;

            Winline = new Line(); //creates winline
            Grid.SetColumnSpan(Winline, Info.GameSettings[0]); //allows line to span columns
            Grid.SetRowSpan(Winline, Info.GameSettings[0]);
            Winline.Stroke = System.Windows.Media.Brushes.Black;

            Rectangle fill = rectP1Colour; //setting display icons
            Rectangle shape = rectP1Icon;
            for (int i = 0; i < 2; i++)
            {
                IconPaint(shape, fill, i);
                fill = rectP2Colour;
                shape = rectP2Icon;
            }

            //GridPlay.ShowGridLines = true; //debugging DONT LEAVE IN CODE

            for (int i = 0; i < Info.GameSettings[0]; i++) //creates grid
            {
                RowDefinition GridRow = new RowDefinition();
                ColumnDefinition GridColumn = new ColumnDefinition();

                GridPlay.ColumnDefinitions.Add(GridColumn);
                GridPlay.RowDefinitions.Add(GridRow);
            }

            for (int c = 0; c < Info.GameSettings[0]; c++) //grid column
            {
                for (int r = 0; r < Info.GameSettings[0]; r++) //grid row
                {
                    TileColors[c, r] = new Rectangle();
                    Grid.SetColumn(TileColors[c, r], c);
                    Grid.SetRow(TileColors[c, r], r);
                    TileColors[c, r].MouseLeave += this.TileLeave;
                    TileColors[c, r].MinHeight = c; //this is awful, but it works for tying values to these grids
                    TileColors[c, r].MinWidth = r;

                    GridPlay.Children.Add(TileColors[c, r]);

                    Tiles[c, r] = new Rectangle(); //creates tiles in each row
                    Tiles[c, r].Name = "N";
                    Grid.SetColumn(Tiles[c, r], c);
                    Grid.SetRow(Tiles[c, r], r);
                    Tiles[c, r].Fill = Brushes.Transparent;
                    Tiles[c, r].Stroke = System.Windows.Media.Brushes.Black; //outline
                    Tiles[c, r].HorizontalAlignment = HorizontalAlignment.Stretch; //tile fills grid
                    Tiles[c, r].VerticalAlignment = VerticalAlignment.Stretch;
                    Tiles[c, r].MouseDown += this.TileClick; //adds a click event to Tile
                    Tiles[c, r].MouseEnter += this.TileHover; //for highlighting tile
                    Tiles[c, r].MouseLeave += this.TileLeave;
                    Tiles[c, r].MinHeight = c; //awful x2
                    Tiles[c, r].MinWidth = r;

                    GridPlay.Children.Add(Tiles[c, r]);
                }
            }
        }

        private void BeginClick(object sender, MouseButtonEventArgs e) //begin button pressed
        {
            GameLabel.Content = " Battle! ";
            NCBegin.MouseEnter -= ButtonHover; //removes begin button events
            NCBegin.MouseLeave -= ButtonLeave;
            NCBegin.MouseDown -= BeginClick;
            NCBegin.Cursor = Cursors.Arrow;

            TextBox box = tbxP1Name; // makes it so player names are locked in
            for (int i = 0; i < 2; i++)
            {
                box.IsReadOnly = true;
                box.Cursor = Cursors.Arrow;
                box.BorderBrush = Brushes.Black;
                box.Foreground = Brushes.Black;
                box.Background = Brushes.White;
                box = tbxP2Name;
            }

            Reset();
        }

        private void TileClick(Object sender, MouseButtonEventArgs e) //when Tile is clicked
        {
            if (Info.GameRules[GameTurn+3] == false) //checks for ai turn
            {
                Rectangle Tile = sender as Rectangle;
                TileClickEvent(Tile);
            }
        }

        private void TileClickEvent(Rectangle Tile)
        {
            if (Start == true) //only Plays if game is begun
            {
                int Column = Convert.ToInt32(Tile.MinHeight); //gets tile row and column
                int Row = Convert.ToInt32(Tile.MinWidth);
                int Score = GameTurn + 1;

                Tile.Name = "Y"; //signifies tile being pressed
                Utils.PlaySound("TilePress", Info.SoundPlayers[4]); //plays tilepress sound
                Tile.MouseDown -= this.TileClick; //tile cannot be clicked again
                Tile.MouseEnter -= this.TileHover;
                Tile.MouseLeave -= this.TileLeave;
                Tile.Cursor = Cursors.None;

                if (Info.GameRules[1]) //if memory mode, fills with question mark
                {
                    ImageBrush b = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Resources/MemoryTile.png")));
                    TileColors[Column, Row].Fill = Brushes.Black;
                    Tile.Fill = b;
                }
                else IconPaint(Tile, TileColors[Column, Row], GameTurn); //fills tile

                GameScores[Column, Row] = Score; //adds player score to relevant grid position (for wincheck)
                int Win = WinCheck(Column, Row, Score);
                if (Win == 10) //checks for win
                {
                    int WinNum = GameTurn;
                    if (Info.GameRules[0]) WinNum = 1 - GameTurn; //reverses if inverse mode

                    if (Info.GameRules[1]) //makes line not black for memory mode
                    {
                        SolidColorBrush lb = new SolidColorBrush(PColours[GameTurn]);
                        Winline.Stroke = lb;
                    }

                    double TileWidth = GridPlay.ActualWidth / Info.GameSettings[0]; //WHY IS IT NOT 300X300??????
                    double TileHeight = GridPlay.ActualHeight / Info.GameSettings[0];
                    Winline.StrokeThickness = TileWidth / 3; //sets pen options
                    Winline.X1 = (LinePos[0, 0] + 0.5) * TileWidth; //sets line positions
                    Winline.X2 = (LinePos[1, 0] + 0.5) * TileWidth;
                    Winline.Y1 = (LinePos[0, 1] + 0.5) * TileHeight;
                    Winline.Y2 = (LinePos[1, 1] + 0.5) * TileHeight;
                    Winline.StrokeStartLineCap = PenLineCap.Triangle; //sets end of lines
                    Winline.StrokeEndLineCap = PenLineCap.Triangle;
                    GridPlay.Children.Add(Winline);
                    GridPlay.SizeChanged += ResizeLine; //adds resize event

                    TextBox[] textBoxes = new TextBox[] { tbxP1Name, tbxP2Name }; //displays winner text
                    Label[] PlayerWins = new Label[] { P1Wins, P2Wins };
                    GameLabel.Content = textBoxes[WinNum].Text + " Wins!";
                    SolidColorBrush b = new SolidColorBrush(PColours[WinNum]);
                    GameLabel.Foreground = b;
                    GameEnd(); //end game message

                    PlayerWins[WinNum].Content = Convert.ToInt16(PlayerWins[WinNum].Content) + 1; //adds win and changes bar
                    AdjustBar();

                    GridPlay.Cursor = Cursors.None;
                }
                else if (Win == 0) //checks for draw
                {
                    GameLabel.Content = "It's a Draw!";
                    GameEnd();
                }

                NextTurn();
            }
        }

        private void TileHover(Object sender, EventArgs e) //highlights tile when hovered over
        {
            if (Start == true && Info.GameRules[GameTurn + 3] == false)
            {
                Rectangle Tile = sender as Rectangle;
                SolidColorBrush brush = new SolidColorBrush();
                brush.Color = Color.FromArgb(125, 0, 0, 0);
                Tile.Fill = brush;
            }
        }
        private void TileLeave(Object sender, EventArgs e) //unhighlights tile when mouse leaves
        {
            Rectangle Tile = sender as Rectangle;
            Tile.Fill = Brushes.Transparent;
        }

        private int WinCheck(int Column, int Row, int Score)
        {
            int Streak;
            LinePos = new double[2, 2]; //2d array for Line Positions
            int[] TestPos = new int[2];
            int DirectionY;
            int DirectionX;
            bool Stop;

            for (int Vindex = -1; Vindex < 3; Vindex++) //verttical offset checked
            {
                if (Vindex == 2) //special case for checking vertically
                {
                    DirectionY = 1;
                    DirectionX = 0;
                }
                else
                {
                    DirectionY = Vindex; //moves Vertically
                    DirectionX = -1; //moves right
                }
                Streak = 1; //sets streak
                LinePos[0, 0] = Column;
                LinePos[0, 1] = Row; //sets starting point for line as current place
                LinePos[1, 0] = Column;
                LinePos[1, 1] = Row; //also sets second line point

                for (int i = 0; i < 2; i++) //checks leftwards then rightwards
                {
                    Stop = false;
                    TestPos[0] = Column + DirectionX; //sets testposition
                    TestPos[1] = Row + DirectionY;
                    while (Stop == false && TestPos[0] >= 0 && TestPos[0] < Info.GameSettings[0] && TestPos[1] >= 0 && TestPos[1] < Info.GameSettings[0]) //checks position is within bounds and streak is still ongoing
                    {
                        if (GameScores[TestPos[0], TestPos[1]] == Score)
                        {
                            Streak += 1;
                            LinePos[i, 0] = TestPos[0]; //sets new line point if streak found, offset for style
                            LinePos[i, 1] = TestPos[1];
                            if (Streak == Info.GameSettings[1]) //if a streak of win condition is met
                            {
                                return 10; //winscore
                            }
                        }
                        else Stop = true;

                        TestPos[0] += DirectionX; //moves testposition
                        TestPos[1] += DirectionY;
                    }
                    DirectionX = -DirectionX; //swaps direction to rightwards
                    DirectionY = -DirectionY;
                }
            }

            foreach (int Space in GameScores)
            {
                if (Space == 0) return -1; //spaces left == not draw
            }

            return 0; //draw
        }

        private void RematchClick(object sender, MouseButtonEventArgs e) //resets game
        {
            GameLabel.Foreground = Brushes.Black; //resets win text colour
            NCRematch.Visibility = Visibility.Hidden; //hides rematch button
            foreach (Rectangle Tile in Tiles) //clears grid
            {
                Tile.Fill = Brushes.Transparent;
                Tile.Cursor = null;
                if (Tile.Name == "Y") //resets events if need be
                {
                    Tile.Name = "N";
                    Tile.MouseDown += this.TileClick;
                    Tile.MouseEnter += this.TileHover;
                    Tile.MouseLeave += this.TileLeave;
                }
            }
            foreach (Rectangle Color in TileColors) Color.Fill = Brushes.Transparent; //clears grid colors
            GridPlay.SizeChanged -= ResizeLine; //removes line
            GridPlay.Children.Remove(Winline);
            Reset();
        }

        private void Reset() //resets variables for a match/rematch
        {
            GameScores = new int[Info.GameSettings[0], Info.GameSettings[0]]; //sets score board
            MusicRestart = true; //restarts maint theme when form closes

            GameLabel.Content = " Battle! "; //changes gamelabel
            Utils.PlaySound("Begin", Info.SoundPlayers[3]); //plays gong sound
            Utils.PlaySound("Battle", Info.SoundPlayers[0], Info.Music); //plays battle theme
            GridPlay.Cursor = Cursors.Cross; //cursor set to Player with current turn
            rectTurnIcon.Visibility = Visibility.Visible; //shows turn image
            rectTurnColour.Visibility = Visibility.Visible;
            ImageBrush b = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Resources/NCTurn.png"))); //changes begin to turn
            NCBegin.Fill = b;

            Start = true;
            GameTurn = r.Next(2); //sets turn to random player
            NextTurn(); //icon paint + ai turn
        }

        private async void AdjustBar() //adjusts battlebar when win
        {
            double P1Num = Convert.ToInt32(P1Wins.Content);
            double P2Num = Convert.ToInt32(P2Wins.Content);
            double Total = P1Num + P2Num;
            double ratio = P1Num / Total; //finds a battlebar percentage
            ratio = 100 * ratio;
            int position = Convert.ToInt32(ratio);
            int offset;
            if (position >= BattleBar.Value) offset = 1;
            else offset = -1;
            while (BattleBar.Value != position) //delays for effect!
            {
                BattleBar.Value += offset;
                await Task.Delay(20);
            }
        }

        private void GameEnd() //when game ends
        {
            MusicRestart = false;
            Start = false;
            Info.SoundPlayers[0].Stop(); //ends music, plays gong again and main theme
            Utils.PlaySound("Begin", Info.SoundPlayers[3]);
            Utils.PlaySound("Main", Info.SoundPlayers[0], Info.Music);
            NCRematch.Visibility = Visibility.Visible; //allows rematch
            SpeechSynthesizer voice = new SpeechSynthesizer(); //speaks when game over
            voice.SelectVoice("Microsoft David Desktop");
            int VoiceVolume = Convert.ToInt16(100 * Info.SoundPlayers[0].Volume * 4); //sets volume of voice
            if (VoiceVolume > 100) VoiceVolume = 100;
            voice.Volume = VoiceVolume;
            voice.SpeakAsync(GameLabel.Content.ToString());
            ImageBrush b = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Resources/NCGameOver.png"))); //changes turn to Game over
            NCBegin.Fill = b;
            rectTurnIcon.Visibility = Visibility.Hidden; //removes turn image
            rectTurnColour.Visibility = Visibility.Hidden;
        }

        private void IconPaint(Rectangle IconTile, Rectangle ColourTile, int Player) //for.. painting tiles
        {
            SolidColorBrush c = new SolidColorBrush(); //paints tile
            c.Color = PColours[Player];
            ColourTile.Fill = c;
            ImageBrush b = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Resources/" + Info.PlayerIcons[Player] + ".png"))); //changes tile icon
            IconTile.Fill = b;
        }

        private void ResizeLine(object sender, SizeChangedEventArgs e) //Resizes Line when grid resized
        {
            double TileWidth = GridPlay.ActualWidth / Info.GameSettings[0];
            double TileHeight = GridPlay.ActualHeight / Info.GameSettings[0];
            Winline.StrokeThickness = TileWidth / 3;
            Winline.X1 = (LinePos[0, 0] + 0.5) * TileWidth;
            Winline.X2 = (LinePos[1, 0] + 0.5) * TileWidth;
            Winline.Y1 = (LinePos[0, 1] + 0.5) * TileHeight;
            Winline.Y2 = (LinePos[1, 1] + 0.5) * TileHeight;
        }

        private void TextResize(object sender, SizeChangedEventArgs e) //resizes text when form resized
        {
            double Multiplier = this.ActualWidth / 640; //uses multiplier based on minimum width

            tbxP1Name.FontSize = 30 * Multiplier;
            tbxP2Name.FontSize = 30 * Multiplier;
            P1Wins.FontSize = 12 * Multiplier;
            P2Wins.FontSize = 12 * Multiplier;
            GameLabel.FontSize = 36 * Multiplier;
            lblTimer.FontSize = 24 * Multiplier;
        }

        private async void AIMove() //call when ai turn
        {
            await Task.Delay(1500); //time to click

            int GridSize = Info.GameSettings[0];
            List<Rectangle> FreeTiles = new List<Rectangle>();
            int Turn = GameTurn;
            int BestScore = int.MinValue;
            int Score;
            int[] BestMove = new int[2];

            for (int Col = 0; Col < GridSize; Col++)
            {
                for (int Row = 0; Row < GridSize; Row++)
                {
                    if (GameScores[Col, Row] == 0)
                    {
                        FreeTiles.Add(Tiles[Col, Row]); //adds spot to free spaces
                        GameScores[Col, Row] = Turn + 1; //adds score of AI turn
                        Score = Minimax(!Info.GameRules[0], Turn, Col, Row); //passes in inverse of whether inverse mode is on
                        GameScores[Col, Row] = 0; //board change only temporary

                        if (Score > BestScore) //takes best move
                        {
                            BestScore = Score;
                            BestMove[0] = Col;
                            BestMove[1] = Row;
                        }

                        if (Score == BestScore) //takes best move
                        {
                            int vary = r.Next(1, GridSize*GridSize); //adds a bit o spice to things
                            if (vary == 1)
                            {
                                BestScore = Score;
                                BestMove[0] = Col;
                                BestMove[1] = Row;
                            }
                        }
                    }
                }
            }

            if (Start == true) //helps prevent weirdness
            {
                int RandomChoose = r.Next(1, 101); //chance of making random move
                if (RandomChoose < Info.GameSettings[3 + Turn]) TileClickEvent(FreeTiles[r.Next(FreeTiles.Count)]); //based on random slider
                else TileClickEvent(Tiles[BestMove[0], BestMove[1]]); //chooses best move tile
            }
        }

        private int Minimax(bool AI, int Turn, int CheckColumn, int CheckRow, int depth = 0, int alpha = int.MinValue, int beta = int.MaxValue) //tree of outcomes
        {
            int GridSize = Info.GameSettings[0];

            int Win = WinCheck(CheckColumn,CheckRow,Turn+1);
            if (Win == 10)
            {
                if (AI) return 10-depth; //10 for max win, closer better for win
                else return depth-10; //-10 for min win, further is better for for loss
            }

            if (Win == 0) return 0; //draw, further better for draw

            int Score;
            int NewTurn = 1 - Turn; //swaps turn
            AI = !AI;

            if (depth <= 144/(GridSize*GridSize)) //something of a limit //WARNING AI GETS VERY DUMB FOR NON 3x3
            {
                if (AI) //if maximising
                {
                    for (int Col = 0; Col < Info.GameSettings[0]; Col++)
                    {
                        for (int Row = 0; Row < Info.GameSettings[0]; Row++)
                        {
                            if (GameScores[Col, Row] == 0) //checks spot is free
                            {
                                GameScores[Col, Row] = NewTurn + 1; //adds score of next player
                                Score = Minimax(true, NewTurn, Col, Row, depth + 1, alpha, beta); //calls func again
                                GameScores[Col, Row] = 0; //board change only temporary

                                alpha = Math.Max(alpha, Score);
                                if (alpha >= beta) return alpha; //pruning
                            }
                        }
                    }
                    return alpha;
                }
                else //if minimising
                {
                    //BestScore = int.MaxValue;
                    for (int Col = 0; Col < Info.GameSettings[0]; Col++)
                    {
                        for (int Row = 0; Row < Info.GameSettings[0]; Row++)
                        {
                            if (GameScores[Col, Row] == 0) //checks spot is free
                            {
                                GameScores[Col, Row] = NewTurn + 1; //adds score of AI turn
                                Score = Minimax(false, NewTurn, Col, Row, depth + 1, alpha, beta);
                                GameScores[Col, Row] = 0; //board change only temporary

                                beta = Math.Min(beta, Score);
                                if (beta <= alpha) return beta; //pruning
                            }
                        }
                    }
                    return beta;
                }
            }
            return -1;
        }

        private async void Timer(int Turn) //timer for turn
        {
            Time = Info.GameSettings[2] + 0.01;
            while (Turn == GameTurn && Start == true)
            {
                await Task.Delay(10);
                Time -= 0.01;
                lblTimer.Content = "Time: " + Time.ToString("n2");
                if (Time <= 0)
                {
                    Utils.PlaySound("Fail", Info.SoundPlayers[3]);
                    Time = Info.GameSettings[2];

                    NextTurn();
                }
            }
        }

        private void NextTurn()
        {
            if (Start == true)
            {
                if (Info.GameRules[2]) GameTurn = r.Next(2); //swaps turn depending on setting
                else GameTurn = 1 - GameTurn;

                Timer(GameTurn);
                IconPaint(rectTurnIcon, rectTurnColour, GameTurn); //paints next turn tile

                if (Info.GameRules[4] == true && Start == true && GameTurn == 1) AIMove(); //does ai turn;
                else if (Info.GameRules[3] == true && Start == true && GameTurn == 0) AIMove(); //does ai turn;
            }
        }
    }
}