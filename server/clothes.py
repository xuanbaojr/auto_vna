import cv2
import numpy as np
from ultralytics import YOLO
import os

current_dir = os.path.dirname(__file__)
model = YOLO(f"{current_dir}/yolov8n-pose.pt")

class CLothesService:
    def check(self, frame):
        results = model(frame)
        keypoints = results[0].keypoints[0].xy.cpu().numpy()[0]
        for i in range(5,16,2):
            if keypoints[i].tolist() == [0,0] or keypoints[i+1].tolist() == [0,0]:
                return "clothes:false"
            elif abs(keypoints[i][1] - keypoints[i+1][1]) > 50:
                return "clothes:false"
        return "clothes:true"

if __name__ == "__main__":
    clothes_service = CLothesService()
    frame = cv2.imread("im_clothes.png")
    res = clothes_service.check(frame)
    print(res)
