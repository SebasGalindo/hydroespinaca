# HydroEspinaca

Distributed architecture project for intelligent control of hydroponic cultivation using microservices, fuzzy control, and hardware communication.

This project is a graduation thesis developed by students of **Universidad de Cundinamarca** as part of their final academic work. It is divided into two independent but interconnected subprojects:

- **Project 1 – Hardware Team:** Responsible for data acquisition from sensors and actuator control.
- **Project 2 – Software Team:** Responsible for data processing using fuzzy logic, visualization, and decision-making.

### 👥 Team Members

**Project 1: Hardware**
- Juan David Moreno Beltrán
- Julian David Lara Beltrán

**Project 2: Software**
- John Sebastián Galindo Hernández
- Miguel Ángel Moreno Beltrán

---

## 📦 Project Structure

```plaintext
/hydroespinaca
├── docker-compose.yml
├── .env                         # Optional: global environment variables
│
├── /frontend                   # React + Vite + TailwindCSS
│
├── /shared                     # .NET project with common logic (JWT, m2m, helpers)
│
├── /project1 (hardware-project)
│   ├── sensor-service          # Sensor reading via MQTT
│   ├── actuator-service        # Actuator activation
│   └── mqtt-agent              # MQTT client/subscriber
│
├── /project2 (software-project)
│   ├── auth-service            # Login, JWT tokens (RS256), password recovery (future)
│   ├── api-gateway             # Secure entry point to microservices
│   ├── bff-service             # Backend-for-frontend, manages sessions
│   ├── fuzzy-service           # Fuzzy controller (FastAPI + Python)
│   ├── notification-service    # Email sending (alerts, etc.)
│   └── catalog-service         # CRUD for:
│       ├── config              # Fuzzy rules
│       ├── data                # Historical readings
│       ├── log                 # System logs
│       └── admin               # Users and roles
│
└── /deploy
    ├── mosquitto/             # MQTT broker configuration
    └── nginx/                  # Reverse proxy configuration (optional)
```

---

## 🚀 Requirements

- Docker & Docker Compose
- WSL2 or native Linux (recommended)
- .NET 9 SDK (only if developing locally)
- Node.js 18+ (for frontend)

---

## 🧪 How to Run

```bash
git clone https://github.com/youruser/hydroespinaca.git
cd hydroespinaca
cp .env.example .env
docker compose up --build
```

Frontend: http://localhost:3000  
API Gateway: http://localhost  
MongoDB: mongodb://localhost:27017

---

## ✍️ Technical Notes

- Each microservice has its own `.env` for configuration.
- Hardware ↔ backend communication is handled via MQTT.
- Authentication tokens are signed using JWT RS256.
- `fuzzy-service` is implemented separately in Python (FastAPI).
- Hexagonal architecture is applied in every .NET microservice.

---

## 📜 License

MIT — free for research, modification, and educational use.