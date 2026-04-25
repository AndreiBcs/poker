// URL-ul tău original va rămâne comentat până e gata backend-ul
// const API_URL = "https://localhost:5000/api";

// URL public de test
const TEST_API_URL = "https://jsonplaceholder.typicode.com/users";

async function getPlayers() {
  try {
    console.log("Se face fetch la jucători de test...");

    // 1. Facem fetch de la API-ul public
    const response = await fetch(TEST_API_URL);

    if (!response.ok) throw new Error("Nu s-au putut lua jucatorii");

    const rawData = await response.json();

    // 2. Transformăm datele primite în formatul de poker așteptat de funcția ta
    const fakePokerPlayers = rawData.map(user => ({
      id: user.id,
      username: user.username, 
      chips: Math.floor(Math.random() * 5000) + 1000, // Generăm random între 1000 și 6000 de jetoane
      bet: 0,
      status: "playing"
    }));

    // 3. Trimitem jucătorii formatați către funcția de afișare
    displayPlayers(fakePokerPlayers);

  } catch (error) {
    console.error("Eroare getPlayers:", error);
  }
}

// Afisare jucatori (a rămas complet neschimbată)
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

// Ye Neymar sunny init
async function init() {
  console.log("=== START ===");
  
  await getPlayers();
}

init();
