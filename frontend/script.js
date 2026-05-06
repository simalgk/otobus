const API_BASE = "http://localhost:5112/api";
let tripId = Number(localStorage.getItem("tripId")) || null;
let staffId = Number(localStorage.getItem("staffId")) || null;
let passengers = [];
let toastTimer = null;

async function api(path, options = {}) {
  const response = await fetch(`${API_BASE}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(options.headers || {})
    },
    ...options
  });

  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || `API hatası: ${response.status}`);
  }

  if (response.status === 204) return null;
  return response.json();
}

async function init() {
  setLoading(true);

  try {
    await ensureDemoTrip();
    await loadPassengers();
    await loadScanLogs();
    showToast("Backend bağlantısı kuruldu", "success");
  } catch (error) {
    passengers = [];
    showToast(`Backend bağlantısı kurulamadı: ${error.message}`, "warn");
  } finally {
    setLoading(false);
    render();
  }
}

async function ensureDemoTrip() {
  const trips = await api("/trips");
  const activeTrip = trips.find(trip => trip.durum !== "Completed" && trip.durum !== "Cancelled") || trips[0];

  if (activeTrip) {
    tripId = activeTrip.id;
    localStorage.setItem("tripId", tripId);
  } else {
    const seeded = await api("/demo/seed", { method: "POST", body: "{}" });
    tripId = seeded.tripId;
    staffId = seeded.staffId;
    localStorage.setItem("tripId", tripId);
    localStorage.setItem("staffId", staffId);
  }

  if (!staffId) {
    const users = await api("/users");
    const staff = users.find(user => user.role === "Staff" || user.role === "Admin");
    if (!staff) {
      const seeded = await api("/demo/seed", { method: "POST", body: "{}" });
      staffId = seeded.staffId;
    } else {
      staffId = staff.id;
    }
    localStorage.setItem("staffId", staffId);
  }
}

async function loadPassengers() {
  const tickets = await api(`/trippassengers?tripId=${tripId}`);

  if (tickets.length === 0) {
    const seeded = await api("/demo/seed", { method: "POST", body: "{}" });
    tripId = seeded.tripId;
    staffId = seeded.staffId;
    localStorage.setItem("tripId", tripId);
    localStorage.setItem("staffId", staffId);
    return loadPassengers();
  }

  passengers = tickets.map(ticket => ({
    id: ticket.passengerId,
    ticketId: ticket.id,
    qrCodeValue: ticket.qrCodeValue,
    seat: ticket.koltukNo,
    name: ticket.passengerName,
    status: "bekliyor",
    seatSensor: false,
    beltSensor: false,
    lastEvent: null,
    boardedAt: null
  }));
}

async function loadScanLogs() {
  const logs = await api(`/scanlogs?tripId=${tripId}`);
  const latestByPassenger = new Map();

  logs.forEach(log => {
    if (!latestByPassenger.has(log.passengerId)) {
      latestByPassenger.set(log.passengerId, log);
    }
  });

  passengers.forEach(passenger => {
    const log = latestByPassenger.get(passenger.id);
    if (!log) return;

    passenger.lastEvent = new Date(log.scanTime);
    if (log.scanType === "IN") {
      passenger.status = "bindi";
      passenger.boardedAt = new Date(log.scanTime);
    } else if (log.scanType === "OUT") {
      passenger.status = "indi";
      passenger.boardedAt = null;
      passenger.seatSensor = false;
      passenger.beltSensor = false;
    }
  });
}

function setLoading(isLoading) {
  const qrContainer = document.getElementById("qrButtons");
  const sensorContainer = document.getElementById("sensorButtons");
  const table = document.getElementById("passengerTable");

  if (isLoading) {
    qrContainer.innerHTML = `<button type="button" class="scan-btn" disabled>Backend'e bağlanıyor...</button>`;
    sensorContainer.innerHTML = "";
    table.innerHTML = "";
  }
}

function showToast(msg, type = "info") {
  const el = document.getElementById("toast");
  el.textContent = msg;
  el.className = "toast show toast-" + type;
  if (toastTimer) clearTimeout(toastTimer);
  toastTimer = setTimeout(() => el.classList.remove("show"), 3500);
}

