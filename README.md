# 🏛️ AR Museum — An Interactive Augmented Reality Heritage Experience

An interactive Augmented Reality Museum built using **Unity 6, AR Foundation and Google ARCore**, bringing historically significant Indian sculptures and artefacts into the real world through mobile AR.

AR Museum allows users to explore a curated collection of digital 3D artefacts, place them in their physical surroundings, inspect them from different angles, learn about their historical and cultural significance, and listen to narrated descriptions.

The project aims to make cultural heritage more accessible through immersive, interactive and educational technology.

---

## 📸 Project Overview

AR Museum transforms a traditional museum experience into an interactive mobile AR application.

Users can:

- Explore a collection of historical Indian artefacts through an interactive welcome screen.
- Read detailed historical information about individual exhibits.
- View 3D artefacts in their actual surroundings using Augmented Reality.
- Detect real-world surfaces and place artefacts using touch interaction.
- Rotate and inspect artefacts interactively.
- Switch between different exhibits without restarting the AR experience.
- Listen to exhibit descriptions using Android Text-to-Speech narration.

---

## 🏺 Museum Collection

The current museum contains five exhibits representing different periods, artistic traditions and religious concepts from Indian history.

### 01. Nataraja

**Chola Bronze Tradition**

<details>
<summary>Explore exhibit</summary>

A representation of Shiva as Nataraja, the Lord of Dance, depicting the cosmic dance associated with creation, preservation and destruction.

</details>

### 02. Gandhara Buddha

**Gandharan Art · c. 3rd Century CE**

<details>
<summary>Explore exhibit</summary>

A representation of Buddha from the Gandhara artistic tradition, known for its distinctive sculptural style and interaction with Greco-Roman artistic influences.

</details>

### 03. Ashoka Lion Capital

**Mauryan Period · c. 3rd Century BCE**

<details>
<summary>Explore exhibit</summary>

A digital representation of the Ashoka Lion Capital, associated with Emperor Ashoka and the Mauryan Empire.

</details>

### 04. Dancing Girl of Mohenjo-daro

**Indus Valley Civilisation · c. 2500 BCE**

<details>
<summary>Explore exhibit</summary>

A digital representation of the celebrated bronze figurine discovered at Mohenjo-daro. The figure demonstrates the metalworking skills and artistic expression of the Indus Valley Civilisation.

</details>

### 05. Harihara

**Sandstone Relief · Shaivism and Vaishnavism · c. 1000 CE**

<details>
<summary>Explore exhibit</summary>

A representation of Harihara, a composite form combining Shiva and Vishnu into a single divine figure. The sandstone relief illustrates the relationship between Shaivite and Vaishnavite traditions.

</details>

---

## ✨ Features

### 1. Interactive Welcome Screen

- Museum introduction and exhibit showcase.
- Individual exhibit cards with 3D model thumbnails.
- Featured exhibit selection.
- Quick access to exhibit information.
- Navigation into the AR experience.

### 2. Interactive Museum Guide

Each exhibit has a dedicated information screen containing:

- Exhibit title and category.
- Historical period.
- Introduction.
- Historical context.
- Cultural significance.
- Provenance information.
- Narration controls.

### 3. Augmented Reality Experience

Powered by Unity AR Foundation and Google ARCore.

- Real-world horizontal plane detection.
- AR camera passthrough.
- Touch-based artefact placement.
- Repositioning of placed artefacts.
- Real-world scale presentation.
- AR interaction with detected surfaces.

### 4. 3D Artefact Interaction

- Individual exhibit prefabs.
- Interactive rotation controls.
- Step-based rotation and reset functionality.
- Stable placement using a bottom-centred pivot.
- Runtime exhibit switching.
- Individual colliders for interaction.

### 5. Audio Narration

- Android native Text-to-Speech integration.
- Play, pause, resume and stop controls.
- Exhibit-specific narration.
- Automatic narration handling when switching exhibits.

### 6. Dynamic Exhibit Management

The application uses a shared exhibit management architecture.

New exhibits can be registered through ExhibitData assets and their corresponding prefabs without recreating the entire AR interaction system.

---

## 🛠️ Technology Stack

| Technology | Purpose |
|---|---|
| Unity 6 (6000.6.9f1) | Main development platform |
| C# | Application logic and interaction |
| AR Foundation 6.6.2 | Cross-platform AR framework |
| Google ARCore XR Plugin 6.6.2 | Android AR support |
| URP | Rendering pipeline |
| glTFast 6.21.0 | GLB asset import support |
| Unity Built-in Model Importer | OBJ and FBX model importing |
| Android Text-to-Speech | Exhibit narration |
| Unity MCP | AI-assisted Unity editor workflow |

### Development Environment

- Target platform: Android
- Tested device: Samsung Galaxy SM-G990E
- Graphics API: OpenGLES3
- Input System: Unity New Input System

---

