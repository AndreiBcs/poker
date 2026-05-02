const LOBBIES = [
    { name: "Rapid Ace", code: "RAP-216", players: 4, stakes: "5/10" },
    { name: "Noapte Lunga", code: "NLP-983", players: 2, stakes: "10/20" },
    { name: "Cupa Orasului", code: "CUP-114", players: 6, stakes: "2/4" },
    { name: "Dealerul Sef", code: "DFS-330", players: 1, stakes: "25/50" }
];

const byId = (id) => document.getElementById(id);

const renderLobbies = () => {
    const list = byId("lobby-list");
    if (!list) {
        return;
    }

    list.innerHTML = "";
    LOBBIES.forEach((lobby) => {
        const item = document.createElement("div");
        item.className = "lobby-item";
        item.innerHTML = `
      <div>
        <strong>${lobby.name}</strong>
        <div class="muted">Miza: ${lobby.stakes} | Cod: ${lobby.code}</div>
      </div>
      <div>
        <span class="badge">${lobby.players}/8 jucatori</span>
        <a class="button alt" href="game.html">Intra</a>
      </div>
    `;
        list.appendChild(item);
    });
};

const wireLobbySearch = () => {
    const search = byId("lobby-search");
    if (!search) {
        return;
    }

    search.addEventListener("input", (event) => {
        const value = event.target.value.trim().toLowerCase();
        const list = byId("lobby-list");
        if (!list) {
            return;
        }

        list.querySelectorAll(".lobby-item").forEach((item) => {
            const text = item.textContent.toLowerCase();
            item.style.display = text.includes(value) ? "flex" : "none";
        });
    });
};

renderLobbies();
wireLobbySearch();
