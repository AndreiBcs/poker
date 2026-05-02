using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace BlackjackNet
{
    public partial class fServer : Form
    {
        private TcpListener server;
        private List<NetworkStream> clientStreams = new List<NetworkStream>();
        private List<Player> players = new List<Player>();
        private CardDeck deck;
        private Dealer dealer;

        private bool isServerRunning = false;
        private int currentPlayerIndex = 0;   // cine este la rând (0 = Player 1)
        private bool isGameRunning = false;
        private int hostPlayerId = 1;        // Player 1 este host-ul
        private string lastResultMessage = null;

        public fServer()
        {
            InitializeComponent();

            deck = new CardDeck();
            dealer = new Dealer();
            this.MaximizeBox = false;             // dezactivează butonul de maximizare
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // fereastră cu mărime fixă
        }

        // clase simple folosite pentru trimiterea stării la client
        public class GameState
        {
            public List<PlayerState> Players { get; set; }
            public DealerState Dealer { get; set; }
            public int CurrentPlayerIndex { get; set; }
            public string Message { get; set; }
            public bool IsGameRunning { get; set; }
            public int HostPlayerId { get; set; }
        }

        public class PlayerState
        {
            public int PlayerId { get; set; }
            public List<string> Hand { get; set; }
            public int Score { get; set; }
            public bool CanHit { get; set; }
        }

        public class DealerState
        {
            public List<string> Hand { get; set; }
            public int VisibleScore { get; set; }
        }

        private void btn_Click(object sender, EventArgs e)
        {
            if (isServerRunning)
            {
                Log("Server is already running.");
                return;
            }

            StartServerInternal();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            StopServer();
        }

        private void StartServerInternal()
        {
            try
            {
                server = new TcpListener(IPAddress.Any, 3000);
                server.Start();
                isServerRunning = true;
                Log("Server started (port 3000).");

                Thread t = new Thread(Asculta_Server);
                t.IsBackground = true;
                t.Start();
            }
            catch (Exception ex)
            {
                Log("Start server error: " + ex.Message);
            }
        }

        private void StopServer()
        {
            if (!isServerRunning || server == null)
            {
                Log("Server is not running.");
                return;
            }

            try
            {
                isServerRunning = false;
                server.Stop();
                server = null;
                Log("Server stopped.");
            }
            catch (Exception ex)
            {
                Log("Failed to stop the server: " + ex.Message);
            }
        }

        // acceptă clienți noi
        private void Asculta_Server()
        {
            try
            {
                while (isServerRunning)
                {
                    try
                    {
                        TcpClient client = server.AcceptTcpClient();
                        NetworkStream stream = client.GetStream();
                        clientStreams.Add(stream);

                        // adăugăm un nou jucător
                        Player newPlayer = new Player();
                        players.Add(newPlayer);
                        int playerIndex = players.Count - 1; // 0-based

                        Thread clientThread = new Thread(() => HandleClient(stream, playerIndex));
                        clientThread.IsBackground = true;
                        clientThread.Start();

                        Log("New client connected as Player " + (playerIndex + 1));
                    }
                    catch (SocketException)
                    {
                        Log("Server stopped or connection interrupted while accepting.");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Unhandled exception in Asculta_Server: " + ex.Message);
            }
        }

        // comunică cu un client
        private void HandleClient(NetworkStream stream, int playerIndex)
        {
            using (StreamReader reader = new StreamReader(stream))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.AutoFlush = true;

                try
                {
                    // trimitem ID-ul clientului (1-based)
                    writer.WriteLine("ID:" + (playerIndex + 1));
                    Log("Assigned ID " + (playerIndex + 1) + " to client.");

                    while (true)
                    {
                        string command = reader.ReadLine();
                        if (command == null)
                            break;

                        Log("Player " + (playerIndex + 1) + " sent: " + command);

                        int thisPlayerId = playerIndex + 1;

                        if (command == "RESTART")
                        {
                            if (thisPlayerId == hostPlayerId)
                            {
                                RestartGameState();
                            }
                            else
                            {
                                writer.WriteLine("Only the host can restart the game.");
                                Log("Player " + thisPlayerId + " tried RESTART but host is Player " + hostPlayerId);
                            }
                        }
                        else if (command == "DEAL")
                        {
                            if (isGameRunning)
                            {
                                writer.WriteLine("GAME_RUNNING");
                                Log("Player " + thisPlayerId + " tried DEAL but game is already running.");
                            }
                            else
                            {
                                if (thisPlayerId == hostPlayerId)
                                {
                                    StartGame();
                                }
                                else
                                {
                                    writer.WriteLine("Only the host can start the game.");
                                    Log("Player " + thisPlayerId + " tried DEAL but host is Player " + hostPlayerId);
                                }
                            }
                        }
                        else if (command == "HIT")
                        {
                            if (!isGameRunning)
                            {
                                writer.WriteLine("NO_GAME_RUNNING");
                                continue;
                            }

                            if (playerIndex != currentPlayerIndex)
                            {
                                writer.WriteLine("It's not your turn.");
                                continue;
                            }

                            PlayerHit(playerIndex);
                        }
                        else if (command == "STAND")
                        {
                            if (!isGameRunning)
                            {
                                writer.WriteLine("NO_GAME_RUNNING");
                                continue;
                            }

                            if (playerIndex != currentPlayerIndex)
                            {
                                writer.WriteLine("It's not your turn.");
                                continue;
                            }

                            PlayerStand(playerIndex);
                        }

                        // după orice comandă care a modificat starea,
                        // trimitem starea la toți clienții
                        BroadcastGameState();
                    }
                }
                catch (Exception ex)
                {
                    Log("Player " + (playerIndex + 1) + " disconnected with error: " + ex.Message);
                }
                finally
                {
                    clientStreams.Remove(stream);

                    if (playerIndex >= 0 && playerIndex < players.Count)
                    {
                        players[playerIndex].ClearHand();
                    }

                    bool allEmpty = true;
                    foreach (Player p in players)
                    {
                        if (p.Hand.Count > 0)
                        {
                            allEmpty = false;
                            break;
                        }
                    }

                    if (allEmpty)
                    {
                        isGameRunning = false;
                        currentPlayerIndex = -1;
                        Log("All players disconnected or cleared. Game stopped.");
                    }

                    BroadcastGameState();
                }
            }
        }

        // pornește o rundă nouă
        private void StartGame()
        {
            if (isGameRunning)
            {
                Log("Game is already running. DEAL ignored.");
                return;
            }

            Log("Starting a new game...");

            deck.Reset();
            dealer.ClearHand();

            foreach (Player p in players)
                p.ClearHand();

            foreach (Player p in players)
            {
                p.AddCard(deck.DrawCard());
                p.AddCard(deck.DrawCard());
            }

            dealer.AddCard(deck.DrawCard());
            dealer.AddCard(deck.DrawCard());

            currentPlayerIndex = 0;
            isGameRunning = true;
            Log("Game started. Waiting for Player 1.");
        }

        // doar resetează starea fără cărți pe masă
        private void RestartGameState()
        {
            Log("Restarting game state (no cards, waiting for Deal)...");

            deck.Reset();
            dealer.ClearHand();

            foreach (Player p in players)
                p.ClearHand();

            currentPlayerIndex = -1;
            isGameRunning = false;
        }

        private void PlayerHit(int playerIndex)
        {
            Player player = players[playerIndex];
            PlayingCard card = deck.DrawCard();
            player.AddCard(card);

            Log("Player " + (playerIndex + 1) + " draws: " + card.ResourceKey);

            int score = player.CalculateScore();

            if (score > 21)
            {
                Log("Player " + (playerIndex + 1) + " busts (Score: " + score + ")");

                if (AllPlayersBusted())
                {
                    Log("All players have busted. Dealer will reveal hand without drawing.");

                    isGameRunning = false;
                    currentPlayerIndex = -1;
                    DetermineWinner();
                }
                else
                {
                    if (playerIndex == players.Count - 1)
                    {
                        DealerTurn();
                    }
                    else
                    {
                        NextPlayer();
                    }
                }
            }
            else if (score == 21)
            {
                Log("Player " + (playerIndex + 1) + " reached 21.");
            }
        }

        private void PlayerStand(int playerIndex)
        {
            Log("Player " + (playerIndex + 1) + " stands (Score: " + players[playerIndex].CalculateScore() + ").");

            if (playerIndex == players.Count - 1)
            {
                DealerTurn();
            }
            else
            {
                NextPlayer();
            }
        }

        private void NextPlayer()
        {
            if (players.Count == 0)
                return;

            currentPlayerIndex++;
            if (currentPlayerIndex >= players.Count)
                currentPlayerIndex = 0;

            Log("It's now Player " + (currentPlayerIndex + 1) + "'s turn.");
        }

        private void DealerTurn()
        {
            Log("Dealer's turn...");

            while (dealer.CalculateScore() < 17 && deck.GetTotalNumberOfCards() > 0)
            {
                PlayingCard card = deck.DrawCard();
                dealer.AddCard(card);
                Log("Dealer draws: " + card.ResourceKey);
            }

            Log("Dealer's final score: " + dealer.CalculateScore());

            DetermineWinner();

            isGameRunning = false;
            currentPlayerIndex = -1;

            Log("Round over. Waiting for Restart/Deal from host.");
        }

        private bool AllPlayersBusted()
        {
            foreach (Player p in players)
            {
                if (p.CalculateScore() <= 21)
                    return false;
            }
            return true;
        }

        private void DetermineWinner()
        {
            Log("Determining the winner...");

            int dealerScore = dealer.CalculateScore();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine("Dealer score: " + dealerScore);

            for (int i = 0; i < players.Count; i++)
            {
                Player p = players[i];
                int playerScore = p.CalculateScore();
                int pIndex = i + 1;

                string line;
                if (playerScore > 21)
                {
                    line = "Player " + pIndex + " busted with " + playerScore + ". Dealer wins.";
                }
                else if (dealerScore > 21)
                {
                    line = "Dealer busted. Player " + pIndex + " wins with " + playerScore + ".";
                }
                else if (playerScore > dealerScore)
                {
                    line = "Player " + pIndex + " wins with " + playerScore + " vs dealer " + dealerScore + ".";
                }
                else if (playerScore < dealerScore)
                {
                    line = "Player " + pIndex + " loses with " + playerScore + " vs dealer " + dealerScore + ".";
                }
                else
                {
                    line = "Player " + pIndex + " pushes with dealer (tie at " + playerScore + ").";
                }

                Log(line);
                sb.AppendLine(line);
            }

            lastResultMessage = sb.ToString().TrimEnd();
        }

        private string SerializeGameStateText()
        {
            List<string> dealerHandImages = new List<string>();
            int visibleScore = 0;

            if (dealer.Hand.Count > 0)
            {
                if (isGameRunning)
                {
                    PlayingCard firstCard = dealer.Hand[0];
                    dealerHandImages.Add(firstCard.ResourceKey);
                    visibleScore = firstCard.Value;
                    if (dealer.Hand.Count >= 2 && firstCard.CardType == PlayingCard.Type.Ace)
                    {
                        visibleScore = 11;
                    }
                    if (dealer.Hand.Count >= 2)
                    {
                        dealerHandImages.Add("Spate_carte_joc");
                    }

                    for (int i = 2; i < dealer.Hand.Count; i++)
                    {
                        PlayingCard card = dealer.Hand[i];
                        dealerHandImages.Add(card.ResourceKey);
                        visibleScore += card.Value;
                    }
                }
                else
                {
                    foreach (PlayingCard card in dealer.Hand)
                    {
                        dealerHandImages.Add(card.ResourceKey);
                    }
                    visibleScore = dealer.CalculateScore();
                }
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine("STATE");
            sb.AppendLine("IS_RUNNING=" + (isGameRunning ? 1 : 0));
            sb.AppendLine("CURRENT_PLAYER=" + currentPlayerIndex);
            sb.AppendLine("HOST=" + hostPlayerId);
            sb.AppendLine("DEALER_SCORE=" + visibleScore);

            // construim DEALER_HAND fără string.Join
            string dealerHandLine = "";
            for (int i = 0; i < dealerHandImages.Count; i++)
            {
                dealerHandLine += dealerHandImages[i];
                if (i < dealerHandImages.Count - 1)
                {
                    dealerHandLine += ";";
                }
            }
            sb.AppendLine("DEALER_HAND=" + dealerHandLine);

            // jucători
            for (int i = 0; i < players.Count; i++)
            {
                Player p = players[i];
                int playerId = i + 1;
                int score = p.CalculateScore();
                bool canHit = score < 21;

                // construim HAND direct din p.Hand fără listă intermediară
                string hand = "";
                for (int j = 0; j < p.Hand.Count; j++)
                {
                    hand += p.Hand[j].ResourceKey;
                    if (j < p.Hand.Count - 1)
                    {
                        hand += ";";
                    }
                }

                sb.AppendLine(
                    "PLAYER=" + playerId +
                    "|SCORE=" + score +
                    "|CAN_HIT=" + (canHit ? 1 : 0) +
                    "|HAND=" + hand
                );
            }

            if (!string.IsNullOrEmpty(lastResultMessage))
            {
                string oneLineMsg = lastResultMessage.Replace("\r", " ").Replace("\n", " ");
                sb.AppendLine("MESSAGE=" + oneLineMsg);
            }
            else
            {
                sb.AppendLine("MESSAGE=");
            }

            sb.AppendLine("ENDSTATE");
            lastResultMessage = null;

            return sb.ToString();
        }

        private void BroadcastGameState()
        {
            string stateText = SerializeGameStateText();

            foreach (NetworkStream clientStream in new List<NetworkStream>(clientStreams))
            {
                try
                {
                    StreamWriter writer = new StreamWriter(clientStream);
                    writer.AutoFlush = true;
                    writer.WriteLine(stateText);
                }
                catch (Exception ex)
                {
                    Log("Failed to broadcast game state: " + ex.Message);
                }
            }
        }

        private void Log(string message)
        {
            try
            {
                if (textBox1.InvokeRequired)
                {
                    textBox1.Invoke((MethodInvoker)(() =>
                    {
                        string timeText =
                            DateTime.Now.Hour.ToString("00") + ":" +
                            DateTime.Now.Minute.ToString("00") + ":" +
                            DateTime.Now.Second.ToString("00");

                        textBox1.AppendText(timeText + " - " + message + Environment.NewLine);
                    }));
                }
                else
                {
                    string timeText =
                        DateTime.Now.Hour.ToString("00") + ":" +
                        DateTime.Now.Minute.ToString("00") + ":" +
                        DateTime.Now.Second.ToString("00");

                    textBox1.AppendText(timeText + " - " + message + Environment.NewLine);
                }
            }
            catch
            {
                
            }
        }
    }
}