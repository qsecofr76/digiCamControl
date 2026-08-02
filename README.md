# digiCamControl - Canon EOS Exposure Bracketing Fix Fork

> [!NOTE]
> **Fork Disclaimer & AI Attribution:**
> This repository is a dedicated fork of the original [dukus/digicamcontrol](https://github.com/dukus/digicamcontrol) project maintained by `@qsecofr76`.
> All codebase exploration, EDSDK driver diagnosis, reverse-engineering, and code modifications in this fork were identified, analyzed, and implemented autonomously using **Antigravity-Gemini** (Google DeepMind AI Coding Assistant) in pair programming with `@qsecofr76` (who had no prior technical knowledge of the underlying C# codebase or EDSDK driver internals).

---

## 📸 What Was Fixed & Why (Canon EOS Exposure Bracketing)

### The Problem
On Canon EOS cameras (tested and verified on Canon EOS M50 and related EOS models), exposure bracketing was unreliable:
1. Shots in Manual (M) mode did not change exposure (scattavano tutte alla stessa esposizione).
2. Exposure steps jumped, skipped, or executed 1-step out of sync (esposizione "a saltoni" o sfasata di uno scatto).
3. The final shot was often cut short or reset prematurely to the default exposure setting.

---

### Root Causes & Technical Fixes Implemented

#### 1. Added Manual Exposure Bracketing (Shutter Speed) Mode for Canon M Mode
- **Why**: Canon EDSDK firmware ignores `ExposureCompensation` in Manual (M) mode with fixed ISO. The only way to bracket exposure in M mode is by varying the shutter speed (Tv).
- **Fix**: Added **"Manual Exposure Bracketing"** (Shutter Speed mode) to `BraketingWnd.xaml` and `BracketingViewModel.cs`.

#### 2. Fixed EDSDK Property Setter Retry Limits & Removed Spurious `DoEvfAf`
- **Why**: In `EosCamera.cs`, `SetProperty` triggered a Live View Auto Focus (`DoEvfAf`) command on every property change and gave up after 10 retries (500 ms) if the camera was busy writing to the SD card (which takes 1-3 seconds).
- **Fix**: Removed `DoEvfAf` from `SetProperty` and increased the retry count to 100 (5 seconds) so property changes succeed cleanly after SD card writes.

#### 3. Eliminated 1-Step Exposure Lag & Fixed Hardware Event Dropping
- **Why**: In `CanonSDKBase.cs`, `Camera_PropertyChanged` dropped events when `IsBusy` was `true`, causing DigiCamControl to trigger `Capture()` before the Canon camera hardware confirmed the new shutter speed.
- **Fix**: Removed `if (IsBusy) return;` from `Camera_PropertyChanged` and added a hardware confirmation wait loop in `BracketingViewModel._timer_Elapsed`.

#### 4. Corrected 1/3 EV Step Translation Tables
- **Why**: `_shutterTable` and `_apertureTable` in `CanonSDKBase.cs` listed 1/2 EV codes before 1/3 EV codes for duplicate string keys (e.g. `"20"`, `"10"`, `"1/10"`). If 1/2 EV codes were registered, Canon cameras set to 1/3 EV mode rejected the setting.
- **Fix**: Reordered the dictionaries so 1/3 EV codes take priority. Also fixed a typo in `_ec` (`0xEB` -> `"-2 2/3"`).

#### 5. Added Exposure Settling Delay Before Resetting `DefValue`
- **Why**: Immediately after triggering the 5th shot, `Stop()` reset the camera shutter speed to `DefValue` while the 5th shot was still being exposed by the physical shutter curtain.
- **Fix**: Added a `Camera.IsBusy` wait loop and a 1000 ms grace delay in `Stop()` before restoring `DefValue`.

#### 6. Detailed Session Logging
- Automatically logs all bracketing operations, target values, and camera hardware confirmation states to `Bracketing_Log.txt` and `Documents/DigiCamControl_Bracketing_Log.txt`.

---

Original Author README:
=======================

[digiCamControl](http://digicamcontrol.com/)
==============

[![Join the chat at https://gitter.im/dukus/digiCamControl](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/dukus/digiCamControl?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

DSLR camera remote control open source software

==============
Code transfered from : https://code.google.com/p/nikon-camera-control/

digiCamControl - DSLR camera remote control open source software
Copyright (C) 2014  Duka Istvan

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
MERCHANTABILITY,FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
