import asyncio
import json
import logging
import signal
import sys
import cv2
import mediapipe as mp
import websockets

# Silence TensorFlow / MediaPipe logs
import os
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'
from absl import logging as absl_logging
absl_logging.set_verbosity(absl_logging.ERROR)

logging.basicConfig(level=logging.INFO, format='[%(asctime)s] %(levelname)s: %(message)s')

# Setup MediaPipe Hand Tracking
mp_hands = mp.solutions.hands
hands = mp_hands.Hands(
    static_image_mode=False,
    max_num_hands=1,
    min_detection_confidence=0.7,
    min_tracking_confidence=0.5
)

# Setup OpenCV for webcam
cap = cv2.VideoCapture(0)
if not cap.isOpened():
    logging.critical("❌ Could not open webcam. Check camera permissions or device index.")
    sys.exit(1)
else:
    logging.info("📷 Webcam initialized successfully.")

clients = set()

async def send_finger_data():
    while True:
        success, frame = cap.read()
        if not success:
            logging.warning("Failed to read frame from webcam.")
            await asyncio.sleep(0.03)
            continue

        frame = cv2.flip(frame, 1)
        rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        result = hands.process(rgb)

        response = {"detected": False, "fingers": []}

        if result.multi_hand_landmarks:
            finger_tips = [
                mp_hands.HandLandmark.THUMB_TIP,
                mp_hands.HandLandmark.INDEX_FINGER_TIP,
                mp_hands.HandLandmark.MIDDLE_FINGER_TIP,
                mp_hands.HandLandmark.RING_FINGER_TIP,
                mp_hands.HandLandmark.PINKY_TIP,
            ]

            for tip in finger_tips:
                lm = result.multi_hand_landmarks[0].landmark[tip]
                response["fingers"].append({
                    "x": round(lm.x, 4),
                    "y": round(lm.y, 4),
                    "z": round(lm.z, 4)
                })
            response["detected"] = True
            logging.info(f"Finger data: {response}")
            # Draw landmarks for debug
            mp.solutions.drawing_utils.draw_landmarks(
                frame, result.multi_hand_landmarks[0], mp_hands.HAND_CONNECTIONS
            )

        # Debug OpenCV window
        cv2.imshow("Hand Tracking", frame)
        if cv2.waitKey(1) & 0xFF == 27:
            break

        # Broadcast to all connected clients
        if clients:
            message = json.dumps(response)
            await asyncio.gather(*[client.send(message) for client in clients])

        await asyncio.sleep(0.03)

async def handler(websocket):
    clients.add(websocket)
    logging.info("🔌 New WebSocket client connected.")
    try:
        async for _ in websocket:
            pass
    except websockets.exceptions.ConnectionClosed:
        pass
    finally:
        clients.remove(websocket)
        logging.info("❌ WebSocket client disconnected.")

def shutdown():
    logging.info("🧹 Shutting down server...")
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
    await send_finger_data()

if __name__ == "__main__":
    asyncio.run(main())
