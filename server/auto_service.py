import cv2
import os
import uuid
from face import FaceService
from clothes import ClothesService
import time
current_dir = os.path.dirname(__file__)

class AutoService:
    def __init__(self):
        self.face_service = FaceService()
        self.clothes_service = ClothesService()
    def get_instruction(self, frame1, frame2, frame3, frame4, type_instruction):
        if type_instruction == "is_check":
            return "haha"
            # start = time.time()
            # self.session_id = self.face_service.is_create_session(frame1)
            # print(f"Time check session: {time.time() - start}")
            # if self.session_id is True:
            #     return "session: True"
            
        if type_instruction == "clothes":
            start = time.time()
            keypoints = self.clothes_service.get_keypoints(frame1)
            print(f"Time check clothes: {time.time() - start}")
            if keypoints is True:
                return "clothes:True"
            else:
                return f"clothes: {str(keypoints)}"

