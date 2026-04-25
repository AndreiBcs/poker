const API_URL = "https://localhost:5000/api";

// Deck

const suits = ["hearts", "diamonds", "clubs", "spades"];
const values = ["2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A"];

const deck = suits.flatMap(suit =>
  values.map(value => ({
    suit,
    value
  }))
);

// Trimite deck la server
async function sendDeck() {
  try {
    const response = await fetch(`${API_URL}/deck`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify(deck)
    });

    if (!response.ok) throw new Error("Nu s-a putut trimite deck-ul");

    const data = await response.json();
    console.log("Deck trimis:", data);

  } catch (error) {
    console.error("Eroare sendDeck:", error);
  }
}

// Jucatori

// Ia jucatorii de pe server
async function getPlayers() {
  try {
    console.log("Se face fetch la jucători...");

    const response = await fetch(`${API_URL}/players`);

    if (!response.ok) throw new Error("Nu s-au putut lua jucatorii");

    const players = await response.json();
    displayPlayers(players);

  } catch (error) {
    console.error("Eroare getPlayers:", error);
  }
}

// Afisare jucatori
function displayPlayers(players) {
  console.log("=== JUCATORI ===");

  if (!players.length) {
    console.log("Nu exista jucatori pe server");
    return;
  }

  players.forEach(p => {
    console.log(`
ID: ${p.id}
Nume: ${p.username}
Chips: ${p.chips}
Bet: ${p.bet}
Status: ${p.status}
------------------------
    `);
  });
}

// Init mate

async function init() {
  console.log("=== START ===");

  await sendDeck();
  await getPlayers();
}

init();
