import sys
import cv2
import neoapi
import requests
import time
import numpy as np
import io
from threading import Thread
from queue import Queue
print(neoapi)

# Function to save image and send its path to API
def encode_image(image):
    _, img_encoded = cv2.imencode('.jpg', image)
    return img_encoded.tobytes()

def process_image(image):
    encoded_frame = encode_image(image)
    files = {'image': ('image.jpg', encoded_frame, 'image/jpeg')}
    response = requests.post("http://127.0.0.1:5000/predict_cover", files=files)
    processed_image_bytes = response.content
    processed_image = cv2.imdecode(np.frombuffer(processed_image_bytes, np.uint8), cv2.IMREAD_COLOR) #decode_image
    return processed_image


def process_frame(frame, output_queue):
    pro_img = process_image(frame)
    output_queue.put(pro_img)

output_queue = Queue()
processing_complete = True
result = 0
cnt=0
cnt1=0
try:
    camera = neoapi.Cam()
    camera.Connect("700008697917")
    camera.f.ExposureTime.Set(800)  # 800&750 for chammper missing and 600 for champer present
    video = cv2.VideoWriter('outpy.avi', cv2.VideoWriter_fourcc(*'XVID'), 10,
                            (camera.f.Width.value, camera.f.Height.value))

    while True:
        img = camera.GetImage().GetNPArray()
        img = cv2.rotate(img, cv2.ROTATE_90_CLOCKWISE)
        video.write(img)

        cnt1 += 1
        # Save frame processing is complete
        if processing_complete:
            start_time = time.time()
            processing_complete = False
            # Process the frame in a separate thread
            #cv2.imwrite("camera_img.jpg", img)
            thread = Thread(target=process_frame, args=(img,output_queue))
            cnt+=1
            thread.start()

        if not output_queue.empty():
            framed = output_queue.get()
##            img = cv2.resize(img, (640, 500))
##            img_processed = cv2.resize(framed, (640, 500))
##            img_combined = cv2.hconcat([img, img_processed])
            img = cv2.resize(img, (640, 500))
            img_processed = cv2.resize(framed, (1200, 1000))
            #img_combined = cv2.hconcat([img, img_processed])

            #img_combined = cv2.resize(img_combined, (1280, 1000))
            end_time = time.time()
            execution_time_ms = (end_time - start_time) * 1000

            print("Total Execution time:", execution_time_ms, "ms")
            processing_complete = True
            #print("hi")
            cv2.imshow("Video Stream", img_processed)
            if cv2.waitKey(1) & 0xFF == ord('q'):
                print("frame passed to api: ", cnt)
                print("total frame we got: ", cnt1)
                break

    cv2.destroyAllWindows()
    video.release()

except (neoapi.NeoException, Exception) as exc:
    print('Error:', exc)
    result = 1

sys.exit(result)






def process_image_no(image):
    _, img_encoded = cv2.imencode('.jpg', image)
    image_bytes = img_encoded.tobytes()
    img_io = io.BytesIO(image_bytes)
    files = {'image': img_io}
    response = requests.post("http://127.0.0.1:5000/predict_cover", files=files)
    processed_image_bytes = response.content
    processed_image = cv2.imdecode(np.frombuffer(processed_image_bytes, np.uint8), cv2.IMREAD_COLOR) #decode_image 
    #cv2.imwrite("api_img.jpg", processed_image)

    return processed_image

def process_image_old(image_path):
    
    response = requests.post("http://127.0.0.1:5000/predict_Gear", data={"path": image_path})
    response_json = response.json()
    #r1 = response_json['r1']
    processed_image_path, class_ids = next(iter(response_json.items()))
    
    return processed_image_path, class_ids

def process_image_try(image):
    _, img_encoded = cv2.imencode('.jpg', image)
    image_bytes = img_encoded.tobytes()
    img_io = io.BytesIO(image_bytes)
    files = {'image': img_io}
    response = requests.post("http://127.0.0.1:5000/predict_cover", files=files)
    print(response)

    if response.status_code == 200:
        processed_image_bytes = response.json()  # Assuming API response is JSON
        #print(processed_image_bytes)
        img_list = []
        for img_bytes in processed_image_bytes:
            nparr = np.array(img_bytes, dtype=np.uint8)  # Convert list of integers to NumPy array
            img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
            img_list.append(img)

        img1 = img_list[0] if len(img_list) >= 1 else None
        img2 = img_list[1] if len(img_list) >= 2 else None

        if img1 is not None and img2 is not None:
            img_combined = np.concatenate((img1, img2), axis=1)
        else:
            img_combined = None

        return img_combined
    else:
        print("Error: Request failed with status code", response.status_code)
        return None
