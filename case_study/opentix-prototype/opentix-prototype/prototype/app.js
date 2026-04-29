function highlightJson(json) {
  return json
    .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
    .replace(
      /("(\\u[\da-fA-F]{4}|\\[^u]|[^\\"])*"(?:\s*:)?|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?|true|false|null)/g,
      (match) => {
        let cls = "json-num";
        if (/^"/.test(match)) {
          cls = /:$/.test(match) ? "json-key" : "json-str";
        } else if (/true|false/.test(match)) {
          cls = "json-bool";
        }
        return `<span class="${cls}">${match}</span>`;
      }
    );
}

const state = {
  walletConnected: false,
  activeTab: "marketplace",
  activeEventId: "evt-jazz",
  activeTier: "Premium",
  template: "Monochrome Luxe",
  markup: 35,
  contentUnlock: true,
  royaltyEnabled: true,
  mintedCount: 2384,
  owner: "GBC4...9X2Q",
  ticketId: "OTX-001-PREM-8F21",
  ledger: [
    { time: "09:14", type: "event.created", actor: "Grafton Arts Collective", hash: "a7f2c1e90b" },
    { time: "09:22", type: "rules.published", actor: "OpenTix Rules Contract", hash: "c0419982ad" },
    { time: "10:03", type: "ticket.minted", actor: "Fan Wallet GBC4...9X2Q", hash: "f02a9d118e" },
  ],
};

const events = [
  {
    id: "evt-jazz",
    name: "Riverfront Jazz Night",
    organizer: "Grafton Arts Collective",
    date: "May 18, 2026",
    venue: "Riverfront Amphitheater",
    category: "Music",
    description: "A premium evening event with programmable tickets, collectible art, and VIP content unlocks.",
    supply: 1200,
    sold: 842,
    royalty: 7,
    tiers: [
      { name: "General", price: 42, available: 358, perk: "Mobile ticket + QR entry" },
      { name: "Premium", price: 89, available: 96, perk: "Collectible artwork + media unlock" },
      { name: "VIP", price: 175, available: 24, perk: "Backstage pass + limited NFT" },
    ],
  },
  {
    id: "evt-summit",
    name: "Stellar Builders Summit",
    organizer: "OpenTix Labs",
    date: "June 4, 2026",
    venue: "Innovation Hall",
    category: "Conference",
    description: "Conference ticketing with attendee credentials, workshop access, and sponsor content downloads.",
    supply: 600,
    sold: 418,
    royalty: 5,
    tiers: [
      { name: "General", price: 129, available: 182, perk: "Main stage access" },
      { name: "Premium", price: 249, available: 61, perk: "Workshop bundle + recordings" },
      { name: "VIP", price: 499, available: 12, perk: "Speaker dinner + private content" },
    ],
  },
  {
    id: "evt-season",
    name: "Championship Season Pass",
    organizer: "Metro City FC",
    date: "2026 Season",
    venue: "Metro Arena",
    category: "Sports",
    description: "A multi-event listing for teams that want one season catalog with game-level ticket inventory.",
    supply: 5000,
    sold: 3512,
    royalty: 10,
    tiers: [
      { name: "General", price: 360, available: 1488, perk: "Five-match season pass" },
      { name: "Premium", price: 720, available: 420, perk: "Premium seating + merch token" },
      { name: "VIP", price: 1400, available: 80, perk: "Club access + collectible series" },
    ],
  },
];

const seasonGames = ["Home Opener", "Rivalry Night", "Founders Match", "Family Weekend", "Finale"];

function activeEvent() {
  return events.find((event) => event.id === state.activeEventId) || events[0];
}

function activeTier() {
  const event = activeEvent();
  return event.tiers.find((tier) => tier.name === state.activeTier) || event.tiers[0];
}

function money(value) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD", maximumFractionDigits: 0 }).format(value);
}

function addLedger(type, actor) {
  const now = new Date();
  const hash = Math.random().toString(16).slice(2, 12).padEnd(10, "0");
  state.ledger.unshift({
    time: now.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }),
    type,
    actor,
    hash,
  });
}

function setTab(tabName) {
  state.activeTab = tabName;
  document.querySelectorAll(".tab").forEach((tab) => {
    tab.classList.toggle("active", tab.dataset.tab === tabName);
  });
  document.querySelectorAll(".tab-panel").forEach((panel) => {
    panel.classList.toggle("active", panel.id === tabName);
  });
}

