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
  const playersContainerEl = document.getElementById("playersContainer");
  const boardEl = document.getElementById("boardCards");
  const playerHandEl = document.getElementById("playerHoleCards");
  const actionModalEl = document.getElementById("actionModal");
  const raiseModalEl = document.getElementById("raiseModal");
  const actionPanelEl = document.getElementById("actionPanel");
  const leaveGameBtn = document.getElementById("leaveGameBtn");
  const raiseInput = document.getElementById("raiseAmount");
  const confirmRaiseBtn = document.getElementById("confirmRaise");
  const cancelRaiseBtn = document.getElementById("cancelRaise");

  let lastState = null;
  let currentAction = null;

  const protocol = window.location.protocol === "https:" ? "wss" : "ws";
  const socketUrl = `${protocol}://${window.location.host}/ws/game?lobbyId=${lobbyId}`;
  const socket = new WebSocket(socketUrl);

  // Color palette for player avatars
  const colorClasses = [
    "color-1", "color-2", "color-3", "color-4",
    "color-5", "color-6", "color-7", "color-8"
  ];

  // Map card codes to Romanian card filenames
  function getCardImagePath(card) {
    if (!card || !card.code) {
      return "/assets/back/cardCover.png";
    }

    const code = card.code;
    let rank = code.slice(0, -1); // All but last char
    const suit = code[code.length - 1]; // Last char

    // Map suit letters to Romanian suit names
    let suitName = "";
    switch (suit) {
      case "C": suitName = "trefla"; break;  // Clubs
      case "D": suitName = "romb"; break;    // Diamonds
      case "H": suitName = "rosu"; break;    // Hearts
      case "S": suitName = "negru"; break;   // Spades
      default: return "/assets/back/cardCover.png";
    }

    return `/assets/face/${suitName}${rank}.png`;
  }

  // Calculate player positions in a circle
  function calculatePlayerPositions(totalPlayers) {
    const positions = [];
    const radius = 150;
    const centerX = 250;
    const centerY = 190;
    const startAngle = -Math.PI / 2; // Start at top

    for (let i = 0; i < totalPlayers; i++) {
      const angle = startAngle + (i / totalPlayers) * 2 * Math.PI;
      const x = centerX + radius * Math.cos(angle);
      const y = centerY + radius * Math.sin(angle);
      positions.push({ x, y, angle });
    }

    return positions;
  }

  function setStatus(message, type) {
    if (!statusEl) {
      return;
    }

    statusEl.textContent = message;
    statusEl.style.display = "block";
    statusEl.style.borderColor = type === "error" ? "#ff6b6b" : "#f2d479";
    statusEl.style.background = type === "error" ? "rgba(255, 107, 107, 0.15)" : "rgba(242, 212, 121, 0.15)";
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
      console.error("Socket not connected.");
      return;
    }

    socket.send(JSON.stringify({ type, payload }));
  }

  function renderCardElement(card, isHoleCard = false) {
    const cardEl = document.createElement("div");
    cardEl.className = isHoleCard ? "your-card" : "card";

    const imagePath = getCardImagePath(card);
    const img = document.createElement("img");
    img.src = imagePath;
    img.alt = card ? card.code : "Card Back";
    img.style.width = "100%";
    img.style.height = "100%";
    img.style.borderRadius = "8px";
    img.style.objectFit = "cover";

    cardEl.appendChild(img);
    return cardEl;
  }

  function hideAllModals() {
    actionModalEl.style.display = "none";
    raiseModalEl.style.display = "none";
    if (actionPanelEl) {
      actionPanelEl.innerHTML = "";
    }
  }

  function showActionModal(player, state) {
    hideAllModals();

    const currentBet = state.currentBet || 0;
    const playerBet = player.currentBet || 0;
    const callAmount = currentBet - playerBet;
    const minRaise = state.minRaise || 0;

    if (actionPanelEl) {
      actionPanelEl.innerHTML = "";

      // Header
      const headerEl = document.createElement("div");
      headerEl.style.fontSize = "1rem";
      headerEl.style.fontWeight = "700";
      headerEl.style.color = "#f2d479";
      headerEl.style.marginBottom = "12px";
      headerEl.style.textAlign = "center";
      headerEl.textContent = `${player.username}'s Turn - What's your move?`;
      actionPanelEl.appendChild(headerEl);

      // Actions Container
      const actionsContainer = document.createElement("div");
      actionsContainer.style.display = "grid";
      actionsContainer.style.gridTemplateColumns = "1fr 1fr 1fr 1fr";
      actionsContainer.style.gap = "10px";

      // Check button
      if (callAmount === 0) {
        const checkBtn = document.createElement("button");
        checkBtn.className = "action-button";
        checkBtn.textContent = "Check";
        checkBtn.addEventListener("click", () => {
          sendMessage("action", { action: "check", amount: 0 });
          hideAllModals();
        });
        actionsContainer.appendChild(checkBtn);
      }

      // Call button
      if (callAmount > 0) {
        const callBtn = document.createElement("button");
        callBtn.className = "action-button";
        callBtn.textContent = `Call $${callAmount}`;
        callBtn.addEventListener("click", () => {
          sendMessage("action", { action: "call", amount: 0 });
          hideAllModals();
        });
        actionsContainer.appendChild(callBtn);
      }

      // Fold button
      const foldBtn = document.createElement("button");
      foldBtn.className = "action-button";
      foldBtn.textContent = "Fold";
      foldBtn.addEventListener("click", () => {
        sendMessage("action", { action: "fold", amount: 0 });
        hideAllModals();
      });
      actionsContainer.appendChild(foldBtn);

      // Raise button
      const raiseBtn = document.createElement("button");
      raiseBtn.className = "action-button primary";
      raiseBtn.textContent = "Raise";
      raiseBtn.addEventListener("click", () => {
        actionPanelEl.innerHTML = "";
        raiseModalEl.style.display = "block";
        raiseInput.focus();
      });
      actionsContainer.appendChild(raiseBtn);

      actionPanelEl.appendChild(actionsContainer);
    } else {
      // Fallback to modal if actionPanel doesn't exist
      const header = document.getElementById("modalHeader");
      const body = document.getElementById("modalActions");

      header.textContent = `${player.username}'s Turn`;
      body.innerHTML = "";

      // Check button
      if (callAmount === 0) {
        const checkBtn = document.createElement("button");
        checkBtn.className = "action-button";
        checkBtn.textContent = "Check";
        checkBtn.addEventListener("click", () => {
          sendMessage("action", { action: "check", amount: 0 });
          hideAllModals();
        });
        body.appendChild(checkBtn);
      }

      // Call button
      if (callAmount > 0) {
        const callBtn = document.createElement("button");
        callBtn.className = "action-button";
        callBtn.textContent = `Call $${callAmount}`;
        callBtn.addEventListener("click", () => {
          sendMessage("action", { action: "call", amount: 0 });
          hideAllModals();
        });
        body.appendChild(callBtn);
      }

      // Fold button
      const foldBtn = document.createElement("button");
      foldBtn.className = "action-button";
      foldBtn.textContent = "Fold";
      foldBtn.addEventListener("click", () => {
        sendMessage("action", { action: "fold", amount: 0 });
        hideAllModals();
      });
      body.appendChild(foldBtn);

      // Raise button
      const raiseBtn = document.createElement("button");
      raiseBtn.className = "action-button primary";
      raiseBtn.textContent = "Raise";
      raiseBtn.addEventListener("click", () => {
        actionModalEl.style.display = "none";
        raiseModalEl.style.display = "block";
        raiseInput.focus();
      });
      body.appendChild(raiseBtn);

      actionModalEl.style.display = "block";
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

  function renderState(state) {
    lastState = state;
    clearStatus();

    const phaseValue = typeof state.phase === "string" ? state.phase : "waiting";
    const phaseLower = phaseValue.toLowerCase();

    // Update header info
    if (phaseEl) {
      phaseEl.textContent = formatPhase(phaseValue);
    }

    if (potEl) {
      potEl.textContent = state.pot ?? 0;
    }

    // Render community cards
    if (boardEl) {
      boardEl.innerHTML = "";
      (state.board || []).forEach(card => {
        const cardEl = renderCardElement(card, false);
        boardEl.appendChild(cardEl);
      });

      // Fill empty slots for unrevealed cards
      const visibleCards = (state.board || []).length;
      const totalSlots = 5;
      for (let i = visibleCards; i < totalSlots; i++) {
        const cardEl = renderCardElement(null, false);
        boardEl.appendChild(cardEl);
      }
    }

    // Render player hand (hole cards)
    if (playerHandEl) {
      playerHandEl.innerHTML = "";
      const viewerPlayer = (state.players || []).find(p => p.userId === state.viewerUserId);
      if (viewerPlayer && viewerPlayer.holeCards && viewerPlayer.holeCards.length > 0) {
        viewerPlayer.holeCards.forEach(card => {
          const cardEl = renderCardElement(card, true);
          playerHandEl.appendChild(cardEl);
        });
      }
    }

    // Render players in circle
    if (playersContainerEl) {
      playersContainerEl.innerHTML = "";
      const players = state.players || [];
      const positions = calculatePlayerPositions(players.length);

      players.forEach((player, index) => {
        const position = positions[index];
        const seat = document.createElement("div");
        seat.className = "player-seat";
        seat.style.left = (position.x - 50) + "px";
        seat.style.top = (position.y - 50) + "px";

        const playerCard = document.createElement("div");
        playerCard.className = "player-card";

        // Avatar
        const avatar = document.createElement("div");
        avatar.className = "player-avatar";
        const colorClass = colorClasses[index % colorClasses.length];
        avatar.classList.add(colorClass);

        // Initials
        const initials = player.username
          .split(" ")
          .map(word => word[0])
          .join("")
          .substring(0, 2)
          .toUpperCase();
        avatar.textContent = initials;

        if (player.userId === state.currentTurnUserId) {
          avatar.classList.add("current-turn");
        }

        if (player.hasFolded) {
          avatar.classList.add("folded");
        }

        avatar.addEventListener("click", () => {
          if (player.userId === state.viewerUserId && player.userId === state.currentTurnUserId) {
            showActionModal(player, state);
          }
        });

        playerCard.appendChild(avatar);

        // Name
        const nameEl = document.createElement("div");
        nameEl.className = "player-name";
        nameEl.textContent = player.username;
        playerCard.appendChild(nameEl);

        // Chips
        const chipsEl = document.createElement("div");
        chipsEl.className = "player-chips";
        chipsEl.textContent = `$${player.chips}`;
        playerCard.appendChild(chipsEl);

        // Current bet indicator
        if (player.currentBet > 0) {
          const betEl = document.createElement("div");
          betEl.className = "player-bet";
          betEl.textContent = `$${player.currentBet}`;
          avatar.appendChild(betEl);
        }

        // Tags
        const tags = [];
        if (player.isDealer) tags.push("D");
        if (player.isSmallBlind) tags.push("SB");
        if (player.isBigBlind) tags.push("BB");
        if (player.isAllIn) tags.push("All-in");

        if (tags.length > 0) {
          const tagsEl = document.createElement("div");
          tagsEl.className = "player-tags";
          tags.forEach(tag => {
            const tagEl = document.createElement("span");
            tagEl.className = "tag";
            tagEl.textContent = tag;
            tagsEl.appendChild(tagEl);
          });
          playerCard.appendChild(tagsEl);
        }

        seat.appendChild(playerCard);
        playersContainerEl.appendChild(seat);
      });
    }

    // Show result if hand is complete
    if (state.lastResult && state.lastResult.winners?.length) {
      // Hand complete - silently proceed to next hand
    }

    // Auto-show action modal if it's the viewer's turn
    if (state.viewerUserId === state.currentTurnUserId && phaseLower !== "waitingforplayers" && phaseLower !== "showdown") {
      const viewerPlayer = (state.players || []).find(p => p.userId === state.viewerUserId);
      if (viewerPlayer) {
        showActionModal(viewerPlayer, state);
      }
    } else {
      hideAllModals();
    }

    updateControls(state, phaseLower);
  }

  function updateControls(state, phaseLower) {
    // No game start button anymore - game is automatic
  }

  socket.addEventListener("open", () => {
    // Silent connection
  });

  socket.addEventListener("close", () => {
    // Disconnected - will be handled by redirect
  });

  socket.addEventListener("error", () => {
    // Error silently handled
  });

  socket.addEventListener("message", event => {
    let message;
    try {
      message = JSON.parse(event.data);
    } catch (error) {
      console.error("Invalid server message.");
      return;
    }

    if (!message || !message.type) {
      return;
    }

    if (message.type === "state") {
      renderState(message.payload);
      return;
    }
  });

  // Leave Game Button
  if (leaveGameBtn) {
    leaveGameBtn.addEventListener("click", () => {
      if (confirm("Are you sure you want to leave the game?")) {
        // First, call the leave lobby endpoint to remove the player from the lobby
        fetch(`/lobbies/${lobbyId}/leave`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          }
        }).then(() => {
          // Close the WebSocket
          socket.close(1000, "User left game");
          // Redirect to lobby after a short delay to allow socket to close
          setTimeout(() => {
            window.location.href = "/Lobby/Index";
          }, 500);
        }).catch(error => {
          console.error("Error leaving lobby:", error);
          // Still redirect even if the API call fails
          socket.close(1000, "User left game");
          setTimeout(() => {
            window.location.href = "/Lobby/Index";
          }, 500);
        });
      }
    });
  }

  // Raise Modal Buttons
  if (confirmRaiseBtn) {
    confirmRaiseBtn.addEventListener("click", () => {
      const amount = Number(raiseInput?.value || 0);
      if (amount > 0) {
        sendMessage("action", { action: "raise", amount });
        hideAllModals();
      }
    });
  }

  if (cancelRaiseBtn) {
    cancelRaiseBtn.addEventListener("click", () => {
      raiseModalEl.style.display = "none";
      if (lastState) {
        const viewerPlayer = (lastState.players || []).find(p => p.userId === lastState.viewerUserId);
        if (viewerPlayer && viewerPlayer.userId === lastState.currentTurnUserId) {
          showActionModal(viewerPlayer, lastState);
        }
      }
    });
  }

  // Allow Enter key in raise input
  if (raiseInput) {
    raiseInput.addEventListener("keypress", (e) => {
      if (e.key === "Enter") {
        confirmRaiseBtn?.click();
      }
    });
  }
})();