## 🏗️ System Architecture

The application follows a modular architecture in which exhibit data, artefact visuals, AR placement, user interaction and narration are handled by separate components.

### High-Level Architecture

```text
                  AR MUSEUM
                      |
          +-----------+-----------+
          |                       |
     Welcome Screen         Museum Guide
          |                       |
    Exhibit Selection       Exhibit Details
          |                       |
          +-----------+-----------+
                      |
              Museum Manager
                      |
               Exhibit Data
                      |
             Exhibit Prefabs
                      |
                AR Experience
                      |
          +-----------+-----------+
          |           |           |
      AR Session   Plane       AR Camera
                   Detection
                      |
                ArtifactPlacer
                      |
              Placed 3D Exhibit
                      |
          +-----------+-----------+
          |           |           |
       Rotation   Information   Narration
       Controls     Placard      Service
```

### Core Components

| Component | Responsibility |
|---|---|
| `ExhibitData` | Stores exhibit information and prefab references |
| `ExhibitArtifact` | Connects an artefact instance to its exhibit data |
| `ArtifactTurntable` | Handles interactive artefact rotation |
| `MuseumExhibitManager` | Manages registered exhibits and selection |
| `ExhibitSelectorUI` | Dynamically generates exhibit selection controls |
| `ArtifactPlacer` | Handles AR placement and repositioning |
| `ARExperienceGate` | Controls entry into the AR experience |
| `WelcomeScreenController` | Manages the welcome screen |
| `ExhibitDetailsScreen` | Displays detailed exhibit information |
| `ExhibitInfoPanel` | Displays the placed artefact's information placard |
| `NarrationService` | Handles audio narration and Android TTS |
| `SafeAreaFitter` | Adapts UI layout to device safe areas |

---

## 📁 Project Structure

The following is a simplified representation of the important project directories.

```text
AR_Museum/
│
├── Assets/
│   │
│   ├── Artifacts/
│   │   ├── Nataraja/
│   │   ├── GandharaBuddha/
│   │   ├── AshokaLion/
│   │   ├── DancingGirl/
│   │   └── Harihara/
│   │
│   ├── Exhibits/
│   │   ├── Data/
│   │   │   ├── Nataraja.asset
│   │   │   ├── GandharaBuddha.asset
│   │   │   ├── AshokaLion.asset
│   │   │   ├── DancingGirl.asset
│   │   │   └── Harihara.asset
│   │   │
│   │   └── Thumbnails/
│   │
│   ├── Prefabs/
│   │   └── Artifacts/
│   │
│   ├── Scenes/
│   │   └── SampleScene.unity
│   │
│   └── Scripts/
│       ├── AR/
│       ├── Audio/
│       ├── Exhibits/
│       ├── Museum/
│       └── UI/
│
├── Packages/
│   └── manifest.json
│
├── ProjectSettings/
│
├── .gitattributes
└── .gitignore
```

---

## 🚀 Getting Started

### Prerequisites

Before opening the project, ensure you have:

1. Unity Hub.
2. Unity Editor 6000.6.9f1.
3. Android Build Support for Unity, including:
   - Android SDK & NDK Tools.
   - OpenJDK.
4. A compatible Android device supporting Google ARCore.
5. USB debugging enabled if deploying directly from Unity.

### Clone the Repository

```bash
git clone https://github.com/DarkEntity27/AR-Museum.git
```

Navigate into the project:

```bash
cd AR-Museum
```

### Open in Unity

1. Open Unity Hub.
2. Select **Add Project from Disk**.
3. Choose the cloned `AR-Museum` directory.
4. Open the project using Unity 6000.6.9f1.
5. Allow Unity to import and compile the assets.

**Important:** Allow the initial asset import to finish before opening or building the scene.

### Configure Android Build

1. Open `File → Build Profiles`.
2. Select Android as the target platform.
3. Switch the project to Android if necessary.
4. Verify that ARCore XR Plugin is installed and enabled.
5. Verify that the Android graphics API includes OpenGLES3.
6. Ensure the New Input System is enabled.
7. Connect a compatible ARCore-supported Android device.

### Run the Application

Open:

```text
Assets/Scenes/SampleScene.unity
```

Then:

1. Connect your Android device.
2. Enable USB debugging.
3. Select Build And Run.
4. Allow camera permissions when prompted.
5. Launch AR Museum.

---

## 📱 How to Use

### Step 1 — Explore the Museum

Launch the application to access the welcome screen.

Browse the available exhibits and select an artefact.

### Step 2 — View Exhibit Details

Open an exhibit's information screen to explore its history, cultural significance and other available details.

### Step 3 — Enter AR Mode

Select the option to enter the AR experience.

The application activates the AR camera and begins detecting supported real-world surfaces.

### Step 4 — Place an Artefact

- Point the camera towards a suitable surface.
- Allow the application to detect the plane.
- Tap the desired position to place the artefact.
- Tap another suitable position to reposition it.

