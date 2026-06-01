(() => {
  const container = document.querySelector(".game-container");
  if (!container) {
    return;
  }

  const lobbyId = container.dataset.lobbyId;
  if (!lobbyId) {
    return;
  }

  const isLeader = container.dataset.isLeader === "true";
  const statusEl = document.getElementById("gameStatus");
  const phaseEl = document.getElementById("phaseValue");
  const potEl = document.getElementById("potValue");
  const turnEl = document.getElementById("turnValue");
  const boardEl = document.getElementById("boardCards");
  const playersEl = document.getElementById("playersList");
  const startBtn = document.getElementById("startGameBtn");
  const raiseInput = document.getElementById("raiseAmount");
  const actionButtons = document.querySelectorAll(".action-btn[data-action]");

  let lastState = null;

  const protocol = window.location.protocol === "https:" ? "wss" : "ws";
  const socketUrl = `${protocol}://${window.location.host}/ws/game?lobbyId=${lobbyId}`;
  const socket = new WebSocket(socketUrl);

  function setStatus(message, type) {
    if (!statusEl) {
      return;
    }

    statusEl.textContent = message;
    statusEl.style.display = "block";
    statusEl.style.borderColor = type === "error" ? "#ff6b6b" : "#b8daff";
    statusEl.style.background = type === "error" ? "rgba(255, 107, 107, 0.15)" : "rgba(23, 162, 184, 0.15)";
  }

  function clearStatus() {
    if (!statusEl) {
      return;
    }

    statusEl.textContent = "";
    statusEl.style.display = "none";
  }

  function sendMessage(type, payload) {
    if (socket.readyState !== WebSocket.OPEN) {
      setStatus("Socket not connected.", "error");
      return;
    }

    socket.send(JSON.stringify({ type, payload }));
  }

  function getCardImageUrl(card) {
    if (!card) return "/images/cards/back/cardCover.png";
    const suitMap = {
      "Spades": "negru",
      "Hearts": "rosu",
      "Diamonds": "romb",
      "Clubs": "trefla",
      "spades": "negru",
      "hearts": "rosu",
      "diamonds": "romb",
      "clubs": "trefla"
    };
    const prefix = suitMap[card.suit] || "negru";
    return `/images/cards/face/${prefix}${card.rank}.png`;
  }

  function renderState(state) {
    lastState = state;
    clearStatus();

    const phaseValue = typeof state.phase === "string" ? state.phase : "waiting";
    const phaseLower = phaseValue.toLowerCase();

    if (phaseEl) {
      phaseEl.textContent = formatPhase(phaseValue);
    }

    if (potEl) {
      potEl.textContent = state.pot ?? 0;
    }

    if (turnEl) {
      const turnPlayer = state.players.find(player => player.userId === state.currentTurnUserId);
      turnEl.textContent = turnPlayer ? turnPlayer.username : "-";
    }

    if (boardEl) {
      boardEl.innerHTML = "";
      (state.board || []).forEach(card => {
        const img = document.createElement("img");
        img.className = "card-img";
        img.src = getCardImageUrl(card);
        img.alt = card.code || "";
        boardEl.appendChild(img);
      });
    }

    if (playersEl) {
      playersEl.innerHTML = "";
      (state.players || []).forEach(player => {
        const row = document.createElement("div");
        row.className = "player-row";
        if (player.userId === state.currentTurnUserId) {
          row.classList.add("turn");
        }
        if (player.hasFolded) {
          row.classList.add("folded");
        }

        const nameCell = document.createElement("div");
        nameCell.textContent = player.username;

        const chipsCell = document.createElement("div");
        chipsCell.textContent = `$${player.chips}`;

        const betCell = document.createElement("div");
        betCell.textContent = `Bet $${player.currentBet}`;

        const metaCell = document.createElement("div");
        const tags = [];
        if (player.isDealer) tags.push("Dealer");
        if (player.isSmallBlind) tags.push("SB");
        if (player.isBigBlind) tags.push("BB");
        if (player.isAllIn) tags.push("All-in");
        if (player.hasFolded) tags.push("Folded");
        if (player.isConnected && player.ipAddress) tags.push(`IP ${player.ipAddress}`);
        metaCell.className = "player-tags";
        metaCell.textContent = tags.join(" ");

        const holeCell = document.createElement("div");
        holeCell.className = "player-cards-cell";
        
        if (player.holeCards && player.holeCards.length > 0) {
          player.holeCards.forEach(card => {
            const img = document.createElement("img");
            img.className = "card-img-small";
            img.src = getCardImageUrl(card);
            holeCell.appendChild(img);
          });
        } else {
          if (!player.hasFolded && state.phase.toLowerCase() !== "waitingforplayers") {
            for (let i = 0; i < 2; i++) {
              const img = document.createElement("img");
              img.className = "card-img-small";
              img.src = getCardImageUrl(null);
              holeCell.appendChild(img);
            }
          }
        }

        row.appendChild(nameCell);
        row.appendChild(chipsCell);
        row.appendChild(betCell);
        row.appendChild(holeCell);
        row.appendChild(metaCell);
        playersEl.appendChild(row);
      });
    }

    if (state.lastResult && state.lastResult.winners?.length) {
      const winnerNames = state.lastResult.winners.map(winner => winner.username).join(", ");
      const handText = state.lastResult.handDescription ? ` - ${state.lastResult.handDescription}` : "";
      setStatus(`Hand complete. Winner(s): ${winnerNames}${handText}.`, "info");
    }

    updateControls(state, phaseLower);
  }

  function updateControls(state, phaseLower) {
    const isYourTurn = state.viewerUserId === state.currentTurnUserId;
    actionButtons.forEach(button => {
      button.disabled = !isYourTurn || phaseLower === "waitingforplayers" || phaseLower === "showdown";
    });

    if (startBtn) {
      startBtn.disabled = !isLeader || (phaseLower !== "waitingforplayers" && phaseLower !== "showdown");
    }
  }

  function formatPhase(phaseValue) {
    if (!phaseValue) {
      return "Waiting";
    }

    return phaseValue
      .replace(/([a-z])([A-Z])/g, "$1 $2")
      .replace(/^./, match => match.toUpperCase());
  }

  socket.addEventListener("open", () => {
    setStatus("Connected to table.", "info");
  });

  socket.addEventListener("close", () => {
    setStatus("Disconnected from table.", "error");
  });

  socket.addEventListener("error", () => {
    setStatus("WebSocket error.", "error");
  });

  socket.addEventListener("message", event => {
    let message;
    try {
      message = JSON.parse(event.data);
    } catch (error) {
      setStatus("Invalid server message.", "error");
      return;
    }

    if (!message || !message.type) {
      return;
    }

    if (message.type === "state") {
      renderState(message.payload);
      return;
    }

    if (message.type === "error") {
      setStatus(message.payload?.message || "Server error.", "error");
    }
  });

  if (startBtn) {
    startBtn.addEventListener("click", () => {
      sendMessage("start_game", {});
    });
  }

  actionButtons.forEach(button => {
    button.addEventListener("click", () => {
      const action = button.dataset.action;
      const amount = action === "raise" ? Number(raiseInput?.value || 0) : 0;
      sendMessage("action", { action, amount });
    });
  });
})();
