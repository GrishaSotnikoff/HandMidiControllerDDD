import asyncio
import json
import math
import signal
import sys
import logging
import os
import cv2
import mediapipe as mp
import websockets

# Silence TensorFlow / MediaPipe logs
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'
from absl import logging as absl_logging
absl_logging.set_verbosity(absl_logging.ERROR)

logging.basicConfig(level=logging.INFO, format='[%(asctime)s] %(levelname)s: %(message)s')

mp_hands = mp.solutions.hands
hands = mp_hands.Hands(
    static_image_mode=False,
    max_num_hands=1,
    min_detection_confidence=0.7,
    min_tracking_confidence=0.5
)

cap = cv2.VideoCapture(0)
if not cap.isOpened():
    logging.critical("❌ Could not open webcam.")
    sys.exit(1)

clients = set()

def calc_distance(a, b):
    return math.sqrt((a.x - b.x)**2 + (a.y - b.y)**2 + (a.z - b.z)**2)

def detect_gesture(landmarks):
    wrist = landmarks[mp_hands.HandLandmark.WRIST]
    thumb_tip = landmarks[mp_hands.HandLandmark.THUMB_TIP]
    index_tip = landmarks[mp_hands.HandLandmark.INDEX_FINGER_TIP]
    middle_tip = landmarks[mp_hands.HandLandmark.MIDDLE_FINGER_TIP]
    ring_tip = landmarks[mp_hands.HandLandmark.RING_FINGER_TIP]
    pinky_tip = landmarks[mp_hands.HandLandmark.PINKY_TIP]

    d_thumb = calc_distance(wrist, thumb_tip)
    d_index = calc_distance(wrist, index_tip)
    d_middle = calc_distance(wrist, middle_tip)
    d_ring = calc_distance(wrist, ring_tip)
    d_pinky = calc_distance(wrist, pinky_tip)

    fingers_extended = sum([
        d_thumb > 0.2,
        d_index > 0.2,
        d_middle > 0.2,
        d_ring > 0.2,
        d_pinky > 0.2
    ])

    if fingers_extended >= 4:
        return "open"
    if fingers_extended == 0:
        return "fist"
    if d_index > 0.2 and d_middle < 0.15:
        return "point"
    if d_index > 0.2 and d_middle > 0.2:
        return "victory"
    if d_index > 0.2 and pinky_tip.y < wrist.y:
        return "rock"

    return "unknown"

async def send_hand_data():
    while True:
        success, frame = cap.read()
        if not success:
            await asyncio.sleep(0.03)
            continue

        frame = cv2.flip(frame, 1)
        rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        result = hands.process(rgb)

        data = {"detected": False}

        if result.multi_hand_landmarks:
            lm = result.multi_hand_landmarks[0].landmark
            gesture = detect_gesture(lm)

            # take wrist position for general hand X/Y
            wrist = lm[mp_hands.HandLandmark.WRIST]
            data = {
                "Detected": True,
                "Gesture": gesture,
                "x": round(wrist.x, 4),
                "y": round(wrist.y, 4)
            }
            logging.info(data)
            mp.solutions.drawing_utils.draw_landmarks(
                frame, result.multi_hand_landmarks[0], mp_hands.HAND_CONNECTIONS
            )

        cv2.imshow("Hand Tracking", frame)
        if cv2.waitKey(1) & 0xFF == 27:
            break

        if clients:
            await asyncio.gather(*[client.send(json.dumps(data)) for client in clients])

        await asyncio.sleep(0.03)

async def handler(websocket):
    clients.add(websocket)
    logging.info("🔌 New client connected.")
    try:
        async for _ in websocket:
            pass
    except websockets.exceptions.ConnectionClosed:
        pass
    finally:
        clients.remove(websocket)
        logging.info("❌ Client disconnected.")

def shutdown():
    logging.info("🧹 Shutting down...")
    cap.release()
    cv2.destroyAllWindows()
    sys.exit(0)

def setup_signal_handlers():
    for sig in (signal.SIGINT, signal.SIGTERM):
        signal.signal(sig, lambda *_: shutdown())

async def main():
    setup_signal_handlers()
    server = await websockets.serve(handler, "localhost", 8765)
    logging.info("🛰️ WebSocket server running at ws://localhost:8765")
    await send_hand_data()

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        shutdown()
    except Exception as e:
        logging.error(f"❌ An error occurred: {e}")
        asyncio.run(main())
    