async function doorQR(id) {
  const passenger = passengers.find(item => item.id === id);
  if (!passenger) return;

  const isBoarding = passenger.status === "bekliyor" || passenger.status === "indi";
  const previous = { ...passenger };

  applyDoorState(passenger, isBoarding);
  render();

  try {
    await api("/scanlogs/qr", {
      method: "POST",
      body: JSON.stringify({
        tripId,
        qrCodeValue: passenger.qrCodeValue,
        scannedByStaffId: staffId,
        scanType: isBoarding ? "IN" : "OUT",
        locationType: isBoarding ? "OTOGAR" : "MOLA"
      })
    });

    showToast(
      isBoarding
        ? `${passenger.name} otobüse bindi, kayıt backend'e işlendi`
        : `${passenger.name} otobüsten indi, kayıt backend'e işlendi`,
      isBoarding ? "info" : "warn"
    );
  } catch (error) {
    Object.assign(passenger, previous);
    showToast(`QR kaydı gönderilemedi: ${error.message}`, "warn");
    render();
  }
}

function applyDoorState(passenger, isBoarding) {
  if (isBoarding) {
    passenger.status = "bindi";
    passenger.boardedAt = new Date();
    passenger.lastEvent = new Date();
    return;
  }

  passenger.status = "indi";
  passenger.seatSensor = false;
  passenger.beltSensor = false;
  passenger.boardedAt = null;
  passenger.lastEvent = new Date();
}

function seatSensor(id) {
  const passenger = passengers.find(item => item.id === id);
  if (!passenger) return;

  if (passenger.status !== "bindi" && passenger.status !== "yerleşti") {
    showToast(`${passenger.name} henüz otobüse binmedi`, "warn");
    return;
  }

  passenger.seatSensor = !passenger.seatSensor;
  if (!passenger.seatSensor) passenger.beltSensor = false;

  trySettle(passenger);
  passenger.lastEvent = new Date();
  render();
}

function beltSensor(id) {
  const passenger = passengers.find(item => item.id === id);
  if (!passenger) return;

  if (passenger.status !== "bindi" && passenger.status !== "yerleşti") {
    showToast(`${passenger.name} henüz otobüse binmedi`, "warn");
    return;
  }

  if (!passenger.seatSensor) {
    showToast(`${passenger.name} önce koltuğa oturmalı`, "warn");
    return;
  }

  passenger.beltSensor = !passenger.beltSensor;
  trySettle(passenger);
  passenger.lastEvent = new Date();
  render();
}

function trySettle(passenger) {
  if (passenger.seatSensor && passenger.beltSensor) {
    passenger.status = "yerleşti";
    showToast(`${passenger.name} koltuğuna yerleşti, kemeri tamam`, "success");
  } else if (passenger.status === "yerleşti") {
    passenger.status = "bindi";
    showToast(
      !passenger.seatSensor
        ? `${passenger.name} koltuğundan kalktı`
        : `${passenger.name} kemerini çözdü`,
      "warn"
    );
  }
}

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

  if (passengers.length === 0) {
    container.innerHTML = `<button type="button" class="scan-btn" disabled>Yolcu bulunamadı</button>`;
    return;
  }

  passengers.forEach(passenger => {
    const action = passenger.status === "bekliyor" || passenger.status === "indi" ? "Bindir" : "İndir";
    const cls = action === "Bindir" ? "btn-board" : "btn-exit";

    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = `scan-btn ${cls}`;
    btn.onclick = () => doorQR(passenger.id);
    btn.innerHTML = `
      <span class="btn-seat">${passenger.seat}</span>
      <span class="btn-name">${passenger.name}</span>
      <span class="btn-action">${action} ▣</span>
    `;
    container.appendChild(btn);
  });
}

