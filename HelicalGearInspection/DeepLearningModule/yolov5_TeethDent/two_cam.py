import cv2
import sys
import numpy as np
import threading
import neoapi
from queue import Queue
import time

class BaumerCamera:
    def __init__(self, serial_number, camera_label):
        self.serial_number = serial_number
        self.camera_label = camera_label
        self.camera = None
        self.capture_thread = None
        self.image_queue = Queue()
        self.is_running = False

    def connect(self):
        self.camera = neoapi.Cam()
        self.camera.Connect(self.serial_number)
        self.camera.ExposureTime = 800
        print("Both cameras connected")

    def start_capture(self):
        self.is_running = True
        self.capture_thread = threading.Thread(target=self._capture_frames)
        self.capture_thread.start()

    def stop_capture(self):
        self.is_running = False
        self.capture_thread.join()

    def _capture_frames(self):
        while self.is_running:
            start_time = time.time()

            try:
                image = self.camera.GetImage().GetNPArray()
                self.image_queue.put(image)
            except (neoapi.NeoException, Exception) as exc:
                print('Error capturing frame:', exc)

            elapsed_time = time.time() - start_time
            delay = max(0, 1 / 15 - elapsed_time)  # Adjust 15 to desired frame rate
            time.sleep(delay)

    def read(self):
        if not self.image_queue.empty():
            frame = self.image_queue.get()
            return frame
        else:
            return None


try:
    # Create two instances of the BaumerCamera class
    camera1 = BaumerCamera("DA0522572", "Camera 1")  
    camera2 = BaumerCamera("J94963970", "Camera 2")

    # Connect to the cameras
    camera1.connect()
    camera2.connect()

    # Start capturing frames
    camera1.start_capture()
    camera2.start_capture()

    temp = 0

    while True:
        # Read frames from both cameras
        frame1 = camera1.read()
        frame2 = camera2.read()

        if frame1 is not None and frame2 is not None:
            frame1 = cv2.putText(frame1, "Cam 1", (100, 150), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 3)
            frame2 = cv2.putText(frame2, "Cam 2", (100, 150), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 3)

            frame1 = cv2.resize(frame1, (frame2.shape[1], frame2.shape[0]))
            concatenated_image = cv2.hconcat([frame1, frame2])
            final_image = cv2.resize(concatenated_image, (1200, 800))
            cv2.imwrite("final_img.jpg", final_image)

            # Display the concatenated image
            cv2.imshow("Concatenated Images", final_image)
            temp += 1
        print(temp)

        # Check for key press
        key = cv2.waitKey(1) & 0xFF
        if key == ord('q'):
            break

except (neoapi.NeoException, Exception) as exc:
    print('Error:', exc)
    sys.exit(1)

finally:
    # Stop capturing and release resources
    camera1.stop_capture()
    camera2.stop_capture()
    cv2.destroyAllWindows()


"""
###################################################################################################
import cv2
import neoapi
import threading
from queue import Queue

class BaumerCamera:
    def __init__(self, serial_number):
        self.serial_number = serial_number
        self.camera = None
        self.capture_thread = None
        self.image_queue = Queue()

    def connect(self):
        self.camera = neoapi.Cam()
        self.camera.Connect(self.serial_number)
        self.camera.f.ExposureTime.Set(800)  # Adjust the exposure time as needed

    def start_capture(self):
        self.capture_thread = threading.Thread(target=self._capture_frames)
        self.capture_thread.start()

    def _capture_frames(self):
        while True:
            try:
                image = self.camera.GetImage().GetNPArray()
                self.image_queue.put(image)
            except neoapi.NeoTimeoutException:
                pass

    def stop_capture(self):
        if self.capture_thread:
            self.capture_thread.join()

    def disconnect(self):
        if self.camera:
            self.camera.Disconnect()
            self.camera = None

# Example usage:
if __name__ == '__main__':
    camera1 = BaumerCamera("700009367122")
    camera1.connect()
    camera1.start_capture()

    camera2 = BaumerCamera("700008697917")
    camera2.connect()
    camera2.start_capture()

    while True:
        if not camera1.image_queue.empty():
            image1 = camera1.image_queue.get()
            cv2.imshow("Camera 1", image1)

        if not camera2.image_queue.empty():
            image2 = camera2.image_queue.get()
            cv2.imshow("Camera 2", image2)

##        img_combined = cv2.hconcat([image1, image2])
##        img_combined = cv2.resize(img_combined, (1280, 1000))
##        cv2.imshow("both image", img_combined)

        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    camera1.stop_capture()
    camera1.disconnect()

    camera2.stop_capture()
    camera2.disconnect()

    cv2.destroyAllWindows()

"""
