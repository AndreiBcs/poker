using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace BlackjackNet
{
    public partial class fClient : Form
    {
        private TcpClient client;
        private NetworkStream clientStream;
        private StreamWriter writer;
        private StreamReader reader;
        private Thread listenThread;
        private bool isListening = false;

        private int clientId = -1; // PlayerId primit de la server

        public fClient()
        {
            InitializeComponent();

            btnDeal.Enabled = false;
            btnHit.Enabled = false;
            btnStand.Enabled = false;
            this.MaximizeBox = false;             // dezactivează butonul de maximizare
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // fereastră cu mărime fixă
        }

        // DTO-uri pentru starea jocului (să corespundă cu fServer)
        public class GameState
        {
            public List<PlayerState> Players { get; set; }
            public DealerState Dealer { get; set; }
            public int CurrentPlayerIndex { get; set; } // 0-based, -1 = joc terminat
            public string Message { get; set; }
            public bool IsGameRunning { get; set; }
            public int HostPlayerId { get; set; }
        }

        public class PlayerState
        {
            public int PlayerId { get; set; }       // 1-based
            public List<string> Hand { get; set; }
            public int Score { get; set; }
            public bool CanHit { get; set; }
        }

        public class DealerState
        {
            public List<string> Hand { get; set; }
            public int VisibleScore { get; set; }
        }

        // Conectare / deconectare
        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (btnConnect.Text == "Connect")
            {
                try
                {
                    client = new TcpClient(tbAddress.Text, 3000);
                    clientStream = client.GetStream();
                    writer = new StreamWriter(clientStream);
                    writer.AutoFlush = true;
                    reader = new StreamReader(clientStream);

                    isListening = true;
                    listenThread = new Thread(Asculta_client);
                    listenThread.IsBackground = true;
                    listenThread.Start();

                    Log("Connected to server.");
                    btnConnect.Text = "Disconnect";

                    btnDeal.Enabled = false;
                    btnHit.Enabled = false;
                    btnStand.Enabled = false;
                }
                catch (Exception ex)
                {
                    Log("Failed to connect: " + ex.Message);
                }
            }
            else
            {
                DisconnectClient();
            }
        }

        private void DisconnectClient()
        {
            try
            {
                isListening = false;

                try { if (reader != null) reader.Close(); } catch { }
                try { if (writer != null) writer.Close(); } catch { }
                try { if (client != null) client.Close(); } catch { }

                listenThread = null;
                clientId = -1;

                Log("Disconnected from server.");
                btnConnect.Text = "Connect";

                btnDeal.Enabled = false;
                btnHit.Enabled = false;
                btnStand.Enabled = false;

                playerFlowPanel.Controls.Clear();
                lblPlayerScore.Text = "Player Scor: 0";

                dealerFlowPanel.Controls.Clear();
                lblDealerScore.Text = "Dealer Scor: 0";
            }
            catch (Exception ex)
            {
                Log("Error during disconnection: " + ex.Message);
            }
        }

        // Ascultare mesaje de la server
        private void Asculta_client()
        {
            try
            {
                while (isListening)
                {
                    string line = reader.ReadLine();
                    if (line == null)
                        break;

                    // Mesaj de tip "ID:x"
                    if (line.StartsWith("ID:"))
                    {
                        string idText = line.Substring(3).Trim();
                        int id;
                        if (int.TryParse(idText, out id))
                        {
                            clientId = id;
                            Log("Your client ID is: " + clientId);

                            if (clientId == 1)
                            {
                                this.Invoke((MethodInvoker)(() =>
                                {
                                    btnDeal.Enabled = true;
                                    btnHit.Enabled = false;
                                    btnStand.Enabled = false;
                                }));
                            }
                        }
                        continue;
                    }

                    // Bloc de stare
                    if (line == "STATE")
                    {
                        List<string> lines = new List<string>();
                        lines.Add(line);

                        while (true)
                        {
                            string l = reader.ReadLine();
                            if (l == null)
                                break;

                            lines.Add(l);
                            if (l == "ENDSTATE")
                                break;
                        }

                        ParseGameStateFromText(lines);
                    }
                    else
                    {
                        Log("Server says: " + line);
                    }
                }
            }
            catch (IOException ex)
            {
                Log("Connection lost: " + ex.Message);
                DisconnectClient();
            }
            catch (Exception ex)
            {
                Log("Unhandled exception: " + ex.Message);
                DisconnectClient();
            }
        }

        // Parsează textul de stare primit de la server
        private void ParseGameStateFromText(List<string> lines)
        {
            try
            {
                GameState gameState = new GameState();
                gameState.Players = new List<PlayerState>();
                gameState.Dealer = new DealerState();
                gameState.Dealer.Hand = new List<string>();

                for (int i = 0; i < lines.Count; i++)
                {
                    string raw = lines[i];
                    string line = raw.Trim();
                    if (line == "" || line == "STATE" || line == "ENDSTATE")
                        continue;

                    if (line.StartsWith("IS_RUNNING="))
                    {
                        string val = line.Substring("IS_RUNNING=".Length);
                        gameState.IsGameRunning = (val == "1");
                    }
                    else if (line.StartsWith("CURRENT_PLAYER="))
                    {
                        string val = line.Substring("CURRENT_PLAYER=".Length);
                        int idx;
                        if (int.TryParse(val, out idx))
                            gameState.CurrentPlayerIndex = idx;
                    }
                    else if (line.StartsWith("HOST="))
                    {
                        string val = line.Substring("HOST=".Length);
                        int hostId;
                        if (int.TryParse(val, out hostId))
                            gameState.HostPlayerId = hostId;
                    }
                    else if (line.StartsWith("DEALER_SCORE="))
                    {
                        string val = line.Substring("DEALER_SCORE=".Length);
                        int dScore;
                        if (int.TryParse(val, out dScore))
                            gameState.Dealer.VisibleScore = dScore;
                    }
                    else if (line.StartsWith("DEALER_HAND="))
                    {
                        string handPart = line.Substring("DEALER_HAND=".Length);
                        gameState.Dealer.Hand = new List<string>();
                        if (handPart != "")
                        {
                            string[] parts = handPart.Split(';');
                            for (int j = 0; j < parts.Length; j++)
                            {
                                if (parts[j] != "")
                                    gameState.Dealer.Hand.Add(parts[j]);
                            }
                        }
                    }
                    else if (line.StartsWith("PLAYER="))
                    {
                        PlayerState p = new PlayerState();
                        p.Hand = new List<string>();

                        string[] parts = line.Split('|');
                        for (int j = 0; j < parts.Length; j++)
                        {
                            string part = parts[j];
                            if (part.StartsWith("PLAYER="))
                            {
                                string val = part.Substring("PLAYER=".Length);
                                int pid;
                                if (int.TryParse(val, out pid))
                                    p.PlayerId = pid;
                            }
                            else if (part.StartsWith("SCORE="))
                            {
                                string val = part.Substring("SCORE=".Length);
                                int sc;
                                if (int.TryParse(val, out sc))
                                    p.Score = sc;
                            }
                            else if (part.StartsWith("CAN_HIT="))
                            {
                                string val = part.Substring("CAN_HIT=".Length);
                                p.CanHit = (val == "1");
                            }
                            else if (part.StartsWith("HAND="))
                            {
                                string handText = part.Substring("HAND=".Length);
                                if (handText != "")
                                {
                                    string[] cards = handText.Split(';');
                                    for (int k = 0; k < cards.Length; k++)
                                    {
                                        if (cards[k] != "")
                                            p.Hand.Add(cards[k]);
                                    }
                                }
                            }
                        }

                        gameState.Players.Add(p);
                    }
                    else if (line.StartsWith("MESSAGE="))
                    {
                        gameState.Message = line.Substring("MESSAGE=".Length);
                    }
                }

                Log("GameState: CurrentPlayerIndex=" + gameState.CurrentPlayerIndex +
                    ", IsRunning=" + gameState.IsGameRunning);

                for (int i = 0; i < gameState.Players.Count; i++)
                {
                    PlayerState ps = gameState.Players[i];
                    Log("Player " + ps.PlayerId +
                        ": Score=" + ps.Score +
                        ", CanHit=" + ps.CanHit +
                        ", HandCount=" + ps.Hand.Count);
                }

                // 1. Dealer UI
                UpdateDealerUI(gameState.Dealer);

                // 2. Player UI (căutăm jucătorul nostru după PlayerId)
                PlayerState myPlayer = null;
                for (int i = 0; i < gameState.Players.Count; i++)
                {
                    if (gameState.Players[i].PlayerId == clientId)
                    {
                        myPlayer = gameState.Players[i];
                        break;
                    }
                }

                if (myPlayer != null)
                    UpdatePlayerUI(myPlayer);

                // 3. Butoane
                UpdateButtons(gameState, myPlayer);

                // 4. Mesaj de final (dacă există)
                if (!string.IsNullOrEmpty(gameState.Message))
                {
                    Log("Result message from server:");
                    Log(gameState.Message);
                }
            }
            catch (Exception ex)
            {
                Log("Failed to parse state text: " + ex.Message);
            }
        }

        private void UpdateDealerUI(DealerState dealerState)
        {
            if (dealerFlowPanel.InvokeRequired || lblDealerScore.InvokeRequired)
            {
                dealerFlowPanel.Invoke((MethodInvoker)(() => UpdateDealerUI(dealerState)));
                return;
            }

            lblDealerScore.Text = "Dealer Scor: " + dealerState.VisibleScore;

            dealerFlowPanel.Controls.Clear();

            if (dealerState.Hand == null || dealerState.Hand.Count == 0)
                return;

            // DIMENSIUNE FIXĂ pentru cărți (la fel ca la player)
            int cardWidth = 80;
            int cardHeight = 120;

            for (int i = 0; i < dealerState.Hand.Count; i++)
            {
                string cardKey = dealerState.Hand[i];
                Image img = GetCardImage(cardKey);
                if (img == null) continue;

                PictureBox pb = new PictureBox();
                pb.Width = cardWidth;
                pb.Height = cardHeight;
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                pb.Margin = new Padding(3);
                pb.Image = img;

                dealerFlowPanel.Controls.Add(pb);
            }
        }

        private void UpdatePlayerUI(PlayerState playerState)
        {
            if (playerFlowPanel.InvokeRequired || lblPlayerScore.InvokeRequired)
            {
                playerFlowPanel.Invoke((MethodInvoker)(() => UpdatePlayerUI(playerState)));
                return;
            }

            lblPlayerScore.Text = "Player Scor: " + playerState.Score;

            playerFlowPanel.Controls.Clear();

            if (playerState.Hand == null || playerState.Hand.Count == 0)
                return;

            // ACEEAȘI DIMENSIUNE ca la dealer
            int cardWidth = 80;
            int cardHeight = 120;

            for (int i = 0; i < playerState.Hand.Count; i++)
            {
                string cardKey = playerState.Hand[i];
                Image img = GetCardImage(cardKey);
                if (img == null) continue;

                PictureBox pb = new PictureBox();
                pb.Width = cardWidth;
                pb.Height = cardHeight;
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                pb.Margin = new Padding(3);
                pb.Image = img;

                playerFlowPanel.Controls.Add(pb);
            }
        }

        private void UpdateButtons(GameState gameState, PlayerState myPlayer)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)(() => UpdateButtons(gameState, myPlayer)));
                return;
            }

            bool iAmHost = (clientId == gameState.HostPlayerId);

            bool boardIsEmpty = true;
            if (gameState.Dealer.Hand != null && gameState.Dealer.Hand.Count > 0)
            {
                boardIsEmpty = false;
            }
            else
            {
                for (int i = 0; i < gameState.Players.Count; i++)
                {
                    if (gameState.Players[i].Hand != null && gameState.Players[i].Hand.Count > 0)
                    {
                        boardIsEmpty = false;
                        break;
                    }
                }
            }

            if (!gameState.IsGameRunning)
            {
                if (boardIsEmpty)
                {
                    btnRestart.Enabled = iAmHost;
                    btnDeal.Enabled = iAmHost;
                }
                else
                {
                    btnRestart.Enabled = iAmHost;
                    btnDeal.Enabled = false;
                }

                btnHit.Enabled = false;
                btnStand.Enabled = false;
                return;
            }

            btnDeal.Enabled = false;
            btnRestart.Enabled = false;

            if (myPlayer == null)
            {
                btnHit.Enabled = false;
                btnStand.Enabled = false;
                return;
            }

            bool isMyTurn = (gameState.CurrentPlayerIndex == clientId - 1);

            btnHit.Enabled = isMyTurn && myPlayer.CanHit;
            btnStand.Enabled = isMyTurn;
        }

        // Butoane de joc
        private void btnDeal_Click(object sender, EventArgs e)
        {
            SendCommand("DEAL");
        }

        private void btnHit_Click(object sender, EventArgs e)
        {
            SendCommand("HIT");
        }

        private void btnStand_Click(object sender, EventArgs e)
        {
            SendCommand("STAND");
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            SendCommand("RESTART");
        }

        private void SendCommand(string command)
        {
            try
            {
                if (client != null && client.Connected)
                {
                    writer.WriteLine(command);
                    Log("Command sent: " + command);
                }
                else
                {
                    Log("Not connected to server.");
                }
            }
            catch (Exception ex)
            {
                Log("Error sending command: " + ex.Message);
            }
        }

        // Maparea cheii la resursă (ai deja asta, îl copiez de la tine)
        private Image GetCardImage(string key)
        {
            switch (key)
            {
                case "Spate_carte_joc": return Properties.Resources.Spate_carte_joc;
                case "2_Club": return Properties.Resources._2_Club;
                case "3_Club": return Properties.Resources._3_Club;
                case "4_Club": return Properties.Resources._4_Club;
                case "5_Club": return Properties.Resources._5_Club;
                case "6_Club": return Properties.Resources._6_Club;
                case "7_Club": return Properties.Resources._7_Club;
                case "8_Club": return Properties.Resources._8_Club;
                case "9_Club": return Properties.Resources._9_Club;
                case "10_Club": return Properties.Resources._10_Club;
                case "Jack_Club": return Properties.Resources.Jack_Club;
                case "Queen_Club": return Properties.Resources.Queen_Club;
                case "King_Club": return Properties.Resources.King_Club;
                case "Ace_Club": return Properties.Resources.Ace_Club;
                case "2_Diamond": return Properties.Resources._2_Diamond;
                case "3_Diamond": return Properties.Resources._3_Diamond;
                case "4_Diamond": return Properties.Resources._4_Diamond;
                case "5_Diamond": return Properties.Resources._5_Diamond;
                case "6_Diamond": return Properties.Resources._6_Diamond;
                case "7_Diamond": return Properties.Resources._7_Diamond;
                case "8_Diamond": return Properties.Resources._8_Diamond;
                case "9_Diamond": return Properties.Resources._9_Diamond;
                case "10_Diamond": return Properties.Resources._10_Diamond;
                case "Jack_Diamond": return Properties.Resources.Jack_Diamond;
                case "Queen_Diamond": return Properties.Resources.Queen_Diamond;
                case "King_Diamond": return Properties.Resources.King_Diamond;
                case "Ace_Diamond": return Properties.Resources.Ace_Diamond;
                case "2_Heart": return Properties.Resources._2_Heart;
                case "3_Heart": return Properties.Resources._3_Heart;
                case "4_Heart": return Properties.Resources._4_Heart;
                case "5_Heart": return Properties.Resources._5_Heart;
                case "6_Heart": return Properties.Resources._6_Heart;
                case "7_Heart": return Properties.Resources._7_Heart;
                case "8_Heart": return Properties.Resources._8_Heart;
                case "9_Heart": return Properties.Resources._9_Heart;
                case "10_Heart": return Properties.Resources._10_Heart;
                case "Jack_Heart": return Properties.Resources.Jack_Heart;
                case "Queen_Heart": return Properties.Resources.Queen_Heart;
                case "King_Heart": return Properties.Resources.King_Heart;
                case "Ace_Heart": return Properties.Resources.Ace_Heart;
                case "2_Spade": return Properties.Resources._2_Spade;
                case "3_Spade": return Properties.Resources._3_Spade;
                case "4_Spade": return Properties.Resources._4_Spade;
                case "5_Spade": return Properties.Resources._5_Spade;
                case "6_Spade": return Properties.Resources._6_Spade;
                case "7_Spade": return Properties.Resources._7_Spade;
                case "8_Spade": return Properties.Resources._8_Spade;
                case "9_Spade": return Properties.Resources._9_Spade;
                case "10_Spade": return Properties.Resources._10_Spade;
                case "Jack_Spade": return Properties.Resources.Jack_Spade;
                case "Queen_Spade": return Properties.Resources.Queen_Spade;
                case "King_Spade": return Properties.Resources.King_Spade;
                case "Ace_Spade": return Properties.Resources.Ace_Spade;
                default: return null;
            }
        }

        // Logging
        private void Log(string message)
        {
            if (textBoxLog.InvokeRequired)
            {
                textBoxLog.Invoke((MethodInvoker)(() =>
                {
                    string timeText =
                        DateTime.Now.Hour.ToString("00") + ":" +
                        DateTime.Now.Minute.ToString("00") + ":" +
                        DateTime.Now.Second.ToString("00");

                    textBoxLog.AppendText(timeText + " - " + message + Environment.NewLine);
                }));
            }
            else
            {
                string timeText =
                    DateTime.Now.Hour.ToString("00") + ":" +
                    DateTime.Now.Minute.ToString("00") + ":" +
                    DateTime.Now.Second.ToString("00");

                textBoxLog.AppendText(timeText + " - " + message + Environment.NewLine);
            }
        }

        private void fClient_FormClosed(object sender, FormClosedEventArgs e)
        {
            DisconnectClient();
        }
    }
}