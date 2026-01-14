# 🧠 AI AR Memory Palace (Smart Blackboard)

**University Major Project | B.Tech CSE**
*An AI-powered Augmented Reality system that transforms physical classroom blackboards into interactive 3D learning environments using On-Device Edge AI and Cloud Connectivity.*

---

## 📌 Project Overview
**AI AR Memory Palace** is an educational technology platform designed to bridge the gap between static physical teaching aids and dynamic digital content.

By combining **Artificial Intelligence** with **Augmented Reality**, this application allows a smartphone to "understand" the classroom environment. It detects physical blackboards in real-time and overlays interactive 3D flashcards (Memory Palaces) directly onto them. These 3D models are streamed from the cloud, ensuring that learning materials are always up-to-date without requiring app updates.

---

## 🚀 Key Features (Implemented)

### 👁️ AI-Powered Blackboard Detection
* Uses a custom-trained **MobileNet SSD (Single Shot Detector)** model running locally on the device.
* Achieves **real-time inference (>30 FPS)** on mobile hardware using TensorFlow Lite.
* Filters results using a confidence threshold (currently set to >0.6) to prevent false positives.

### ⚓ Spatial Anchoring & Tracking
* Converts 2D AI detection coordinates into 3D world space using **AR Raycasting**.
* "Locks" virtual content to the physical blackboard using **AR Anchors**, preventing drift even if the user moves around.
* **Smart Positioning:** Automatically offsets content **30cm** from the wall to create a "floating hologram" effect and prevent texture clipping.

### ☁️ Cloud-Based Content Streaming
* Decouples the app logic from the content.
* Dynamically downloads 3D assets (`.glb` files) from **Google Drive** at runtime.
* Uses asynchronous loading (`glTFast`) to ensure the app never freezes while downloading heavy models.

### ✨ Immersive UX
* **Visual Cleaning:** Automatically detects when a lock is established and hides the AR Point Cloud (dots) and Plane visuals for a clutter-free view.
* **Auto-Orientation:** Algorithms ensure the content always spawns facing the user, regardless of the blackboard's angle.

---

## 🛠️ Technical Stack

| Component | Technology Used | Purpose |
| :--- | :--- | :--- |
| **Engine** | Unity 6 | Core AR/VR Development Environment |
| **AR Framework** | ARFoundation 5.x (ARCore) | Surface detection and motion tracking |
| **AI Inference** | TensorFlow Lite (TFLite) | Running the `.tflite` model on Android |
| **Vision Model** | MobileNet SSD | Object detection (Blackboard) |
| **3D Loading** | glTFast | Runtime import of GLB/GLTF assets |
| **Backend** | Google Drive API | Hosting and serving 3D Flashcards |
| **Render Pipeline** | URP (Universal Render Pipeline) | High-performance mobile graphics |

---

## ⚙️ System Architecture & Workflow

The system operates in a continuous loop comprising four distinct stages:

### 1. Perception (The AI Layer)
* The camera feed is intercepted and resized to a `640x640` texture.
* This texture is normalized and passed to the **TFLite Interpreter**.
* The model outputs a tensor containing Bounding Boxes `[x, y, w, h]` and Class Probabilities.

### 2. Translation (The Coordinate Layer)
* The raw AI coordinates (normalized 0.0 to 1.0) are mapped to Unity's Screen Space coordinates (Pixels).
* A mathematical transformation flips the Y-axis (Tensorflow origin: Top-Left vs. Unity origin: Bottom-Left) to ensure correct alignment.

### 3. Localization (The AR Layer)
* A **Raycast** is fired from the camera's center point towards the detected blackboard center.
* The ray intersects with the **AR Plane** (the physical wall detected by ARCore).
* A "Hit Pose" (Position + Rotation) is extracted from the physical surface.

### 4. Augmentation (The Cloud Layer)
* An empty "Container" object is instantiated at the Hit Pose.
* An `ARAnchor` component is added to rigidly attach this container to the world coordinates.
* The app initiates a `UnityWebRequest` to the Google Drive direct link.
* The 3D model is downloaded, instantiated, scaled to `0.05x`, and rotated `180°` to face the user.

---

## 📸 Screenshots

*(Add your screenshots here from the `Assets` folder or upload them to the repo)*
> *1. Scanning Phase (Point Cloud)*
> *2. Detection Phase (Red Box / Confidence Score)*
> *3. Anchoring Phase (3D Card Floating on Board)*
![WhatsApp Image 2026-01-02 at 8 20 58 PM](https://github.com/user-attachments/assets/a5d50b51-1e1a-4221-970a-acee96317baa)

---

## 👨‍🎓 Project Status
**Current Version:** v1.0 (Prototype)
**Status:** Functional AR Pipeline with Cloud Integration.

**Future Roadmap:**
- [ ] Implement Pinch-to-Resize and Drag-to-Move interactions.
- [ ] Integrate PDF-to-3D conversion pipeline.
- [ ] Multi-class detection (detecting different subjects).

---

**Developed by:**
* **Muhammed Fayiz V C**
* *Major Project - B.Tech Computer Science Engineering*
