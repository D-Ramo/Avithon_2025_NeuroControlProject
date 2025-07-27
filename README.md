# Brain Wave Monitor for Air Traffic Controller (ATCO) Fatigue and Stress Assessment

## Project Overview

This project is a brain wave monitoring system designed to assess the mental state of Air Traffic Controllers (ATCOs) while resolving air conflict situations. The goal is to detect whether the brain is in an active or inactive state, which helps correlate brain activity with fatigue and stress levels.

We use the **TGAM EEG brain wave sensor module kit** to gather EEG signals. This sensor connects to a simulation application that generates conflict scenarios for ATCOs. The EEG data is transmitted via **UDP multicast** to both a **backend API** and a **frontend visualization app**.

- The **backend** uses an AI model to classify the EEG signals as "active" or "inactive."
- The **frontend** displays EEG data, classification labels, and statistical insights from the backend.

---

## System Setup and Instructions

### 1. Connect the EEG Reader

- Connect the **TGAM EEG reader** to your PC via USB.
- Open **Device Manager** to identify the COM port assigned to the EEG device.

![Device Manager Example](https://media.discordapp.net/attachments/1397264002907897929/1399119485285306429/image.png?ex=6887d70f&is=6886858f&hm=c3a8f4b72dc55eae9ac32f07f3ac4e83326ea1b7e3aa57442fd6206fd6d081cb&=&format=webp&quality=lossless)

---

### 2. Run the ATC Simulator (SimATC)

- Navigate to the `SimATC-compiled` folder.
- Open the `run.cmd` file and **edit the following**:
  - `COM port` (as found in Device Manager)
  - `Multicast IP` and `Port` to be used for communication.
  -  `IPv4` (Better to not use local host)
- Save and run `run.cmd`.

```cmd
# EXAMPLE
simATC.exe COM3 192.175.9.10 230.0.0.0 5004
```
---
### 3. AI-API

After setting COM and Multicast channel in SimATC, open AI-API:

- Edit `config.py` to set multicast channel and port
- Execute `run-api.bat`
```py
# Change the following in the config.py file
    multicast_group: str = "230.0.0.0"  # Multicast IP address
    multicast_port: int = 4446           # Multicast port number
```
---

### 4. Frontend Monitor System

Open NeuroControlApp-compiled:

- Run `NeuroControlApp.exe`
- In the settings popup, set the following:
  - **API IP**
  - **API PORT**
  - **Multicast Group**
  - **Multicast IP address**
  - **Multicast Port**

If settings popup did not appear or you want to reconfigure, **PRESS 'S'** and popup should appear.
![NEURO_CONTROL_SETTINGS_POPUP_IMAGE](https://cdn.discordapp.com/attachments/1397264002907897929/1399121958314049647/image.png?ex=6887d95d&is=688687dd&hm=acde5695cf24d5c3e6b3e7ce94547d59beba8c49cf85a095c1de0e508e7d5f1d&)