function renderSensorButtons() {
  const container = document.getElementById("sensorButtons");
  container.innerHTML = "";

  passengers.forEach(passenger => {
    const active = passenger.status === "bindi" || passenger.status === "yerleşti";
    const card = document.createElement("div");
    card.className = `sensor-card ${active ? "" : "sensor-inactive"}`;
    card.innerHTML = `
      <div class="sensor-name">
        <span class="btn-seat">${passenger.seat}</span>
        <span>${passenger.name}</span>
      </div>
      <div class="sensor-row">
        <button type="button"
          class="sensor-btn ${passenger.seatSensor ? "sensor-on" : "sensor-off"}"
          onclick="seatSensor(${passenger.id})" ${active ? "" : "disabled"}>
          💺 Koltuk<br><small>${passenger.seatSensor ? "DOLU" : "BOŞ"}</small>
        </button>
        <button type="button"
          class="sensor-btn ${passenger.beltSensor ? "sensor-on" : "sensor-off"}"
          onclick="beltSensor(${passenger.id})" ${active && passenger.seatSensor ? "" : "disabled"}>
          🔒 Kemer<br><small>${passenger.beltSensor ? "TAKILI" : "ÇÖZÜK"}</small>
        </button>
      </div>
    `;
    container.appendChild(card);
  });
}

function renderTable() {
  const tbody = document.getElementById("passengerTable");
  tbody.innerHTML = "";

  passengers.forEach(passenger => {
    const { label, cls } = statusInfo(passenger.status);
    const beltCell = passenger.status === "bindi" || passenger.status === "yerleşti"
      ? (passenger.beltSensor
          ? `<span class="badge badge-settled">✓ TAKILI</span>`
          : `<span class="badge badge-warn">✕ ÇÖZÜK</span>`)
      : `<span style="color:var(--text-dim)">-</span>`;

    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${passenger.seat}</td>
      <td>${passenger.name}</td>
      <td><span class="badge ${cls}">${label}</span></td>
      <td>${beltCell}</td>
      <td>${formatTime(passenger.lastEvent)}</td>
    `;
    tbody.appendChild(tr);
  });
}

function renderAlerts() {
  const list = document.getElementById("alertList");
  const alerts = [];

  passengers.forEach(passenger => {
    if (passenger.status === "bindi") {
      const elapsed = passenger.boardedAt ? Math.floor((Date.now() - passenger.boardedAt) / 1000) : 0;
      alerts.push({
        type: "danger",
        msg: `<strong>${passenger.name}</strong> (${passenger.seat}) - Otobüse bindi ama koltuğuna oturmadı${elapsed > 0 ? ` (${elapsed} sn önce)` : ""}`
      });
    }

    if (passenger.status === "yerleşti" && !passenger.beltSensor) {
      alerts.push({
        type: "warn",
        msg: `<strong>${passenger.name}</strong> (${passenger.seat}) - Koltuğunda ama kemeri takılı değil`
      });
    }
  });

  if (alerts.length === 0) {
    list.innerHTML = `<p class="empty-msg">Şu an uyarı yok ✓</p>`;
  } else {
    list.innerHTML = alerts.map(alert =>
      `<div class="alert-item alert-${alert.type}">${alert.msg}</div>`
    ).join("");
  }
}

function updateCounts() {
  setCount("totalCount", passengers.length);
  setCount("settledCount", passengers.filter(passenger => passenger.status === "yerleşti").length);
  setCount("boardedCount", passengers.filter(passenger => passenger.status === "bindi").length);
  setCount("outsideCount", passengers.filter(passenger => passenger.status === "indi").length);
  setCount("waitingCount", passengers.filter(passenger => passenger.status === "bekliyor").length);
}

function setCount(id, target) {
  document.getElementById(id).textContent = target;
}

function statusInfo(status) {
  return {
    bekliyor: { label: "BİNMEDİ", cls: "badge-waiting" },
    bindi: { label: "BİNDİ/AYAKTA", cls: "badge-boarded" },
    yerleşti: { label: "YERLEŞTİ", cls: "badge-settled" },
    indi: { label: "DIŞARIDA", cls: "badge-outside" }
  }[status] || { label: status, cls: "" };
}

function formatTime(date) {
  if (!date) return "-";
  return date.toLocaleTimeString("tr-TR", { hour: "2-digit", minute: "2-digit", second: "2-digit" });
}

setInterval(() => {
  const hasBoardedOnly = passengers.some(passenger => passenger.status === "bindi");
  if (hasBoardedOnly) renderAlerts();
}, 5000);

setInterval(async () => {
  if (!tripId || passengers.length === 0) return;

  try {
    await loadScanLogs();
    render();
  } catch {
    // Sessiz geçiyoruz; anlık kopmada ekranı bozmasın.
  }
}, 15000);

init();
