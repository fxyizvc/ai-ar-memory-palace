# 🧠 AI AR Memory Palace (Smart Blackboard)

**University Major Project | B.Tech Computer Science Engineering**

AI AR Memory Palace is an **AI-powered Augmented Reality learning system** that transforms physical classroom blackboards into **interactive 3D learning environments** using **On-Device Edge AI**, **Cloud Database**, and **Geo-Spatial Awareness**.

This platform ensures students can access **strictly filtered, curriculum-relevant assets** (3D Models & PDF Notes) **only when they are physically inside the college campus**, improving both learning engagement and content security.

---

## 📌 Project Overview

Unlike traditional marker-based AR applications, this system uses **Edge AI-based blackboard detection** to understand classroom environments in real time.

The system integrates:

- ✅ Geo-Fencing (GPS Enforcement)
- ✅ Cloud filtering through MongoDB Atlas
- ✅ Serverless API layer (Vercel Backend)
- ✅ 3D asset streaming in AR (GLB)
- ✅ Secure PDF notes delivery

---

## 🚀 Key Features

### 🌍 1) Geo-Fenced Access Control (GPS Enforcement)

The application continuously validates the user's location before enabling scanning.

- **Location Locking:** GPS is tracked using `GPSManager.cs`
- **Smart Validation:** Coordinates are verified against registered colleges stored in MongoDB Atlas
- **Distance Check:** Uses the **Haversine Formula**
- **Security Enforcement:** Scanning is disabled if the user is outside the campus radius (**500m**)

✅ Result: Students can only use the AR scan feature when they are physically present inside the college zone.

---

### 🧠 2) Cloud "Brain" Architecture

A scalable cloud architecture ensures secure and accurate delivery of academic content.

- **MongoDB Atlas** stores:
  - Branch (CSE, CE, ME...)
  - Semester (S1–S8)
  - Subject Codes
  - Asset links (GLB + PDF)

- **Vercel Backend (Node.js + Express)** acts as a secure middleware:
  - Example endpoint: `api/find.js`
  - Handles filtered requests securely
  - Protects database credentials
  - Prevents unauthorized data access

✅ Smart Filtering prevents mismatches like **S7 content appearing for S1 students**.

---

### 👁️ 3) Edge AI Blackboard Detection

A custom-trained object detection model enables real-time blackboard recognition.

- Uses **MobileNet SSD**
- Runs fully on-device using **TensorFlow Lite**
- Real-time detection at **30+ FPS**
- Detection confidence threshold: **> 60%**

✅ Once a blackboard is detected, the app triggers the cloud retrieval pipeline.

---

### 📦 4) Dual Asset Delivery (3D + Notes)

This system provides **both AR visual learning and digital notes** instantly.

#### 🎯 3D Augmentation
- Streams `.glb` models directly into the AR scene using **GLTFast**
- Auto adjustments:
  - Scale set to **0.05x**
  - Rotated **180°**
  - Anchored on detected wall/plane

#### 📄 PDF Notes Delivery
- Provides a secure **download link**
- Auto converts Google Drive "View" links → **Direct Download**
- Avoids viewer loading issues

✅ Students receive both **3D interaction + study notes** in one flow.

---

## 🛠️ Technical Stack

| Component | Technology Used | Purpose |
|----------|------------------|---------|
| Engine | Unity 6 | Core AR Development |
| AR Framework | ARFoundation (ARCore) | Tracking & Plane Detection |
| AI Model | MobileNet SSD (TFLite) | Blackboard Object Detection |
| Backend API | Node.js / Express (Vercel) | Serverless API + Security Layer |
| Database | MongoDB Atlas | Colleges, Subjects, Links Storage |
| Geo-Spatial | Native GPS Service | Location Validation |
| 3D Runtime Loader | GLTFast | Runtime GLB Import |

---

## ⚙️ System Workflow

The system follows a secure pipeline:

### ✅ Step 1 — Verification (GPS Layer)
- `GPSManager.cs` polls location every **10 seconds**
- Sends coordinates to the Vercel API
- If API returns:
```json
{ "found": true }
