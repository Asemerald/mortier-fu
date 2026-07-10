# Contributions
![Alt](https://repobeats.axiom.co/api/embed/e727dce87137596776ac0f99df7d78a1abd9f801.svg "Repobeats analytics image")

#  Mortier FU Modding Framework

A C# modding framework for Mortier FU (Unity 6000.2) that allows you to create custom mods and modify game behavior.

## 📋 Prerequisites

- **One of the following:**
  - [.NET SDK 6.0+](https://dotnet.microsoft.com/download) (recommended - supports all versions including 9.0)
  - OR [.NET Framework 4.7.2+ Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net472)
- **IDE:**
  - [JetBrains Rider](https://www.jetbrains.com/rider/) (recommended)
  - OR [Visual Studio 2022](https://visualstudio.microsoft.com/) (Community edition is free)
- **Game:**
  - **Option 1:** Mortier FU purchased on [Steam](https://store.steampowered.com/app/XXXXX)
  - **Option 2:** Build from source - The Unity project is available in this repository
- **Skills:**
  - Basic C# knowledge

> [!NOTE]
If you're building from source, you'll find the full Unity project in the `/Mortier FU` folder. 

## 🚀 Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/Asemerald/mortier-fu.git
cd mortier-fu
```

### 2. Extract Unity DLLs
                  
You need to copy the required Unity assemblies from your game installation to the `libs/` folder.  

> [!IMPORTANT]
> These DLLs are **not included** in the repository due to Unity's licensing. You must extract them from your own game installation.

**Location:** `<Game Install Path>/Mortier FU_Data/Managed/`

**Required files:**
- `UnityEngine.dll`
- `UnityEngine.CoreModule.dll`
- `MortierFu.Runtime.dll` (game-specific assembly)

> [!NOTE]
> You may need others assembly depending on what you want to achieve, for example you main need the `UnityEngine.InputLegacyModule.dll` if you want to use user input. 

**Copy them to:**
```
CustomMod/
└── libs/
    ├── UnityEngine.dll
    ├── UnityEngine.CoreModule.dll
    └── MortierFu.Runtime.dll
```

### 3. Build the Project

#### Using Rider:
1. Open `CustomMod.csproj` in Rider
2. Right-click on the project → Build
3. The compiled DLL will be in `bin/Debug/net472/` or `bin/Release/net472/`

#### Using Command Line:
```bash
dotnet build -c Release
```

> [!TIP]
> Use `Release` configuration for better performance in production mods.

### 4. Install Your Mod

Copy the compiled `CustomMod.dll` to your game's mod folder:

```
<Game Install Path>/Mods/CustomMod.dll
```

> [!NOTE]
> The exact mod folder location may vary depending on your modding framework. Check your mod loader documentation.

---

## 🛠️ Creating Your First Mod

### Basic Mod Structure

```csharp
using UnityEngine;
using Mortierfu;

namespace CustomMod
{
    public class MyFirstMod : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("My first mod loaded!");
        }
        
        void Update()
        {
            // Your mod logic here
        }
    }
}
```

### Example Mods

#### 1. **Speed Modifier Mod**

Increases player movement speed by 50%:

```csharp
using UnityEngine;

namespace CustomMod
{
    public class SpeedMod : MonoBehaviour
    {
        private float speedMultiplier = 1.5f;
        
        void Update()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var rb = player.GetComponent<Rigidbody>();
                if (rb != null && Input.GetKey(KeyCode.W))
                {
                    rb.velocity *= speedMultiplier;
                }
            }
        }
    }
}
```

> [!WARNING]
> Modifying physics values can cause unexpected behavior. Always test thoroughly!

#### 2. **Debug Info Display Mod**

Shows player position and FPS on screen:

```csharp
using UnityEngine;

namespace CustomMod
{
    public class DebugInfoMod : MonoBehaviour
    {
        private float deltaTime = 0.0f;
        
        void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }
        
        void OnGUI()
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var position = player.transform.position;
                var fps = 1.0f / deltaTime;
                
                GUI.Label(new Rect(10, 10, 300, 20), $"Position: {position}");
                GUI.Label(new Rect(10, 30, 300, 20), $"FPS: {fps:0.}");
            }
        }
    }
}
```

#### 3. **Custom Key Binding Mod**

Teleports player to spawn on pressing F5:

```csharp
using UnityEngine;

namespace CustomMod
{
    public class TeleportMod : MonoBehaviour
    {
        private Vector3 spawnPoint = new Vector3(0, 10, 0);
        
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    player.transform.position = spawnPoint;
                    Debug.Log("Teleported to spawn!");
                }
            }
        }
    }
}
```

> [!TIP]
> Use `Input.GetKeyDown()` for single-press actions and `Input.GetKey()` for continuous actions.

---

## 🔧 Troubleshooting

### Build Errors

**Error: "Could not find UnityEngine.dll"**
- Make sure you've copied all required DLLs to the `libs/` folder
- Check that the paths in `.csproj` are correct

**Error: "MSB3277 netstandard version conflict"**
- This has been fixed by targeting `net472` instead of `net462`
- If you still see it, verify your `.csproj` has `<TargetFramework>net472</TargetFramework>`

### Runtime Errors

**Mod doesn't load in game:**
- Verify the DLL is in the correct mods folder
- Check game logs for error messages
- Ensure your mod class inherits from `MonoBehaviour`

**Game crashes on startup:**
- Check for infinite loops in `Start()` or `Awake()`
- Verify you're not accessing null references
- Use try-catch blocks during development

> [!CAUTION]
> Always backup your game saves before testing new mods!

---

## 🔗 Useful Resources

- [Unity Scripting API](https://docs.unity3d.com/ScriptReference/)
- [C# Programming Guide](https://learn.microsoft.com/en-us/dotnet/csharp/)
- [Unity MonoBehaviour Lifecycle](https://docs.unity3d.com/Manual/ExecutionOrder.html)

---

**Happy Modding! 🎮**
