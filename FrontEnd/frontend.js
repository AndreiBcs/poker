const API_URL = "http://localhost:5000/api";

// un array basic
const deck = [
  { suit: "hearts", value: "A" },
  { suit: "spades", value: "K" },
  { suit: "diamonds", value: "10" },
  { suit: "clubs", value: "7" }
];

// Trimite deck-ul catre server
async function sendDeck() {
  try {
    const response = await fetch(`${API_URL}/deck`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify(deck)
    });

    const data = await response.json();
    console.log("Deck trimis:", data);
  } catch (error) {
    console.error("Eroare la trimitere deck:", error);
  }
}

// Ia jucatorii de la Server
async function getPlayers() {
  try {
    const response = await fetch(`${API_URL}/players`);
    const players = await response.json();

    console.log("Jucători:", players);
  } catch (error) {
    console.error("Eroare la fetch jucători:", error);
  }
}

// Fetch-ul
async function initGame() {
  await sendDeck();
  await getPlayers();
}

initGame();
