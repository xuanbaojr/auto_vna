import grpc
import instruction_pb2
import instruction_pb2_grpc
import cv2
import numpy as np
class CameraClient:
    def __init__(self):
        channel = grpc.insecure_channel("localhost:50051")
        self.stub = instruction_pb2_grpc.GRPCServiceStub(channel)
        
    def call_grpc_stream(self, frame1, frame2, frame3, frame4):
        """Send frames to gRPC server and get instruction stream"""
        _, f1 = cv2.imencode('.jpg', frame1)
        _, f2 = cv2.imencode('.jpg', frame2)
        _, f3 = cv2.imencode('.jpg', frame3)
        _, f4 = cv2.imencode('.jpg', frame4)

        frame1, frame2, frame3, frame4 = f1.tobytes(), f2.tobytes(), f3.tobytes(), f4.tobytes()
        request = instruction_pb2.InstructionRequest(
            frame1=frame1,
            frame2=frame2,
            frame3=frame3,
            frame4=frame4,
        )
        return self.stub.GetInstruction(request=request)

if __name__ == "__main__":
    client = CameraClient()
    frame1 = cv2.imread("im_.png")
    frame2 = cv2.imread("im_.png")
    frame3 = cv2.imread("im_.png")
    frame4 = cv2.imread("im_.png")
    response = client.call_grpc_stream(frame1, frame2, frame3, frame4)
    print(response.instruction_str)