function renderEventList() {
  const container = document.getElementById("eventList");
  container.innerHTML = events.map((event) => `
    <button class="event-card ${event.id === state.activeEventId ? "active" : ""}" data-event-id="${event.id}">
      <div class="event-card-top">
        <span class="badge">${event.category}</span>
        <span class="badge">${event.sold}/${event.supply}</span>
      </div>
      <h3>${event.name}</h3>
      <p>${event.organizer}</p>
      <div class="progress-bar"><div class="progress-fill" style="width:${Math.round((event.sold / event.supply) * 100)}%"></div></div>
    </button>
  `).join("");

  container.querySelectorAll("[data-event-id]").forEach((button) => {
    button.addEventListener("click", () => {
      state.activeEventId = button.dataset.eventId;
      state.activeTier = "Premium";
      renderAll();
    });
  });
}

function renderEventDetail() {
  const event = activeEvent();
  const tier = activeTier();
  const receipt = {
    event_id: event.id,
    ticket_id: state.ticketId,
    tier: tier.name,
    buyer_wallet: state.owner,
    price: tier.price,
    organizer_royalty: state.royaltyEnabled ? `${event.royalty}%` : "disabled",
    transfer_rule: `max_markup_${state.markup}_percent`,
  };

  document.getElementById("eventDetail").innerHTML = `
    <div class="event-hero-line">
      <div>
        <p class="eyebrow">${event.category}</p>
        <h2>${event.name}</h2>
        <p class="muted">${event.description}</p>
        <div class="event-meta">
          <span class="badge">${event.date}</span>
          <span class="badge">${event.venue}</span>
          <span class="badge">${event.royalty}% resale royalty</span>
        </div>
      </div>
      <div class="stat-card">
        <span>Supply sold</span>
        <strong>${Math.round((event.sold / event.supply) * 100)}%</strong>
        <small>${event.sold} of ${event.supply}</small>
      </div>
    </div>

    <h3>Choose ticket tier</h3>
    <div class="tier-grid">
      ${event.tiers.map((item) => `
        <button class="tier-card ${item.name === tier.name ? "active" : ""}" data-tier="${item.name}">
          <span class="badge">${item.name}</span>
          <strong>${money(item.price)}</strong>
          <small>${item.available} available</small>
          <p class="muted">${item.perk}</p>
        </button>
      `).join("")}
    </div>

    <div class="ticket-actions">
      <button class="button button-dark" id="mintTicket">Mint ticket</button>
      <button class="button button-light" data-tab-target="wallet">View wallet ticket</button>
    </div>

    <div class="receipt">
      <p class="eyebrow" style="color:#20C997;margin-bottom:12px;">Mock transaction payload</p>
      <code>${highlightJson(JSON.stringify(receipt, null, 2))}</code>
    </div>
  `;

  document.querySelectorAll("[data-tier]").forEach((button) => {
    button.addEventListener("click", () => {
      state.activeTier = button.dataset.tier;
      renderAll();
    });
  });

  document.getElementById("mintTicket").addEventListener("click", () => {
    state.walletConnected = true;
    state.mintedCount += 1;
    state.ticketId = `OTX-${event.id.slice(-3).toUpperCase()}-${tier.name.slice(0, 4).toUpperCase()}-${Math.random().toString(16).slice(2, 6).toUpperCase()}`;
    addLedger("ticket.minted", `Fan Wallet ${state.owner}`);
    setTab("wallet");
    renderAll();
  });

  wireTabTargetButtons();
}

function ticketPreviewHtml(size = "normal") {
  const event = activeEvent();
  const tier = activeTier();
  const templateClass = state.template === "Season Pass" ? "season" : state.template === "Premium Collectible" ? "collectible" : "";
  return `
    <div class="ticket-preview ${templateClass}">
      <div>
        <div class="ticket-top">
          <div>
            <div class="ticket-kicker">OpenTix verified ticket</div>
            <h3>${event.name}</h3>
            <p>${event.organizer}</p>
          </div>
          <div class="qr-box">▦</div>
        </div>
        <div class="ticket-facts">
          <div class="ticket-fact"><span>Date</span><strong>${event.date}</strong></div>
          <div class="ticket-fact"><span>Tier</span><strong>${tier.name}</strong></div>
          <div class="ticket-fact"><span>Owner</span><strong>${state.owner}</strong></div>
          <div class="ticket-fact"><span>Token</span><strong>${state.ticketId}</strong></div>
        </div>
      </div>
      <div class="ticket-bottom">
        <div>
          <div class="ticket-kicker">Template</div>
          <strong>${state.template}</strong>
        </div>
        <div>
          <div class="ticket-kicker">Royalty</div>
          <strong>${state.royaltyEnabled ? `${event.royalty}%` : "Off"}</strong>
        </div>
      </div>
    </div>
  `;
}

