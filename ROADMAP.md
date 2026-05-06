# 🚌 QR Otobüs Takip Sistemi — ROADMAP

> Hackathon projesi. Çok hatlı, gerçek zamanlı yolcu takip sistemi.

---

## ✅ Tamamlananlar

- Muavin web paneli (anlık yolcu durumu)
- 4 aşamalı durum akışı: `Binmedi → Bindi → Yerleşti → Dışarıda`
- Koltuk & kemer sensör simülasyonu
- Kapı QR okutma simülasyonu
- Muavin uyarı ekranı (kemersiz / ayakta kalan yolcu)

---

## 🔨 Yapılacaklar (Hackathon Kapsamı)

- [ ] Backend API (yolcu, sefer, olay endpoint'leri)
- [ ] Veritabanı bağlantısı
- [ ] Frontend → Backend entegrasyonu
- [ ] Gerçek QR kamera okuyucu bağlantısı

---

## 🔮 Hackathon Sonrası Vizyon

Sistemin gerçek dünyada nasıl büyüyeceği:

```
Şu an (Demo)          →       Gerçek Sistem
─────────────────────────────────────────────
Simüle sensörler      →   Fiziksel koltuk & kemer sensörleri
Tek hat               →   Çok hat, çok şehir
Manuel QR butonu      →   Kapıda otomatik kamera okuyucu
Statik veri           →   Canlı WebSocket + GPS takip
```

---

## 🗃️ Veri Modeli

```
trips       → id, route, bus, departure_time
passengers  → id, name, seat_no, trip_id, ticket_qr
events      → id, passenger_id, type, timestamp
              (type: boarded | seated | belted | exited)
```

---

> 💡 Sensör simülasyonu demo için yeterli — jüriye akışı göstermek için tasarlandı.