import cv2
import os
import uuid
from face import FaceService
from clothes import CLothesService
from hand import HandService
import time
current_dir = os.path.dirname(__file__)

class AutoService:
    def __init__(self):
        self.face_service = FaceService()
        self.clothes_service = CLothesService()
        self.hand_service = HandService()
    def get_instruction(self, frame1, frame2, frame3, frame4, type_instruction):
        if type_instruction == "is_check":
            start = time.time()
            check_face = self.face_service.is_create_session(frame1)
            print(f"Time check session: {time.time() - start}")
            return check_face
            
        if type_instruction == "clothes":
            start = time.time()
            check_clothes = self.clothes_service.check(frame2)
            print(f"Time check clothes: {time.time() - start}")
            return check_clothes
        
        if type_instruction == "hand":
            start = time.time()
            check_hand = self.hand_service.check(frame3)
            print(f"Time check hand: {time.time() - start}")
            return check_hand

