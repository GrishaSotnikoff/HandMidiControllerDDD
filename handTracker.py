Python 3.12.6 (tags/v3.12.6:a4a2d2b, Sep  6 2024, 20:11:23) [MSC v.1940 64 bit (AMD64)] on win32
Type "help", "copyright", "credits" or "license()" for more information.
>>> # hand_server.py
... import asyncio
... import json
... import cv2
... import mediapipe as mp
... import websockets
... 
... mp_hands = mp.solutions.hands
... hands   = mp_hands.Hands(
...     min_detection_confidence=0.7,
...     min_tracking_confidence=0.5
... )
... 
... async def serve(ws, path):
...     cap = cv2.VideoCapture(0)
...     try:
...         while True:
...             ret, frame = cap.read()
...             if not ret:
...                 await asyncio.sleep(0.03)
...                 continue
... 
...             frame = cv2.flip(frame, 1)
...             rgb   = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
...             res   = hands.process(rgb)
... 
...             x = y = z = 0.0
...             if res.multi_hand_landmarks:
...                 lm = res.multi_hand_landmarks[0].landmark[mp_hands.HandLandmark.INDEX_FINGER_TIP]
...                 x, y, z = float(lm.x), float(lm.y), float(lm.z)
...                 # draw for quick visual debug
...                 mp.solutions.drawing_utils.draw_landmarks(frame, res.multi_hand_landmarks[0], mp_hands.HAND_CONNECTIONS)
... 
...             cv2.imshow("Python Hand Debug", frame)
...             if cv2.waitKey(1) & 0xFF == 27:
                break

            await ws.send(json.dumps({"x": x, "y": y, "z": z}))
            await asyncio.sleep(0.03)
    finally:
        cap.release()
        cv2.destroyAllWindows()

async def main():
    async with websockets.serve(serve, "localhost", 8765):
        await asyncio.Future()  # run forever

if __name__ == "__main__":
    asyncio.run(main())
