/**
 * Durum akışı:
 *  bekliyor  ──[Kapı QR]──▶  bindi       (otobüse girdi ama oturmadı)
 *  bindi     ──[Koltuk+Kemer]──▶  yerleşti  (koltuğa oturdu, kemeri taktı)
 *  yerleşti  ──[Kapı QR]──▶  indi        (otobüsten indi / mola)
 *  indi      ──[Kapı QR]──▶  bindi       (tekrar bindi)
 *
 * Muavin uyarıları:
 *  - "bindi" durumunda 30sn geçerse → "Koltuğuna oturmadı!"
 *  - "yerleşti" ama kemer takılı değilse → "Kemeri yok!"
 */

const passengers = [
  { id: 1, seat: "12A", name: "Ayşe Yılmaz",  status: "bekliyor", seatSensor: false, beltSensor: false, lastEvent: null, boardedAt: null },
  { id: 2, seat: "12B", name: "Mehmet Kaya",   status: "bekliyor", seatSensor: false, beltSensor: false, lastEvent: null, boardedAt: null },
  { id: 3, seat: "13A", name: "Elif Demir",    status: "bekliyor", seatSensor: false, beltSensor: false, lastEvent: null, boardedAt: null },
  { id: 4, seat: "13B", name: "Ahmet Çelik",   status: "bekliyor", seatSensor: false, beltSensor: false, lastEvent: null, boardedAt: null },
];

// ── Toast ──────────────────────────────────────────
let toastTimer = null;
function showToast(msg, type = "info") {
  const el = document.getElementById("toast");
  el.textContent = msg;
  el.className = "toast show toast-" + type;
  if (toastTimer) clearTimeout(toastTimer);
  toastTimer = setTimeout(() => el.classList.remove("show"), 3500);
}

// ── Kapı QR okutma ────────────────────────────────
function doorQR(id) {
  const p = passengers.find(x => x.id === id);
  if (!p) return;

  if (p.status === "bekliyor" || p.status === "indi") {
    p.status = "bindi";
    p.boardedAt = new Date();
    p.lastEvent = new Date();
    showToast(`🚪 ${p.name} otobüse bindi — koltuk bekleniyor`, "info");
  } else if (p.status === "yerleşti" || p.status === "bindi") {
    // İniş
    p.status = "indi";
    p.seatSensor = false;
    p.beltSensor = false;
    p.boardedAt = null;
    p.lastEvent = new Date();
    showToast(`🚪 ${p.name} otobüsten indi`, "warn");
  }

  render();
}

// ── Koltuk sensörü ────────────────────────────────
function seatSensor(id) {
  const p = passengers.find(x => x.id === id);
  if (!p) return;
  if (p.status !== "bindi" && p.status !== "yerleşti") {
    showToast(`⚠ ${p.name} henüz otobüse binmedi!`, "warn");
    return;
  }

  p.seatSensor = !p.seatSensor;
  if (!p.seatSensor) p.beltSensor = false; // Kalktıysa kemer de çözülür

  trySettle(p);
  p.lastEvent = new Date();
  render();
}

// ── Kemer sensörü ─────────────────────────────────
function beltSensor(id) {
  const p = passengers.find(x => x.id === id);
  if (!p) return;
  if (p.status !== "bindi" && p.status !== "yerleşti") {
    showToast(`⚠ ${p.name} henüz otobüse binmedi!`, "warn");
    return;
  }
  if (!p.seatSensor) {
    showToast(`⚠ ${p.name} önce koltuğa oturmalı!`, "warn");
    return;
  }

  p.beltSensor = !p.beltSensor;
  trySettle(p);
  p.lastEvent = new Date();
  render();
}

function trySettle(p) {
  if (p.seatSensor && p.beltSensor) {
    p.status = "yerleşti";
    showToast(`✅ ${p.name} koltuğuna yerleşti, kemeri tamam`, "success");
  } else if (p.status === "yerleşti") {
    p.status = "bindi";
    if (!p.seatSensor) showToast(`⚠ ${p.name} koltuğundan kalktı!`, "warn");
    else showToast(`⚠ ${p.name} kemerini çözdü!`, "warn");
  }
}

// ── Render ────────────────────────────────────────
function render() {
  renderQRButtons();
  renderSensorButtons();
  renderTable();
  renderAlerts();
  updateCounts();
}

function renderQRButtons() {
  const container = document.getElementById("qrButtons");
  container.innerHTML = "";
  passengers.forEach(p => {
    const canScan = ["bekliyor", "indi", "yerleşti", "bindi"].includes(p.status);
    const action  = (p.status === "bekliyor" || p.status === "indi") ? "Bindir" : "İndir";
    const cls     = action === "Bindir" ? "btn-board" : "btn-exit";

    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = `scan-btn ${cls}`;
    btn.onclick = () => doorQR(p.id);
    btn.innerHTML = `
      <span class="btn-seat">${p.seat}</span>
      <span class="btn-name">${p.name}</span>
      <span class="btn-action">${action} ▣</span>
    `;
    container.appendChild(btn);
  });
}

