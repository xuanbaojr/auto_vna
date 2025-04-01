from auto_service import AutoService
import grpc
import numpy as np
from concurrent import futures
import instruction_pb2, instruction_pb2_grpc
import time
import logging
import cv2

class GRPCServicer(instruction_pb2_grpc.GRPCServiceServicer):
    def __init__(self):
        self.auto_service = AutoService()

    def GetInstruction(self, request=None, context=None):
        # Giải mã các frame
        frame1, frame2, frame3, frame4 = request.frame1, request.frame2, request.frame3, request.frame4
        np1, np2, np3, np4 = np.frombuffer(frame1, np.uint8), np.frombuffer(frame2, np.uint8), np.frombuffer(frame3, np.uint8), np.frombuffer(frame4, np.uint8)
        frame1 = cv2.imdecode(np1, cv2.IMREAD_COLOR)
        frame2 = cv2.imdecode(np2, cv2.IMREAD_COLOR)
        frame3 = cv2.imdecode(np3, cv2.IMREAD_COLOR)
        frame4 = cv2.imdecode(np4, cv2.IMREAD_COLOR)
        type_instruction = request.type_instruction

        response = self.auto_service.get_instruction(frame1, frame2, frame3, frame4, type_instruction)
        return instruction_pb2.InstructionResponse(instruction_str=response)
    
def serve():
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    instruction_pb2_grpc.add_GRPCServiceServicer_to_server(
        GRPCServicer(), server
    )
    server.add_insecure_port('[::]:50051')
    server.start()
    print("Server started, listening on port 50051")
    try:
        while True:
            time.sleep(86400)
    except KeyboardInterrupt:
        server.stop(0)

if __name__ == "__main__":
    logging.basicConfig()
    
    serve()