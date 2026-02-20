# 🧠 AI AR Memory Palace (Smart Blackboard)

**B.Tech Computer Science Engineering (8th Semester) | Major Project**

An AI-powered Augmented Reality system that transforms ordinary classroom blackboards into interactive 3D learning environments using **On-Device Edge AI**, **Cloud Connectivity**, and **Geo-Spatial Security**.

## 📌 Project Overview

AI AR Memory Palace is a location-aware educational platform designed to bridge physical classroom infrastructure with modern digital learning.

Unlike typical AR apps that depend on static image markers or costly headsets, this system uses a standard smartphone camera + **Edge AI (TensorFlow Lite)** to understand the classroom environment in real time. It combines **GPS Geofencing** with a **Cloud Database** to ensure students can only access strictly filtered, curriculum-relevant content (3D models & PDF notes) when physically present on their college campus.

## 📸 Screenshots

1. **Smart Dropdown Selection (Branch / Sem / Subject)**  
   <img src="https://github.com/user-attachments/assets/a99fbc29-1fb8-490f-b3a9-545cb58c42eb" width="220"/>

2. **GPS "Connected" State**  
   <img src="https://github.com/user-attachments/assets/0a2b6682-5b79-4f04-82aa-a1b43e826541" width="220"/>

3. **AI Detection & AR Augmentation**  
   <img src="https://github.com/user-attachments/assets/c2975876-9d47-44cd-9814-a3732ac99910" width="220"/>

---

## 🚀 Key Features

### 🎠 3D Hologram Carousel (Multi-Module Support)
- **Dynamic Gallery**: Subjects with multiple modules (e.g. Module 1–5) load as a seamless carousel from the cloud.
- **Smart Memory Management**: Tapping ← or → instantly unloads the previous 3D model (frees RAM) and streams the next .glb file using **glTFast**.
- **Synchronized Notes**: The "Download PDF" button automatically updates to the secure Google Drive link matching the currently viewed module.

### 🌍 Geo-Fenced Access Control (GPS Security)
- Continuously monitors user GPS coordinates against MongoDB-stored college locations.
- AR scanning is **physically disabled** outside the 500 m campus radius — ensures academic integrity and prevents off-campus misuse.

### 📚 Dynamic Syllabus Engine
- Cascading, context-aware dropdown UI (no manual typing needed).
- Selecting **Branch** (CSE, ME, CE, EEE…) + **Semester** (S1–S8) auto-populates the correct subject list for that curriculum.

### 👁️ Edge AI Blackboard Detection
- Custom-trained **YOLOv8n** model running locally via **TensorFlow Lite** (completely offline).
- Real-time inference at **>30 FPS** on mobile devices (quantized INT8 export for optimal performance).
- Draws dynamic bounding boxes to guide users to position the blackboard correctly.

### 🧠 Cloud "Brain" Architecture
- **MongoDB Atlas** + **Vercel** serverless Node.js/Express API (`api/find.js`) handles complex queries and hides credentials.
- Custom **C#** algorithm auto-converts standard Google Drive "View" links into direct-download binary streams for smooth AR content injection.

## 🛠️ Technical Stack

| Component          | Technology Used              | Purpose                                      |
|--------------------|------------------------------|----------------------------------------------|
| Engine             | Unity 6                      | Core AR development environment              |
| AR Framework       | ARFoundation (ARCore)        | Surface tracking & device positioning        |
| AI Model           | YOLOv8n (TFLite)             | Offline real-time blackboard detection       |
| Backend API        | Node.js / Express (Vercel)   | Serverless API & secure payload routing      |
| Database           | MongoDB Atlas                | Stores subjects, modules, colleges & links   |
| 3D Loading         | glTFast                      | Runtime .glb streaming (keeps APK size small)|

## 🔬 Innovation: Our Project vs. Base Paper

Inspired by, but significantly improved upon, the paper:

**"Augmented Reality-Based Human Memory Enhancement Using Artificial Intelligence"** (Makhataeva et al., 2022) — "ExoMem"

| Feature            | Base Paper ("ExoMem")         | Our Project (Smart Blackboard)          | Our Advantage                              |
|--------------------|-------------------------------|-----------------------------------------|--------------------------------------------|
| Hardware           | Microsoft HoloLens 2 (~$3500) | Standard Android smartphone             | Extremely accessible — every student owns one |
| Detection          | ArUco Markers (physical QR)   | AI Object Detection (YOLOv8n)           | No classroom modifications required; more robust & modern |
| Localization       | Visual marker tracking        | GPS Geofencing (satellite-based)        | Stronger on-campus-only security           |
| Data Source        | Hardcoded local memory        | MongoDB Cloud Database                  | Faculty can update content instantly       |

## ⚙️ System Architecture Workflow

1. **Verification (GPS Layer)**  
   `GPSManager.cs` continuously polls location → unlocks app only inside 500 m campus radius.

2. **Selection (Syllabus Layer)**  
   User picks Branch + Semester → `SyllabusManager.cs` fetches filtered subject list.

3. **Perception (AI Layer)**  
   TensorFlow Lite processes camera frames with **YOLOv8n** → if blackboard confidence >60%, anchors AR session.

4. **Augmentation (Cloud Layer)**  
   `MongoManager.cs` queries Vercel API → retrieves module array → spawns Hologram Carousel with 3D models + matching PDF links.

## 👨‍💻 Developer / Author

- **Muhammed Fayiz V C**  
- 8th Semester, B.Tech Computer Science Engineering (CSE)  
- St. Thomas College of Engineering and Technology, Kannur

---

Built with ❤️ for better classroom learning experiences.  
Now powered by **YOLOv8n** for faster, more accurate blackboard detection!
