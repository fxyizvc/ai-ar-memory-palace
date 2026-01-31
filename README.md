# 🧠 AI AR Memory Palace (Smart Blackboard)

[![Unity](https://img.shields.io/badge/Unity-6-black?style=for-the-badge&logo=unity)](https://unity.com/)
[![ARFoundation](https://img.shields.io/badge/ARFoundation-ARCore-blue?style=for-the-badge)](https://unity.com/unity/features/arfoundation)
[![TensorFlow](https://img.shields.io/badge/TensorFlow-Lite-orange?style=for-the-badge&logo=tensorflow)](https://www.tensorflow.org/lite)
[![MongoDB](https://img.shields.io/badge/MongoDB-Atlas-green?style=for-the-badge&logo=mongodb)](https://www.mongodb.com/atlas)
[![Vercel](https://img.shields.io/badge/Vercel-Backend-black?style=for-the-badge&logo=vercel)](https://vercel.com)

> **University Major Project | B.Tech Computer Science Engineering**

An AI-powered Augmented Reality system that transforms physical classroom blackboards into interactive 3D learning environments using **On-Device Edge AI**, **Cloud Connectivity**, and **Geo-Spatial Security**.

---

## 📌 Project Overview

**AI AR Memory Palace** is a location-aware educational platform designed to bridge the gap between physical infrastructure and digital learning.

Unlike traditional AR apps that rely on static image markers, this system uses **Edge AI (TensorFlow Lite)** to "see" and understand the classroom environment in real-time. It combines **GPS Geofencing** with a **Cloud Database** to ensure that students can only access strictly filtered, curriculum-relevant content (3D Models & PDF Notes) when they are physically present on their college campus.

---

## 🚀 Key Features

### 🌍 Geo-Fenced Access Control (GPS Security)
- **Location Locking**: The app continuously monitors the user's GPS coordinates using a custom `GPSManager`
- **Smart Validation**: Before allowing any scans, it cross-references the user's location with a MongoDB database of registered colleges
- **Security**: The scanning feature is physically disabled if the user is outside the college campus radius (500m), ensuring academic integrity

### 📚 Dynamic Syllabus Engine
- **Cascading UI**: Replaces manual text entry with smart, context-aware dropdowns
- **Auto-Populate**: Selecting a Branch (CSE, ME, CE, EEE) and Semester (S1-S8) automatically fetches the correct subject list for that specific curriculum
- **Error Prevention**: Eliminates user typos and ensures queries strictly match the database records

### 🧠 Cloud "Brain" Architecture
- **MongoDB Atlas**: Stores metadata for thousands of subjects, including specific Branch/Semester mappings and Google Drive asset links
- **Vercel Backend**: A custom Node.js/Express API (`api/find.js`) acts as the traffic controller, handling filtered requests and protecting database credentials
- **Smart Filtering**: Prevents data leaks (e.g., S7 notes appearing in S1) by enforcing strict parameter matching at the API level

### 👁️ Edge AI Blackboard Detection
- **MobileNet SSD**: Uses a custom-trained Single Shot Detector (SSD) model running locally via TensorFlow Lite
- **Real-Time Inference**: Achieves >30 FPS performance on mobile hardware
- **Visual Feedback**: Draws dynamic bounding boxes around detected blackboards to guide the user

### 📦 Dual-Asset Delivery
- **3D Augmented Reality**: Streams `.glb` models directly into the AR scene using GLTFast, auto-scaled and anchored to the physical wall
- **Digital Notes**: Provides a direct, secure download link for PDF notes via the phone's browser
- **Auto-Link Fixer**: Automatically converts Google Drive "View" links into "Direct Download" streams to prevent loading errors

---

## 🛠️ Technical Stack

| Component | Technology Used | Purpose |
|-----------|----------------|---------|
| **Engine** | Unity 6 | Core AR Development Environment |
| **AR Framework** | ARFoundation (ARCore) | Surface Tracking & Plane Detection |
| **AI Model** | MobileNet SSD (TFLite) | Object Detection (Blackboard) |
| **Backend API** | Node.js / Express (Vercel) | Serverless API Functions |
| **Database** | MongoDB Atlas | Storing Subjects, Colleges, & Links |
| **Geo-Spatial** | Native GPS Service | Location Verification |
| **3D Loading** | glTFast | Runtime GLB Import from URL |

---

## ⚙️ System Architecture & Workflow

The system operates in a strictly **validation-first pipeline**:

### 1. 🔐 Verification (The GPS Layer)
- `GPSManager.cs` polls the user's location every 10 seconds
- Coordinates are sent to the Vercel API endpoint
- **Result**: If `{"found": true}` is returned, the app unlocks the "SCAN" functionality

### 2. 📋 Selection (The Syllabus Layer)
- User selects **Branch** (e.g., CSE) and **Semester** (e.g., S3)
- `SyllabusManager.cs` instantly populates the Subject Dropdown with the correct course list (e.g., CST201, CST203)
- User selects the specific subject they are attending

### 3. 👁️ Perception (The AI Layer)
- The user points the camera at the blackboard and clicks **Scan**
- `BlackboardDetector.cs` captures the camera frame and runs TFLite inference
- If a board is detected (>60% confidence), the app triggers the Cloud Search

### 4. 🌐 Augmentation (The Cloud Layer)
- `MongoManager.cs` sends a request: `GET /api/find?subject=CST203&branch=CSE&semester=S3`
- **3D**: The GLB model URL is fetched, fixed, and the model is spawned 30cm in front of the board
- **PDF**: A "Download Notes" button appears, allowing immediate access to course materials

---

## 📸 Screenshots

> *Add your screenshots here*

1. Smart Dropdown Selection (Branch/Sem/Subject)
2. GPS "Connected" State
3. AI Detection & AR Augmentation

---

## 🚦 Getting Started

### Prerequisites
- Unity 6 or higher
- Android device with ARCore support
- MongoDB Atlas account
- Vercel account for API deployment

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/ai-ar-memory-palace.git
   cd ai-ar-memory-palace
   ```

2. **Open in Unity**
   - Open Unity Hub
   - Add the project folder
   - Open with Unity 6

3. **Configure Backend**
   - Set up MongoDB Atlas cluster
   - Deploy the Vercel API (`api/find.js`)
   - Update API endpoint in Unity scripts

4. **Build & Deploy**
   - Configure Android Build Settings
   - Enable ARCore support
   - Build and install on your device

---

## 📊 Project Status

- **Current Version**: v2.0 (Cloud-Native Release)
- **Status**: Fully functional with GPS enforcement, Dynamic UI, and Cloud Database integration

### 🗺️ Future Roadmap

- [ ] **Multi-College Support**: Scaling the DB to support multiple campus locations
- [ ] **OCR Integration**: Reading subject codes directly from chalk writing on the board
- [ ] **Admin Panel**: A web portal for professors to upload notes and 3D models directly
- [ ] **Offline Mode**: Cache frequently accessed models for offline use
- [ ] **Analytics Dashboard**: Track student engagement and content usage

---

## 👨‍💻 Author

**Muhammed Fayiz V C**  
Major Project - B.Tech Computer Science Engineering

---

## 📄 License

This project is developed as part of university curriculum requirements.

---

## 🙏 Acknowledgments

- TensorFlow Lite team for edge AI capabilities
- Unity ARFoundation team for AR framework
- MongoDB and Vercel for cloud infrastructure

---

## 📞 Contact

For questions or collaboration opportunities, feel free to reach out!

---

<div align="center">
  <strong>Built with 💡 Innovation and 🎓 Academic Excellence</strong>
</div>