function renderOrganizer() {
  document.getElementById("ticketPreview").innerHTML = ticketPreviewHtml();
  document.getElementById("markupValue").textContent = state.markup;
  document.getElementById("markupRange").value = state.markup;
  document.getElementById("contentUnlock").checked = state.contentUnlock;
  document.getElementById("royaltyToggle").checked = state.royaltyEnabled;

  document.querySelectorAll("#templateSelector .segment").forEach((button) => {
    button.classList.toggle("active", button.dataset.template === state.template);
    button.onclick = () => {
      state.template = button.dataset.template;
      addLedger("template.updated", "Organizer Console");
      renderAll();
    };
  });

  document.getElementById("markupRange").oninput = (event) => {
    state.markup = Number(event.target.value);
    renderOrganizer();
    renderWallet();
  };
  document.getElementById("contentUnlock").onchange = (event) => {
    state.contentUnlock = event.target.checked;
    addLedger("content_rule.updated", "Organizer Console");
    renderAll();
  };
  document.getElementById("royaltyToggle").onchange = (event) => {
    state.royaltyEnabled = event.target.checked;
    addLedger("royalty_rule.updated", "Organizer Console");
    renderAll();
  };

  document.getElementById("seasonList").innerHTML = seasonGames.map((game, index) => `
    <div class="season-item">
      <span>${game}</span>
      <span class="badge">Game ${index + 1}</span>
    </div>
  `).join("");
}

function renderWallet() {
  const event = activeEvent();
  const tier = activeTier();
  const resalePrice = Math.round(tier.price * (1 + state.markup / 100));
  const royaltyAmount = state.royaltyEnabled ? Math.round(resalePrice * (event.royalty / 100)) : 0;
  document.getElementById("walletTicket").innerHTML = ticketPreviewHtml("large");
  document.getElementById("ownershipPanel").innerHTML = `
    <div class="policy-row"><span>Wallet status</span><strong>${state.walletConnected ? "Connected" : "Mock wallet disconnected"}</strong></div>
    <div class="policy-row"><span>Ticket owner</span><strong>${state.owner}</strong></div>
    <div class="policy-row"><span>Transfer allowed</span><strong>Yes, policy-controlled</strong></div>
    <div class="policy-row"><span>Original price</span><strong>${money(tier.price)}</strong></div>
    <div class="policy-row"><span>Max resale price</span><strong>${money(resalePrice)}</strong></div>
    <div class="policy-row"><span>Organizer royalty</span><strong>${money(royaltyAmount)}</strong></div>
    <div class="policy-row"><span>Premium content</span><strong>${state.contentUnlock ? "Unlocked for owner" : "Disabled"}</strong></div>
    <div class="ticket-actions" style="margin-top:18px">
      <button class="button button-dark" id="simulateResale">Simulate resale</button>
      <button class="button button-danger" id="freezeTicket">Flag suspicious transfer</button>
    </div>
  `;
  document.getElementById("simulateResale").onclick = () => {
    state.owner = "GDL9...2H7M";
    addLedger("ticket.resold", `Royalty ${money(royaltyAmount)} to ${event.organizer}`);
    renderAll();
  };
  document.getElementById("freezeTicket").onclick = () => {
    addLedger("risk.flagged", "Venue Verification Console");
    renderAll();
  };
}

function renderLedger() {
  document.getElementById("ledgerLog").innerHTML = state.ledger.map((item) => `
    <div class="ledger-item">
      <strong>${item.time}</strong>
      <div>
        <span class="badge">${item.type}</span>
        <p class="muted" style="margin:8px 0 0">${item.actor}</p>
      </div>
      <code>${item.hash}</code>
    </div>
  `).join("");
}

function renderMetrics() {
  document.getElementById("ticketsMintedMetric").textContent = state.mintedCount.toLocaleString();
  document.getElementById("connectWallet").textContent = state.walletConnected ? `Wallet ${state.owner}` : "Connect Mock Wallet";
}

function renderAll() {
  renderMetrics();
  renderEventList();
  renderEventDetail();
  renderOrganizer();
  renderWallet();
  renderLedger();
}

function wireTabTargetButtons() {
  document.querySelectorAll("[data-tab-target]").forEach((button) => {
    button.onclick = () => setTab(button.dataset.tabTarget);
  });
}

document.querySelectorAll(".tab").forEach((button) => {
  button.addEventListener("click", () => setTab(button.dataset.tab));
});

document.getElementById("connectWallet").addEventListener("click", () => {
  state.walletConnected = !state.walletConnected;
  addLedger(state.walletConnected ? "wallet.connected" : "wallet.disconnected", state.owner);
  renderAll();
});

wireTabTargetButtons();
renderAll();
