# AI AR Memory Palace (Smart Blackboard)

**An AI-powered Augmented Reality Educational Platform**  
**B.Tech CSE Major Project**

An intelligent AR system that transforms ordinary classroom blackboards into interactive 3D learning environments using **On-Device Edge AI**, **Cloud Database**, and **GPS Geo-fencing**.

The app ensures students can only access curriculum-relevant 3D models and notes when physically present inside their college campus.

## ✨ Key Features

- **Geo-Fenced Access Control**  
  Real-time GPS monitoring with Haversine formula validation  
  Scanning is disabled outside the 500m college campus radius  
  Secure location verification using MongoDB-stored college coordinates

- **Cloud-Powered Smart Filtering**  
  MongoDB Atlas stores subject metadata (Branch, Semester, Subject Code)  
  Node.js/Express backend on Vercel enforces strict filtering (Branch + Semester + Subject)  
  Prevents unauthorized access to higher-semester content

- **On-Device Edge AI Blackboard Detection**  
  Custom-trained MobileNet SSD model running locally via TensorFlow Lite  
  Real-time blackboard detection (>30 FPS) on mobile device

- **Dual Asset Delivery**  
  - Augmented Reality 3D Models (.glb) loaded using GLTFast  
  - Secure PDF Notes download with auto-converted direct links from Google Drive

- **Clean & Secure Architecture**  
  No API keys exposed in client  
  Serverless backend handles all database queries

## 🛠️ Tech Stack

| Component              | Technology                          |
|-----------------------|-------------------------------------|
| Game Engine           | Unity 6                             |
| AR Framework          | AR Foundation + ARCore              |
| AI Model              | MobileNet SSD (TensorFlow Lite)     |
| Backend               | Node.js + Express (Vercel)          |
| Database              | MongoDB Atlas                       |
| 3D Model Loading      | glTFast                             |
| Location Services     | Native GPS + Haversine Formula      |

## 📍 System Architecture & Workflow

1. **Verification Layer** → GPSManager checks user location every 10s  
2. **Perception Layer** → AI detects blackboard and confirms subject code  
3. **Query Layer** → Secure filtered request to `/api/find`  
4. **Augmentation Layer** → Loads 3D model (scaled, rotated) + PDF download button

## 🚀 Project Status

- **Current Version**: v2.0 (Cloud-Native Release)  
- **Status**: Fully functional with GPS enforcement, MongoDB integration, and AR augmentation  
- **Tested**: Real-time blackboard detection + location-locked content delivery

## 🛣️ Future Roadmap

- [ ] Multi-college support with scalable database design  
- [ ] OCR Integration to auto-read subject code from chalkboard  
- [ ] Professor/Admin web panel for uploading notes & 3D models  
- [ ] Offline mode support

## 👥 Team Members

- **Muhammed Fayiz V C** (Team Lead & Primary Developer)  
- Jagan K K  
- Rhuthoshika K  
- Jibin P V

**Department of Computer Science & Engineering**  
**B.Tech Major Project (2025-2026)**

## 📸 Screenshots

(Add screenshots here – recommended layout)

1. GPS Location Verification Screen  
2. Branch & Semester Smart Filtering  
3. AI Blackboard Detection + 3D AR Model Overlay  
4. PDF Notes Download Flow

## 📝 License

This project is for academic purposes. All rights reserved.

---

**Made with ❤️ using Unity & Edge AI**