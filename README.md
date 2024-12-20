# ViSCA-Recorder_Community

- Community (publicized) version of a recorder application for ViSCA project data collection on Pico 4 Pro.

# Project Logo

![](./README_Assets/visca-recorder-logo.svg)

# Code Copy

- [`./Unity-App_Code-Copy`](./Unity-App_Code-Copy) .
- Used for proof-of-work purposes.
- Released under the GNU AGPL 3.0 License.
- A copy of the ViSCA Recorder Unity C-Sharp Scripts in the `Unity-App` module of the [`liu-yucheng/ViSCA-Recorder`](https://github.com/liu-yucheng/ViSCA-Recorder) repository.
- The [`liu-yucheng/ViSCA-Recorder`](https://github.com/liu-yucheng/ViSCA-Recorder) repository will be publicly available if the following conditions are met.
  - An academic journal paper, that is at a computer science journal with a [`CCF-A`](https://www.ccf.org.cn/en/Bulletin/2019-05-13/663884.shtml) rating or above, and that is about the [`liu-yucheng/ViSCA-Recorder`](https://github.com/liu-yucheng/ViSCA-Recorder) repository, and whose first author is and only is [Yucheng Liu](https://orcid.org/0000-0002-5322-3983), and whose corresponding author is and only is [Mingming Fan](https://orcid.org/0000-0002-0356-4712), is confirmed to be published.

# Preparation

- Clone the repository or download the source.
- Open a terminal in the repository directory.
- Run the `git submodule update --init --recursive` command.
  - Make sure that you have copies of the following modules.
  - The [`Unity-App`](https://github.com/liu-yucheng/ViSCARecorder.GitHub-Repo) submodule.
  - The [`Script-Utils`](https://github.com/liu-yucheng/ViSCA-Recorder-Utils) submodule.
  - The [`Android-Plugin`](https://github.com/liu-yucheng/liu_yucheng.visca_recorder_android) submodule.
- Run the `git submodule update --remote --recursive` command.
  - Make sure that you have the latest versions of all modules.

# Compilation (Unity Build, Recommended)

- This build disables all Unity-Android runtime communication features.
- Go to the `Unity-App` submodule.
  - Open or import the submodule with [Unity Editor](https://unity.com/download).
  - Build the application with Unity Editor. This will build the `*.apk` application package.

# ~~Compilation~~ (Unity + Android Build, Deprecated)

- This build enables all Unity-Android runtime communication features.
- The following scenes are using Unity-Android runtime communication features.
  - `<this-repo>/Unity-App/Assets/ViSCARecorder/Scenes/.../recording-3.unity`
- Go to the `Unity-App` submodule.
  - Open or import the submodule with [Unity Editor](https://unity.com/download).
  - Export the application as an Android project.
  - Create a new `<repository>/Android-Build` folder.
  - Move the exported Android project content to the `<repository>/Android-Build` folder.
- Go to the `Android-Plugin` submodule.
  - Copy the submodule contents to the `<repository>/Android-Build` folder.
  - Overwrite all the duplicated folders and files.
- Go to the `<repository>/Android-Build` folder.
  - Open or import the folder with [Android Studio](https://developer.android.com/studio).
  - Synchronize the project with Gradle files.
  - Make the `main` module. This will build the `*.apk` application package.

# Running the Application

- Start the application on Pico 4 Pro.
- See if the application works.
- Open the application to start recording.
- Quit the application to stop recording.
- See [`./Usage_Screen-Recordings/Data-Recording_With-Unity-App.mp4`](./Usage_Screen-Recordings/Data-Recording_With-Unity-App.mp4) for details.

# Extracting Recorder Data

- Complete a recording session.
- Find the data at `/storage/emulated/0/Download/liu_yucheng.visca_recorder/` on your device.
- See [`./Sample-Data_Usage_Recorded/`](./Sample-Data_Usage_Recorded/) for what the data contents look like.
- Copy the data contents to your PC for further processing.

# Processing Recorder Data

- Install [Python](https://www.python.org/downloads/).
- Go to the `Script-Utils` submodule.
- Follow the [`Script-Utils/README.md`](./Script-Utils/README.md) - "Preparation" section to install the dependencies.
  - Install the following dependencies.
  - External package: [FFmpeg](https://ffmpeg.org/download.html) .
  - Python packages: [requirements.txt](./Script-Utils/requirements.txt) .
- Run `python Data_FromRecorder_Process.py <Folder_DataFromRecorder_Name>` to process the data at once.
- See [`./Sample-Data_Usage_Processed/`](./Sample-Data_Usage_Processed/) for what the processed data contents look like.
- See [`./Usage_Screen-Recordings/Data-Processing_With-Script-Utils.mp4`](./Usage_Screen-Recordings/Data-Processing_With-Script-Utils.mp4) for details.

# Making Contributions

- Consider redistributing this application with your own changes.
- Don't forget to comply with the GNU AGPL 3.0 License.
- Don't forget to comply with the CC-BY-SA 4.0 License.
- ["There is no system but GNU, and Linux is one of its kernels!"](https://www.gnu.org/fun/humor.html)

# Copyright

## Textual and Code Contents

```
Copyright (C) 2024 Yucheng Liu. Under the AGPL 3.0 License.
AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .
```

- [The AGPL 3.0 License.](./license)

## Non-textual or Non-code Contents

```
Copyright (C) 2024 Yucheng Liu. Under the CC-BY-SA 4.0 License.
CC 4.0 BY-SA License: https://creativecommons.org/licenses/by-sa/4.0/legalcode.txt .
```

- [The CC 4.0 BY-SA License.](./license-2)