### Step 5 — Interact with the Artefact

Use the available controls to:

- Rotate the artefact.
- Reset its orientation.
- Open its information placard.
- Play or control its narration.

### Step 6 — Switch Exhibits

Use the exhibit selector to switch between the five available artefacts.

The existing artefact is replaced with the newly selected exhibit.

---

## 🧩 Adding a New Exhibit

One of the project's design goals is to make future exhibit additions straightforward.

The existing architecture supports adding exhibits through reusable data assets and prefabs.

### Step 1 — Prepare the 3D Model

Obtain a suitable 3D model in a supported format such as:

- FBX
- OBJ
- GLB

Import the model into Unity and configure its materials and textures.

### Step 2 — Prepare the Model

Verify:

- Correct orientation.
- Appropriate display scale.
- Bottom-centred pivot.
- Correct material assignments.
- Suitable collider dimensions.

### Step 3 — Create the Prefab

Create an artefact prefab using the established structure:

```text
NewArtifact
│
├── BoxCollider
├── ExhibitArtifact
├── ArtifactTurntable
│
└── Model
    └── Imported 3D Model
```

Assign the corresponding exhibit data and visual root references.

### Step 4 — Create ExhibitData

Create a new ExhibitData ScriptableObject asset.

Populate the relevant fields:

- Title
- Subtitle
- Category
- Period
- Introduction
- Historical context
- Cultural significance
- Provenance note
- Narration
- Exhibit ID
- Artifact prefab
- Display height

### Step 5 — Register the Exhibit

Add the new ExhibitData asset to the existing MuseumExhibitManager collection.

Update the welcome screen's serialized thumbnail and tradition references if required by the current scene setup.

Verify that the new exhibit appears in:

- Welcome screen.
- Exhibit selector.
- Details screen.
- AR placement workflow.

The existing placement, rotation, placard and narration systems can then be reused.

---

## ⚙️ Important Technical Notes

### AR Rendering

The project uses AR Foundation with Google ARCore for Android AR functionality.

OpenGLES3 is configured as the Android graphics API. Vulkan is disabled in the current project configuration.

### Model Optimization

3D models can have substantially different polygon counts and texture resolutions.

Mobile performance depends on:

- Triangle count.
- Texture resolution.
- Material complexity.
- Draw calls.
- Memory usage.
- Device GPU capabilities.

Large models should be tested on the target device before deciding whether optimization is necessary.

### Texture Import Settings

High-resolution source textures may require platform-specific compression and maximum-size overrides.

The project uses Android texture import settings to control memory consumption.

### Model Orientation and Pivot

Correct model orientation and pivot placement are important for AR interaction.

Artefact roots should be positioned so that:

- The base rests on the detected surface.
- Rotation occurs around the intended pivot.
- The model maintains its intended upright orientation.

### Android Narration

Narration uses Android native Text-to-Speech functionality when a narration audio clip is not provided.

Text-to-Speech availability is platform-dependent, so narration should be tested on an Android device.

---

## 🧪 Testing

The application has been tested on:

**Device:** Samsung Galaxy SM-G990E

The five implemented exhibits have been tested for:

- Model loading.
- AR placement.
- Repositioning.
- Exhibit switching.
- Rotation controls.
- Information display.
- Narration behaviour.
- General loading performance.

A backup APK has also been retained during development.

---

## 🔮 Future Enhancements

Potential future improvements include:

- Expanding the museum with additional historical artefacts.
- Adding more detailed 3D exhibit descriptions.
- Adding multilingual narration.
- Introducing additional interactive exhibit information.
- Improving visual effects and exhibit presentation.
- Adding guided museum tours.
- Supporting additional AR platforms and devices.
- Adding more accessibility features.

---

## 👨‍💻 Development

**Project:** AR Museum

**Platform:** Unity 6

**Target:** Android

**Repository:** [DarkEntity27/AR-Museum](https://github.com/DarkEntity27/AR-Museum)

**Developer:** Hemanth Sadhu

---

## 📜 Disclaimer

This project is developed for undergraduate academic and educational purposes.

The digital models are used to provide an interactive representation of cultural heritage artefacts. Their AR display dimensions are presentation scales and do not necessarily represent the original physical dimensions.

Historical descriptions and provenance information should be interpreted in accordance with the available source information for each exhibit.

All third-party 3D models, textures and other resources remain subject to their respective licences and attribution requirements. Please verify the licensing terms of individual assets before redistribution.

---

## ⭐ Acknowledgements

Built with:

- [Unity](https://unity.com/)
- [AR Foundation](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@latest)
- [Google ARCore](https://developers.google.com/ar)
- [glTFast](https://github.com/atteneder/glTFast)

If you find this project interesting, consider giving the repository a star!

**Explore history. Experience heritage. Discover through AR.**
