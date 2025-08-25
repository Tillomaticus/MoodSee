# MoodSee

## 2.2 Update

- Added local emotion tracking using a model trained on FER2013 and Unity Inference Engine.
(Model used: https://github.com/shangeth/Facial-Emotion-Recognition-PyTorch-ONNX)

The current model is not the best choice for tracking the emotions as its trained on 32x32 greyscale 2d images.
I found that tilting your head can influence the detected expression. 
As a proof of concept, I am happy with the current state. 
But there is definetly room for improvement, both in smoothing the facetracking as well as selecting or training a better model for increased accuracy.

<img src="https://github.com/Tillomaticus/MoodSee/blob/main/Happy.jpg" alt="Happy" width="200"/>
<img src="https://github.com/Tillomaticus/MoodSee/blob/main/Fear.jpg" alt="Fear" width="200"/>
<img src="https://github.com/Tillomaticus/MoodSee/blob/main/Angry.jpg" alt="Angry" width="200"/>

----

### 2025 XRAIHack Cologne Project

[See on Devpost](https://devpost.com/software/c23-moodsee-by-empaths-xr)

Or watch the Video:
[![Watch the video](https://img.youtube.com/vi/TGOCwYbNPf8/maxresdefault.jpg)](https://youtu.be/TGOCwYbNPf8)


## 🎯 Inspiration
Many people with autism struggle to read facial expressions or emotional cues, which can lead to stress and isolation.

## 🐘 What it does
MoodSee helps users identify the emotions of the person they’re speaking to through simple visual cues inside the headset.

The app uses the headset’s passthrough-video data and sends images to ChatGPT to analyze facial expressions in (almost) real time and translate them to simple visuals above the speaker.

## 🛠️ How we built it
- Using Meta Building Blocks,
- BlazeFace for Face Detection 
- OpenAI ChatGPT 4o for Emotion Detection
- Vercel for the OpenAI Interface 
- Unity-PassthroughCameraApiSamples to get a grip of the Acces on the Quest 3 Passthrough Camera

## 🚧 Challenges we ran into
Face tracking in the headset:
The original AI models were trained on cropped faced, which is not what the headset is seeing. 
This forced us to switch to ChatGPT, which came with its own challenges. 
Getting access to camera access was an issue at first. 
Designing visuals that communicate emotion effectively without being distracting or stressful.

## 🏆 Accomplishments that we're proud of
Getting the emotion recognition pipeline to work

## 📚 What we learned
- How to use Meta-PCA
- Set up inference and ran RoboFlow AI
- Used face-tracking locally on the Quest
- Sent images to ChatGPT for recognition, including emotions
- Worked with voice recognition and built actions from it

## 🎯 Target Group
MoodSee is designed for neurodiverse users, especially those with autism, who struggle to interpret facial expressions during conversation. It acts as a real-time emotional assistant to reduce social friction and support smoother face-to-face communication.

## ⏭️ What's next for MoodSee by Empaths XR
- User testing!
- Consent feature that allows users to ask conversation partners for permission
- Voice detection to add another layer of emotional context
- Pet emotion tracking
