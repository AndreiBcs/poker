const API_URL = "https://localhost:5000/api";

// Ia jucatorii de pe server (deja se stie ce deck are fiecare)
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

// Ye Neymar sunny init
async function init() {
  console.log("=== START ===");
  
  await getPlayers();
}

init();