function renderSensorButtons() {
  const container = document.getElementById("sensorButtons");
  container.innerHTML = "";
  passengers.forEach(p => {
    const active = p.status === "bindi" || p.status === "yerleşti";
    const card = document.createElement("div");
    card.className = `sensor-card ${active ? "" : "sensor-inactive"}`;
    card.innerHTML = `
      <div class="sensor-name">
        <span class="btn-seat">${p.seat}</span>
        <span>${p.name}</span>
      </div>
      <div class="sensor-row">
        <button type="button"
          class="sensor-btn ${p.seatSensor ? "sensor-on" : "sensor-off"}"
          onclick="seatSensor(${p.id})" ${active ? "" : "disabled"}>
          💺 Koltuk<br><small>${p.seatSensor ? "DOLU" : "BOŞ"}</small>
        </button>
        <button type="button"
          class="sensor-btn ${p.beltSensor ? "sensor-on" : "sensor-off"}"
          onclick="beltSensor(${p.id})" ${active && p.seatSensor ? "" : "disabled"}>
          🔒 Kemer<br><small>${p.beltSensor ? "TAKILI" : "ÇÖZÜK"}</small>
        </button>
      </div>
    `;
    container.appendChild(card);
  });
}

function renderTable() {
  const tbody = document.getElementById("passengerTable");
  tbody.innerHTML = "";
  passengers.forEach(p => {
    const { label, cls } = statusInfo(p.status);
    const beltCell = p.status === "bindi" || p.status === "yerleşti"
      ? (p.beltSensor
          ? `<span class="badge badge-settled">✓ TAKILI</span>`
          : `<span class="badge badge-warn">✗ ÇÖZÜK</span>`)
      : `<span style="color:var(--text-dim)">—</span>`;
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${p.seat}</td>
      <td>${p.name}</td>
      <td><span class="badge ${cls}">${label}</span></td>
      <td>${beltCell}</td>
      <td>${formatTime(p.lastEvent)}</td>
    `;
    tbody.appendChild(tr);
  });
}

function renderAlerts() {
  const list = document.getElementById("alertList");
  const alerts = [];

  passengers.forEach(p => {
    // Bindi ama koltukta değil
    if (p.status === "bindi") {
      const elapsed = p.boardedAt ? Math.floor((Date.now() - p.boardedAt) / 1000) : 0;
      alerts.push({
        type: "danger",
        msg: `<strong>${p.name}</strong> (${p.seat}) — Otobüse bindi ama koltuğuna oturmadı${elapsed > 0 ? ` (${elapsed}sn önce)` : ""}`
      });
    }
    // Yerleşti ama kemer yok
    if (p.status === "yerleşti" && !p.beltSensor) {
      alerts.push({
        type: "warn",
        msg: `<strong>${p.name}</strong> (${p.seat}) — Koltuğunda ama kemeri takılı değil!`
      });
    }
  });

  if (alerts.length === 0) {
    list.innerHTML = `<p class="empty-msg">Şu an uyarı yok ✓</p>`;
  } else {
    list.innerHTML = alerts.map(a =>
      `<div class="alert-item alert-${a.type}">${a.msg}</div>`
    ).join("");
  }
}

function updateCounts() {
  const settled = passengers.filter(p => p.status === "yerleşti").length;
  const boarded = passengers.filter(p => p.status === "bindi").length;
  const outside = passengers.filter(p => p.status === "indi").length;
  const waiting = passengers.filter(p => p.status === "bekliyor").length;

  animateCount("totalCount",   passengers.length);
  animateCount("settledCount", settled);
  animateCount("boardedCount", boarded);
  animateCount("outsideCount", outside);
  animateCount("waitingCount", waiting);
}

function statusInfo(status) {
  return {
    "bekliyor":  { label: "BİNMEDİ",   cls: "badge-waiting" },
    "bindi":     { label: "BİNDİ/AYAKTA", cls: "badge-boarded" },
    "yerleşti":  { label: "YERLEŞTİ",  cls: "badge-settled" },
    "indi":      { label: "DIŞARIDA",   cls: "badge-outside" },
  }[status] || { label: status, cls: "" };
}

function formatTime(date) {
  if (!date) return "—";
  return date.toLocaleTimeString("tr-TR", { hour: "2-digit", minute: "2-digit", second: "2-digit" });
}

function animateCount(id, target) {
  const el = document.getElementById(id);
  const current = parseInt(el.textContent) || 0;
  if (current === target) return;
  const step = target > current ? 1 : -1;
  let val = current;
  const iv = setInterval(() => {
    val += step;
    el.textContent = val;
    if (val === target) clearInterval(iv);
  }, 60);
}

// Periyodik uyarı güncellemesi (boardedAt süresini canlı tutar)
setInterval(() => {
  const hasBoardedOnly = passengers.some(p => p.status === "bindi");
  if (hasBoardedOnly) renderAlerts();
}, 5000);

render();