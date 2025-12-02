# ArchipelagoDredge

**ArchipelagoDredge** is a WIP mod for the game *DREDGE* that integrates Archipelago multiworld support, enabling synchronized progression and goal tracking across multiple players' games.

![Archipelago Dredge](https://github.com/alextric234/ArchipelagoDredgeMod/blob/main/ArchipelagoDredge/Assets/ArchipelagoDredge.jpg)

## Features

- **Archipelago Integration**: Connects *DREDGE* to the Archipelago multiworld system.
- **Goal Tracking**: Automatically reports goal completion to the Archipelago server upon returning all relics to The Collector.


## How to Install
Download the DREDGE Mod Manager from [dredgemods.com](https://dredgemods.com/)!  
Install Winch  
Install Archipelago Dredge

### Hosting a Multiworld
Visit [Archipelago.gg/Setup](https://archipelago.gg/tutorial) for steps on setting up the Archipelago software to generate a multiworld 

### Joining a Multiworld

Once your save is loaded in DREDGE, you can join your Archipelago multiworld using **any of the following methods**:

---

#### 🖥️ Terminal Command

The in-game terminal lets you type commands directly.

**To open the terminal:**
- On most English (US/UK) keyboards, press **`~`** or **<kbd>`</kbd>** (the key above **Tab**).
- On some non-English layouts, that key may differ:
  - 🇩🇪 **German**: try **`ö`**
  - 🇫🇷 **French (AZERTY)**: try **`ù`** or **`²`**
  - 🇪🇸 **Spanish**: try **`ñ`**
  - 🇸🇪 **Swedish/Nordic**: try **`§`** or **`½`**
  - If none of these work, look for the key **just above the Tab key** or **to the left of the number 1 key** — it usually opens the terminal regardless of the printed symbol.

Once the terminal is open, type: `ap connect <hostname> <port> <slot name> [-p <password>]`

- Example: `ap connect archipelago.gg 38281 boatguy`
- You can include spaces in your slot name (e.g. `ap connect archipelago.gg 38281 boat guy`).
- The `-p` (or `password=<value>`) part is **optional** — only needed if the server requires a password.

---

#### ⚙️ Mod Config Menu
1. Open the in-game **DREDGE Menu**.
2. Click the **Mods** tab.
3. Enter your server details (host, port, slot name, and optional password).
4. Close the DREDGE menu.
5. Connect or disconnect at any time:
- Press **F8** to connect using the current settings.
- Press **F10** to disconnect.

_(You can also open the terminal and simply type `ap connect` to connect using your current mod settings.)_

---

#### 🪟 Pop-up UI
1. Press **F7** to open the in-game connection window.
2. Enter or confirm your connection details.
3. Click **Connect** to join the server.
- Press **Disconnect** to leave.

---

### 📝 Notes
- All three methods use the **same saved configuration**, so updates made in one place will appear in the others.
- If connection fails, check your **host, port, and slot name** carefully.
- You can safely disconnect and reconnect at any time without restarting DREDGE.



## ❗ Known Issues

### ⚠️ Mod Compatibility
ArchipelagoDredge has **not been tested with other DREDGE mods installed**.  
Some mods modify core systems that this mod depends on.

Confirmed conflicts:
- **Discord Rich Presence**
- **Tweaks**

If something is not working, please disable other mods and try again before reporting an issue.
